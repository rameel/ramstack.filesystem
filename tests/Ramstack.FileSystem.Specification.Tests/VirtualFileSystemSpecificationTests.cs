using System.Diagnostics.CodeAnalysis;
using System.Text;

using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Specification.Tests;

/// <summary>
/// Represents a base class for specification tests of virtual file systems.
/// </summary>
/// <param name="safePath">The safe path for modifications. Defaults to "/".</param>
/// <remarks>
/// This class defines common functionality and setup for tests that validate the behavior
/// of virtual file systems. Derived classes should implement the abstract methods to provide specific
/// details about the virtual file system being tested.
/// </remarks>
public abstract class VirtualFileSystemSpecificationTests(string safePath = "/")
{
    [Test]
    [Order(-1003)]
    public async Task FileTree_MatchesStructure()
    {
        using (var fs = GetFileSystem())
            await CompareDirectoriesAsync(fs.GetDirectory("/"), GetDirectoryInfo());

        static async Task CompareDirectoriesAsync(VirtualDirectory virtualDir, DirectoryInfo localDir)
        {
            if (await virtualDir.ExistsAsync() != localDir.Exists)
                Assert.Fail($"Directory '{virtualDir.FullName}'.Exists ({virtualDir.ExistsAsync().Result}) != '{localDir.FullName}'.Exists ({localDir.Exists})");

            var virtualFiles = await virtualDir.GetFilesAsync().OrderBy(f => f.Name).ToArrayAsync();
            var localFiles = localDir.EnumerateFiles().OrderBy(f => f.Name).ToArray();

            Assert.That(
                virtualFiles.Select(f => f.Name),
                Is.EquivalentTo(localFiles.Select(f => f.Name)),
                virtualDir.FullName);

            for (var i = 0; i < virtualFiles.Length; i++)
                await CompareFilesAsync(virtualFiles[i], localFiles[i]);

            var virtualDirectories = await virtualDir.GetDirectoriesAsync().OrderBy(d => d.Name).ToArrayAsync();
            var localDirectories = localDir.EnumerateDirectories().OrderBy(d => d.Name).ToArray();

            Assert.That(
                virtualDirectories.Select(f => f.Name),
                Is.EquivalentTo(localDirectories.Select(f => f.Name)),
                virtualDir.FullName);

            for (var i = 0; i < virtualDirectories.Length; i++)
                await CompareDirectoriesAsync(virtualDirectories[i], localDirectories[i]);
        }

        static async Task CompareFilesAsync(VirtualFile virtualFile, FileInfo localFile)
        {
            var vc = await ReadAllTextAsync(await virtualFile.OpenReadAsync());
            var lc = await ReadAllTextAsync(localFile.OpenRead());

            Assert.That(vc, Is.EqualTo(lc), virtualFile.FullName);
        }
    }

    [Test]
    [Order(-1002)]
    public async Task FileTree_VirtualNode_FileSystem_MatchesIssuingFileSystemReference()
    {
        using var fs = GetFileSystem();

        Assert.That(
            await fs.GetAllFilesRecursively("/").CountAsync(),
            Is.Not.Zero);

        await foreach (var node in fs.GetAllFileNodesRecursively("/"))
        {
            VirtualNode byPath = node is VirtualFile
                ? fs.GetFile(node.FullName)
                : fs.GetDirectory(node.FullName);

            Assert.That(fs, Is.SameAs(node.FileSystem));
            Assert.That(fs, Is.SameAs(byPath.FileSystem));
        }
    }

