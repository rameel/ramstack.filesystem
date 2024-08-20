using System.Diagnostics.CodeAnalysis;
using System.Text;

using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Specification.Tests;

/// <summary>
/// Represents a base class for specification tests of virtual file systems.
/// </summary>
/// <param name="rootPath">The root path to be used for the virtual file system. Defaults to "/" if not specified.</param>
/// <remarks>
/// This class defines common functionality and setup for tests that validate the behavior
/// of virtual file systems. Derived classes should implement the abstract methods to provide specific
/// details about the virtual file system being tested.
/// </remarks>
public abstract class VirtualFileSystemSpecificationTests(string rootPath = "/")
{
    [Test]
    [Order(-1003)]
    public async Task FileTree_MatchesStructure()
    {
        using (var fs = GetFileSystem())
            await CompareDirectoriesAsync(fs.GetDirectory(rootPath), GetDirectoryInfo());

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
            await fs.GetAllFilesRecursively(rootPath).CountAsync(),
            Is.Not.Zero);

        await foreach (var node in fs.GetAllFileNodesRecursively(rootPath))
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
            await fs.GetAllFilesRecursively(rootPath).CountAsync(),
            Is.Not.Zero);

        await foreach (var node in fs.GetAllFileNodesRecursively(rootPath))
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
            await fs.GetAllFilesRecursively(rootPath).CountAsync(),
            Is.Not.Zero);

        await foreach (var node in fs.GetAllFileNodesRecursively(rootPath))
        {
            VirtualNode byPath = node is VirtualFile
                ? node.FileSystem.GetFile(node.FullName)
                : node.FileSystem.GetDirectory(node.FullName);

            Assert.That(await node.ExistsAsync());
            Assert.That(await byPath.ExistsAsync());
        }
    }

    [Test]
    public async Task File_Exists_ReturnsFalse_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        var file = fs.GetFile($"{rootPath}/{Guid.NewGuid()}");
        Assert.That(await file.ExistsAsync(), Is.False);
    }

    [Test]
    public async Task File_OpenRead_ReturnsReadableStream()
    {
        using var fs = GetFileSystem();

        Assert.That(
            await fs.GetAllFilesRecursively(rootPath).CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively(rootPath))
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

        Assert.That(() => fs.OpenReadAsync($"{rootPath}/{name}.txt"), Throws.Exception);
        Assert.That(() => fs.OpenReadAsync($"{rootPath}/{name}/{name}.txt"), Throws.Exception);
    }

    [Test]
    public async Task File_OpenWrite_ReturnsWritableStream()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        Assert.That(
            await fs.GetAllFilesRecursively(rootPath).CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively(rootPath))
        {
            var content = $"Automatically generated on {DateTime.Now:s}\n\nNew Id:{Guid.NewGuid()}";

            await using (var stream = await file.OpenWriteAsync())
            {
                Assert.That(stream.CanWrite, Is.True);

                stream.Write(Encoding.UTF8.GetBytes(content));
                stream.SetLength(stream.Position);
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
        var name = $"{rootPath}/{Guid.NewGuid()}";

        await using (var stream = await fs.OpenWriteAsync(name))
        {
            Assert.That(stream.CanWrite, Is.True);
            stream.Write(Encoding.UTF8.GetBytes(content));
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
            await fs.GetAllFilesRecursively(rootPath).CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively(rootPath))
        {
            var content = $"Automatically generated on {DateTime.Now:s}\n\nNew Id:{Guid.NewGuid()}";

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
            await fs.GetAllFilesRecursively(rootPath).CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively(rootPath))
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
        var path = $"{rootPath}/{Guid.NewGuid()}";

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

        var path = $"{rootPath}/{Guid.NewGuid()}";
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

        await fs.DeleteFileAsync($"{rootPath}/{name}.txt");
        await fs.DeleteFileAsync($"{rootPath}/{name}/{name}.txt");
    }

    [Test]
    public async Task File_Readonly_OpenWrite_ThrowsException()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        Assert.That(
            await fs.GetAllFilesRecursively(rootPath).CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively(rootPath))
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
            () => fs.OpenWriteAsync($"{rootPath}/{Guid.NewGuid()}"),
            Throws.Exception);
    }

    [Test]
    public async Task File_Readonly_Write_ThrowsException()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        Assert.That(
            await fs.GetAllFilesRecursively(rootPath).CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively(rootPath))
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

        Assert.That(
            () => fs.WriteFileAsync($"{rootPath}/{Guid.NewGuid()}", new MemoryStream()),
            Throws.Exception);
    }

    [Test]
    public async Task File_Readonly_Delete_ThrowsException()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        Assert.That(
            await fs.GetAllFilesRecursively(rootPath).CountAsync(),
            Is.Not.Zero);

        await foreach (var file in fs.GetAllFilesRecursively(rootPath))
        {
            Assert.That(
                () => file.DeleteAsync(),
                Throws.Exception);
        }
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void File_Readonly_Delete_ThrowsException_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        Assert.That(
            () => fs.DeleteFileAsync($"{rootPath}/{Guid.NewGuid()}"),
            Throws.Exception);
    }

    [Test]
    public async Task Directory_Enumerate_ReturnsEmpty_For_NonExistingDirectory()
    {
        using var fs = GetFileSystem();

        var directory = fs.GetDirectory($"{rootPath}/{Guid.NewGuid()}");

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
            await fs.GetAllDirectoriesRecursively(rootPath).CountAsync(),
            Is.Not.Zero);

        await foreach (var directory in fs.GetAllDirectoriesRecursively(rootPath))
            await directory.CreateAsync();
    }

    [Test]
    public async Task Directory_Create_For_NonExistingDirectory()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var name = Guid.NewGuid().ToString();
        var directory = fs.GetDirectory($"{rootPath}/{name}/{name}");

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

        var directory = fs.GetDirectory($"{rootPath}/{name}-dir");

        for (var i = 0; i < 10; i++)
        {
            await fs.WriteFileAsync($"{directory.FullName}/{Guid.NewGuid()}.txt", new MemoryStream());
            await fs.WriteFileAsync($"{directory.FullName}/{Guid.NewGuid()}/{Guid.NewGuid()}.txt", new MemoryStream());
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

        var dir1 = fs.GetDirectory($"{rootPath}/{name}-1");
        var dir2 = fs.GetDirectory($"{rootPath}/{name}-2/{name}-3");

        Assert.That(await dir1.GetFileNodesAsync().CountAsync(), Is.Zero);
        Assert.That(await dir2.GetFileNodesAsync().CountAsync(), Is.Zero);

        await dir1.DeleteAsync();
        await dir2.DeleteAsync();
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void Directory_Readonly_Delete_ThrowsException_For_NonExistingDirectory()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        Assert.That(
            () => fs.DeleteDirectoryAsync($"{rootPath}/{Guid.NewGuid()}"),
            Throws.Exception);
    }

    /// <summary>
    /// Retrieves the instance of the virtual file system to be used in the tests.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="IVirtualFileSystem"/> representing the virtual file system.
    /// </returns>
    protected abstract IVirtualFileSystem GetFileSystem();

    /// <summary>
    /// Retrieves the <see cref="DirectoryInfo"/> object representing the root directory of the file system.
    /// </summary>
    /// <returns>
    /// A <see cref="DirectoryInfo"/> object that points to the root directory of the virtual file system.
    /// </returns>
    protected abstract DirectoryInfo GetDirectoryInfo();

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