    [Test]
    [Order(-1001)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public async Task FileTree_NavigateAboveRoot_ThrowsException()
    {
        using var fs = GetFileSystem();

        Assert.Throws<ArgumentException>(() => fs.GetDirectory(".."));
        Assert.Throws<ArgumentException>(() => fs.GetDirectory("/.."));
        Assert.Throws<ArgumentException>(() => fs.GetDirectory("/././.."));
        Assert.Throws<ArgumentException>(() => fs.GetFile(".."));
        Assert.Throws<ArgumentException>(() => fs.GetFile("/.."));
        Assert.Throws<ArgumentException>(() => fs.GetFile("/././.."));

        Assert.That(
            await fs.GetAllFilesRecursively("/").CountAsync(),
            Is.Not.Zero);

        await foreach (var node in fs.GetAllFileNodesRecursively("/"))
        {
            foreach (var path in GetPathsAboveRoot(node.FullName))
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    if (node is VirtualDirectory)
                        fs.GetDirectory(path);
                    else
                        fs.GetFile(path);
                });
            }
        }
    }

    [Test]
    public async Task Exists_ReturnsTrue_For_ExistingFile()
    {
        using var fs = GetFileSystem();

        Assert.That(
            await fs.GetAllFilesRecursively("/").CountAsync(),
            Is.Not.Zero);

        await foreach (var node in fs.GetAllFileNodesRecursively("/"))
        {
            VirtualNode byPath = node is VirtualFile
                ? node.FileSystem.GetFile(node.FullName)
                : node.FileSystem.GetDirectory(node.FullName);

            Assert.That(await node.ExistsAsync(), Is.True);
            Assert.That(await byPath.ExistsAsync(), Is.True);
        }
    }

    [Test]
    public async Task File_Exists_ReturnsFalse_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        var file = fs.GetFile($"{safePath}/{Guid.NewGuid()}");
        Assert.That(await file.ExistsAsync(), Is.False);
    }

    [Test]
    public async Task File_OpenRead_ReturnsReadableStream()
    {
        using var fs = GetFileSystem();

        Assert.That(
            await fs.GetAllFilesRecursively("/").CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively("/"))
        {
            await using var stream = await file.OpenReadAsync();
            Assert.That(stream.CanRead, Is.True);

            stream.ReadByte();
        }
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void File_OpenRead_ThrowsException_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        var name = Guid.NewGuid().ToString();

        Assert.That(() => fs.OpenReadAsync($"/{name}.txt"), Throws.Exception);
        Assert.That(() => fs.OpenReadAsync($"{safePath}/{name}.txt"), Throws.Exception);
        Assert.That(() => fs.OpenReadAsync($"{safePath}/{name}/{name}.txt"), Throws.Exception);
    }

    [Test]
    public async Task File_OpenWrite_ReturnsWritableStream()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        Assert.That(
            await fs.GetAllFilesRecursively("/").CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively("/"))
        {
            var content = $"Id:{Guid.NewGuid()}";

            await using (var stream = await file.OpenWriteAsync())
            {
                Assert.That(stream.CanWrite, Is.True);

                await stream.WriteAsync(Encoding.UTF8.GetBytes(content));
            }

            Assert.That(
                await ReadAllTextAsync(await file.OpenReadAsync()),
                Is.EqualTo(content));
        }
    }

    [Test]
    public async Task File_OpenWrite_NewFile()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var content = $"Automatically generated on {DateTime.Now:s}\n\nNew Id:{Guid.NewGuid()}";
        var name = $"{safePath}/{Guid.NewGuid()}";

        await using (var stream = await fs.OpenWriteAsync(name))
        {
            Assert.That(stream.CanWrite, Is.True);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(content));
        }

        Assert.That(
            await ReadAllTextAsync(await fs.OpenReadAsync(name)),
            Is.EqualTo(content));

        await fs.DeleteFileAsync(name);

        Assert.That(
            await fs.GetFile(name).ExistsAsync(),
            Is.False);
    }

    [Test]
    public async Task File_Write_OverwritesContent_When_OverwriteIsTrue()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        Assert.That(
            await fs.GetAllFilesRecursively("/").CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively("/"))
        {
            var content = $"Id:{Guid.NewGuid()}";

            var ms = new MemoryStream();
            ms.Write(Encoding.UTF8.GetBytes(content));
            ms.Position = 0;

            await file.WriteAsync(ms, overwrite: true);

            Assert.That(
                await ReadAllTextAsync(await file.OpenReadAsync()),
                Is.EqualTo(content));
        }
    }

    [Test]
    public async Task File_Write_ThrowsException_For_ExistingFile_When_OverwriteIsFalse()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        Assert.That(
            await fs.GetAllFilesRecursively("/").CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively("/"))
        {
            var current = await ReadAllTextAsync(await file.OpenReadAsync());

            Assert.That(
                () => file.WriteAsync(new MemoryStream(), overwrite: false),
                Throws.Exception);

            Assert.That(
                await ReadAllTextAsync(await file.OpenReadAsync()),
                Is.EqualTo(current));
        }
    }

    [Test]
    public async Task File_Write_NewFile()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var content = $"Automatically generated on {DateTime.Now:s}\n\nNew Id:{Guid.NewGuid()}";
        var path = $"{safePath}/{Guid.NewGuid()}";

        var ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetBytes(content));
        ms.Position = 0;

        var file = fs.GetFile(path);
        Assert.That(await file.ExistsAsync(), Is.False);

        await file.WriteAsync(ms);

        Assert.That(await file.ExistsAsync(), Is.True);
        Assert.That(await ReadAllTextAsync(await file.OpenReadAsync()), Is.EqualTo(content));

        await file.DeleteAsync();

        Assert.That(await file.ExistsAsync(), Is.False);

    }

    [Test]
    public async Task File_Delete_For_ExistingFile()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var path = $"{safePath}/{Guid.NewGuid()}";
        var file = fs.GetFile(path);

        Assert.That(await file.ExistsAsync(), Is.False);

        await file.WriteAsync(new MemoryStream());

        Assert.That(await file.ExistsAsync(), Is.True);

        await file.DeleteAsync();

        Assert.That(await file.ExistsAsync(), Is.False);
    }

    [Test]
    public async Task File_Delete_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var name = Guid.NewGuid().ToString();

        await fs.DeleteFileAsync($"/{name}.txt");
        await fs.DeleteFileAsync($"{safePath}/{name}.txt");
        await fs.DeleteFileAsync($"{safePath}/{name}/{name}.txt");
    }

    [Test]
    public async Task File_Readonly_OpenWrite_ThrowsException()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        Assert.That(
            await fs.GetAllFilesRecursively("/").CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively("/"))
        {
            Assert.That(
                async () => { await using var stream = await file.OpenWriteAsync(); },
                Throws.Exception);
        }
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void File_Readonly_OpenWrite_ThrowsException_For_NewFile()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        Assert.That(
            () => fs.OpenWriteAsync($"/{Guid.NewGuid()}"),
            Throws.Exception);

        Assert.That(
            () => fs.OpenWriteAsync($"{safePath}/{Guid.NewGuid()}"),
            Throws.Exception);
    }

    [Test]
    public async Task File_Readonly_Write_ThrowsException()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        Assert.That(
            await fs.GetAllFilesRecursively("/").CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively("/"))
        {
            Assert.That(
                () => file.WriteAsync(new MemoryStream()),
                Throws.Exception);
        }
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void File_Readonly_Write_ThrowsException_For_NewFile()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        Assert.That(() => fs.WriteAsync($"/{Guid.NewGuid()}", new MemoryStream()), Throws.Exception);
        Assert.That(() => fs.WriteAsync($"{safePath}/{Guid.NewGuid()}", new MemoryStream()), Throws.Exception);
    }

    [Test]
    public async Task File_Readonly_Delete_ThrowsException()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        Assert.That(
            await fs.GetAllFilesRecursively("/").CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively("/"))
            Assert.That(() => file.DeleteAsync(), Throws.Exception);
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void File_Readonly_Delete_ThrowsException_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        Assert.That(() => fs.DeleteFileAsync($"/{Guid.NewGuid()}"), Throws.Exception);
        Assert.That(() => fs.DeleteFileAsync($"{safePath}/{Guid.NewGuid()}"), Throws.Exception);
    }

    [Test]
    public async Task File_CopyTo_Path()
    {
        using var fs = GetFileSystem();
        if (fs.IsReadOnly)
            return;

        var file = await fs.GetAllFilesRecursively("/").FirstAsync();
        var destinationPath = file.FullName + ".copy";

        Assert.That(await fs.FileExistsAsync(destinationPath), Is.False);

        await file.CopyToAsync(destinationPath);
        Assert.That(await fs.FileExistsAsync(destinationPath), Is.True);

        Assert.That(
            await ReadAllTextAsync(fs.GetFile(destinationPath)),
            Is.EqualTo(await ReadAllTextAsync(file)));

        await fs.DeleteFileAsync(destinationPath);
        Assert.That(await fs.FileExistsAsync(destinationPath), Is.False);
    }

    [Test]
    public async Task File_CopyTo_Path_NoOverwrite_ThrowsException()
    {
        using var fs = GetFileSystem();
        if (fs.IsReadOnly)
            return;

        var file = await fs.GetAllFilesRecursively("/").FirstAsync();
        var destinationPath = file.FullName + ".copy";

        await file.CopyToAsync(destinationPath);

        Assert.That(await fs.FileExistsAsync(destinationPath), Is.True);
        Assert.That(() => file.CopyToAsync(destinationPath), Throws.Exception);

        await fs.DeleteFileAsync(destinationPath);
        Assert.That(await fs.FileExistsAsync(destinationPath), Is.False);
    }

    [Test]
    public async Task File_CopyTo_File_SameFileSystems()
    {
        using var fs = GetFileSystem();
        if (fs.IsReadOnly)
            return;

        var file = await fs.GetAllFilesRecursively("/").FirstAsync();
        var destination = fs.GetFile(file.FullName + ".copy");

        Assert.That(await destination.ExistsAsync(), Is.False);

        await file.CopyToAsync(destination);

        Assert.That(await destination.ExistsAsync(), Is.True);
        Assert.That(
            await ReadAllTextAsync(destination),
            Is.EqualTo(await ReadAllTextAsync(file)));

        await destination.DeleteAsync();
        Assert.That(await destination.ExistsAsync(), Is.False);
    }

    [Test]
    public async Task File_CopyTo_File_NotSameFileSystems()
    {
        using var fs1 = GetFileSystem();
        using var fs2 = GetFileSystem();

        if (fs1.IsReadOnly)
            return;

        var file = await fs1.GetAllFilesRecursively("/").FirstAsync();
        var destination = fs2.GetFile(file.FullName + ".copy");

        Assert.That(await destination.ExistsAsync(), Is.False);

        await file.CopyToAsync(destination);

        Assert.That(await destination.ExistsAsync(), Is.True);
        Assert.That(
            await ReadAllTextAsync(destination),
            Is.EqualTo(await ReadAllTextAsync(file)));

        await destination.DeleteAsync();
        Assert.That(await destination.ExistsAsync(), Is.False);
    }

    [Test]
    public async Task File_CopyTo_File_NoOverwrite_ThrowsException()
    {
        using var fs = GetFileSystem();
        if (fs.IsReadOnly)
            return;

        var file = await fs.GetAllFilesRecursively("/").FirstAsync();
        var destination = fs.GetFile(file.FullName + ".copy");

        Assert.That(await destination.ExistsAsync(), Is.False);
        await file.CopyToAsync(destination);

        Assert.That(await destination.ExistsAsync(), Is.True);
        Assert.That(() => file.CopyToAsync(destination), Throws.Exception);

        await destination.DeleteAsync();
        Assert.That(await destination.ExistsAsync(), Is.False);
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public async Task File_CopyTo_ThrowsException_When_CopyingToItself()
    {
        using var fs1 = GetFileSystem();
        using var fs2 = GetFileSystem();

        if (fs1.IsReadOnly)
            return;

        var file = await fs1.GetAllFilesRecursively("/").FirstAsync();
        Assert.That(() => file.CopyToAsync(file.FullName), Throws.Exception);
        Assert.That(() => file.CopyToAsync(file), Throws.Exception);
        Assert.That(() => file.CopyToAsync(fs1.GetFile(file.FullName)), Throws.Exception);
        Assert.That(() => file.CopyToAsync(fs2.GetFile(file.FullName)), Throws.Exception);
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void File_CopyTo_ThrowException_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        Assert.That(() => fs.CopyFileAsync($"/{Guid.NewGuid()}", "/test.txt"), Throws.Exception);
        Assert.That(() => fs.CopyFileAsync($"{safePath}/{Guid.NewGuid()}", $"/{safePath}/test.txt"), Throws.Exception);
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public async Task File_Readonly_CopyTo_ThrowException_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        var file = await fs.GetAllFilesRecursively("/").FirstAsync();
        Assert.That(() => file.CopyToAsync(file.FullName + ".copy"), Throws.Exception);
    }

    [Test]
    public async Task Directory_Enumerate_ReturnsEmpty_For_NonExistingDirectory()
    {
        using var fs = GetFileSystem();

        var directory = fs.GetDirectory($"/{Guid.NewGuid()}");

        Assert.That(await directory.GetFileNodesAsync().CountAsync(), Is.Zero);
        Assert.That(await directory.GetFilesAsync().CountAsync(), Is.Zero);
        Assert.That(await directory.GetDirectoriesAsync().CountAsync(), Is.Zero);
    }

    [Test]
    public async Task Directory_Create_For_ExistingDirectory()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        Assert.That(
            await fs.GetAllDirectoriesRecursively("/").CountAsync(),
            Is.Not.Zero);

        await foreach (var directory in fs.GetAllDirectoriesRecursively("/"))
            await directory.CreateAsync();
    }

    [Test]
    public async Task Directory_Create_For_NonExistingDirectory()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var name = Guid.NewGuid().ToString();
        var directory = fs.GetDirectory($"{safePath}/{name}/{name}");

        await directory.CreateAsync();
        Assert.That(await directory.ExistsAsync(), Is.True);
    }

    [Test]
    public async Task Directory_Delete()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var name = Guid.NewGuid().ToString();

        var directory = fs.GetDirectory($"{safePath}/{name}-dir");

        for (var i = 0; i < 10; i++)
        {
            await fs.WriteAsync($"{directory.FullName}/{Guid.NewGuid()}.txt", new MemoryStream());
            await fs.WriteAsync($"{directory.FullName}/{Guid.NewGuid()}/{Guid.NewGuid()}.txt", new MemoryStream());
        }

        Assert.That(
            await directory.GetAllFilesRecursively().CountAsync(),
            Is.EqualTo(20));

        await directory.DeleteAsync();

        Assert.That(
            await directory.GetAllFilesRecursively().CountAsync(),
            Is.Zero);
    }

    [Test]
    public async Task Directory_Delete_For_NonExistingDirectory()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var name = Guid.NewGuid().ToString();

        var dir1 = fs.GetDirectory($"/{name}-1");
        var dir2 = fs.GetDirectory($"/{name}-2/{name}-3");
        var dir3 = fs.GetDirectory($"{safePath}/{name}-4");
        var dir4 = fs.GetDirectory($"{safePath}/{name}-5/{name}-6");

        Assert.That(await dir1.GetFileNodesAsync().CountAsync(), Is.Zero);
        Assert.That(await dir2.GetFileNodesAsync().CountAsync(), Is.Zero);
        Assert.That(await dir3.GetFileNodesAsync().CountAsync(), Is.Zero);
        Assert.That(await dir4.GetFileNodesAsync().CountAsync(), Is.Zero);

        await dir1.DeleteAsync();
        await dir2.DeleteAsync();
        await dir3.DeleteAsync();
        await dir4.DeleteAsync();
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void Directory_Readonly_Delete_ThrowsException_For_NonExistingDirectory()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        Assert.That(() => fs.DeleteDirectoryAsync($"/{Guid.NewGuid()}"), Throws.Exception);
        Assert.That(() => fs.DeleteDirectoryAsync($"{safePath}/{Guid.NewGuid()}"), Throws.Exception);
    }

    /// <summary>
    /// Returns an instance of the virtual file system.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="IVirtualFileSystem"/>.
    /// </returns>
    protected abstract IVirtualFileSystem GetFileSystem();

    /// <summary>
    /// Returns a <see cref="DirectoryInfo"/> object representing the root of the test directory.
    /// </summary>
    /// <returns>
    /// A <see cref="DirectoryInfo"/> object that points to the root of the test directory.
    /// </returns>
    protected abstract DirectoryInfo GetDirectoryInfo();

    private static async Task<string> ReadAllTextAsync(VirtualFile file)
    {
        using var reader = await file.OpenTextAsync();
        return await reader.ReadToEndAsync();
    }

    private static async Task<string> ReadAllTextAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static IEnumerable<string> GetPathsAboveRoot(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var n = 0; n <= segments.Length; n++)
        {
            var prefix = segments.Take(n).ToArray();
            var suffix = segments.Skip(n).ToArray();
            var parent = Enumerable.Repeat("..", n + 1).ToArray();
            var curdir = Enumerable.Repeat(".",  n + 1).ToArray();

            yield return "/" + string.Join("/", prefix.Concat(parent).Concat(suffix));
            yield return "/" + string.Join("/", prefix.Concat(curdir).Concat(parent).Concat(suffix));
            yield return "/" + string.Join("/", curdir.Concat(prefix).Concat(parent).Concat(suffix));
        }
    }
}
