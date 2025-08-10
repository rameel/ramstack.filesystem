using System.Security.Cryptography;
using System.Text;

namespace Ramstack.FileSystem.Specification.Tests;

/// <summary>
/// Represents a base class for specification tests of virtual file systems.
/// </summary>
/// <remarks>
/// This class defines common functionality and setup for tests that validate the behavior
/// of virtual file systems. Derived classes should implement the abstract methods to provide specific
/// details about the virtual file system being tested.
/// </remarks>
public abstract class VirtualFileSystemSpecificationTests
{
    [Test]
    [Order(-1003)]
    public async Task FileTree_MatchesStructure()
    {
        using var fs = GetFileSystem();
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
            Assert.That(
                await virtualFile.ReadAllTextAsync(),
                Is.EqualTo(await File.ReadAllTextAsync(localFile.FullName)),
                virtualFile.FullName);
        }
    }

    [Test]
    [Order(-1002)]
    public async Task FileTree_VirtualNode_FileSystem_MatchesIssuingFileSystemReference()
    {
        using var fs = GetFileSystem();

        Assert.That(
            await fs.GetFilesAsync("/", "**").AnyAsync(),
            Is.True);

        await foreach (var node in fs.GetFileNodesAsync("/", "**"))
        {
            VirtualNode byPath = node is VirtualFile
                ? fs.GetFile(node.FullName)
                : fs.GetDirectory(node.FullName);

            Assert.That(fs, Is.SameAs(node.FileSystem));
            Assert.That(fs, Is.SameAs(byPath.FileSystem));
        }
    }

    [Test]
    public async Task Exists_ReturnsTrue_For_ExistingFile()
    {
        using var fs = GetFileSystem();

        Assert.That(await fs.DirectoryExistsAsync("/project/assets"), Is.True);
        Assert.That(await fs.FileExistsAsync("/project/README.md"), Is.True);
    }

    [Test]
    public async Task File_Exists_ReturnsFalse_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        Assert.That(await fs.FileExistsAsync($"/{Guid.NewGuid()}"), Is.False);
    }

    [Test]
    public async Task File_OpenRead_ReturnsReadableStream()
    {
        using var fs = GetFileSystem();
        await using var stream = await fs.OpenReadAsync("/project/README.md");

        Assert.That(stream.CanRead, Is.True);
        stream.ReadByte();
    }

    [Test]
    public async Task File_OpenRead_ThrowsException_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        await Assert.ThatAsync(async () => await fs.OpenReadAsync("/6b01ba26.txt"), Throws.Exception);
        await Assert.ThatAsync(async () => await fs.OpenReadAsync("/6b01ba26/6b01ba26.txt"), Throws.Exception);
    }

    [Test]
    public async Task File_OpenWrite_ReturnsWritableStream()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var text = $"Id:{Guid.NewGuid()}";

        await using (var stream = await fs.OpenWriteAsync("/project/README.md"))
        {
            Assert.That(stream.CanWrite, Is.True);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(text));
        }

        Assert.That(
            await fs.ReadAllTextAsync("/project/README.md"),
            Is.EqualTo(text));
    }

    [Test]
    public async Task File_OpenWrite_NewFile()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var path = "/project/6b01ba2608be";
        var text = $"Automatically generated on {DateTime.Now:s}\n\nNew Id:{Guid.NewGuid()}";

        await using (var stream = await fs.OpenWriteAsync(path))
        {
            Assert.That(stream.CanWrite, Is.True);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(text));
        }

        Assert.That(
            await fs.ReadAllTextAsync(path),
            Is.EqualTo(text));

        await fs.DeleteFileAsync(path);

        Assert.That(
            await fs.FileExistsAsync(path),
            Is.False);
    }

    [Test]
    public async Task File_Write_OverwritesContent_When_OverwriteIsTrue()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var file = fs.GetFile("/project/README.md");
        var text = $"Id:{Guid.NewGuid()}";

        var ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetBytes(text));
        ms.Position = 0;

        await file.WriteAsync(ms, overwrite: true);

        Assert.That(
            await file.ReadAllTextAsync(),
            Is.EqualTo(text));
    }

    [Test]
    public async Task File_Write_ThrowsException_For_ExistingFile_When_OverwriteIsFalse()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var file = fs.GetFile("/project/README.md");
        var text = await file.ReadAllTextAsync();

        var ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
        ms.Position = 0;

        Assert.That(
            () => file.WriteAsync(ms, overwrite: false),
            Throws.Exception);

        Assert.That(
            await file.ReadAllTextAsync(),
            Is.EqualTo(text));
    }

    [Test]
    public async Task File_Write_NewFile()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var path = "/project/0fce49e0.txt";
        var text = $"Automatically generated on {DateTime.Now:s}\n\nNew Id:{Guid.NewGuid()}";

        var ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetBytes(text));
        ms.Position = 0;

        var file = fs.GetFile(path);
        Assert.That(await file.ExistsAsync(), Is.False);

        await file.WriteAsync(ms);

        Assert.That(await file.ExistsAsync(), Is.True);
        Assert.That(await file.ReadAllTextAsync(), Is.EqualTo(text));

        await file.DeleteAsync();

        Assert.That(await file.ExistsAsync(), Is.False);
    }

    [Test]
    public async Task File_Delete_For_ExistingFile()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var path = "/project/793cd29d96c4.txt";
        var file = fs.GetFile(path);

        Assert.That(await file.ExistsAsync(), Is.False);

        await file.WriteAsync(Stream.Null);

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

        await fs.DeleteFileAsync("/1861cb25.txt");
        await fs.DeleteFileAsync("/1861cb25/1861cb25.txt");
    }

    [Test]
    public async Task File_Readonly_OpenWrite_ThrowsException()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        await Assert.ThatAsync(
            async () => { await using var stream = await fs.OpenWriteAsync("/project/README.md"); },
            Throws.Exception);
    }

    [Test]
    public async Task File_Readonly_OpenWrite_ThrowsException_For_NewFile()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        await Assert.ThatAsync(
            async () => await fs.OpenWriteAsync("/project/6d368a9d0e97.md"),
            Throws.Exception);
    }

    [Test]
    public async Task File_Readonly_Write_ThrowsException()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        await Assert.ThatAsync(
            async () => await fs.WriteAsync("/project/README.md", Stream.Null),
            Throws.Exception);
    }

    [Test]
    public async Task File_Readonly_Write_ThrowsException_For_NewFile()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        await Assert.ThatAsync(
            async () => await fs.WriteAsync("/project/9286d04f.pdf", Stream.Null),
            Throws.Exception);
    }

    [Test]
    public async Task File_Readonly_Delete_ThrowsException()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        await Assert.ThatAsync(
            async () => await fs.DeleteFileAsync("/project/README.md"),
            Throws.Exception);

        Assert.That(
            await fs.FileExistsAsync("/project/README.md"),
            Is.True);
    }

    [Test]
    public async Task File_Readonly_Delete_ThrowsException_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        await Assert.ThatAsync(
            async () => await fs.DeleteFileAsync("/project/c180408e8005.png"),
            Throws.Exception);
    }

    [Test]
    public async Task File_CopyTo_Path()
    {
        using var fs = GetFileSystem();
        if (fs.IsReadOnly)
            return;

        var sourcePath = "/project/README.md";
        var destinationPath = "/project/README - Copy.md";

        Assert.That(
            await fs.FileExistsAsync(destinationPath),
            Is.False);

        await fs.CopyFileAsync(sourcePath, destinationPath);
        Assert.That(
            await fs.FileExistsAsync(destinationPath),
            Is.True);

        Assert.That(
            await fs.ReadAllTextAsync(destinationPath),
            Is.EqualTo(await fs.ReadAllTextAsync(sourcePath)));

        await fs.DeleteFileAsync(destinationPath);
        Assert.That(
            await fs.FileExistsAsync(destinationPath),
            Is.False);
    }

    [Test]
    public async Task File_CopyTo_Path_NoOverwrite_ThrowsException()
    {
        using var fs = GetFileSystem();
        if (fs.IsReadOnly)
            return;

        var sourcePath = "/project/README.md";
        var destinationPath = "/project/README - Copy.md";

        await fs.CopyFileAsync(sourcePath, destinationPath);

        Assert.That(
            await fs.FileExistsAsync(destinationPath),
            Is.True);

        await Assert.ThatAsync(
            async () => await fs.CopyFileAsync(sourcePath, destinationPath),
            Throws.Exception);

        await fs.DeleteFileAsync(destinationPath);
        Assert.That(
            await fs.FileExistsAsync(destinationPath),
            Is.False);
    }

    [Test]
    public async Task File_CopyTo_File_SameFileSystems()
    {
        using var fs = GetFileSystem();
        if (fs.IsReadOnly)
            return;

        var source = fs.GetFile("/project/README.md");
        var destination = fs.GetFile("/project/README - Copy.md");

        Assert.That(
            await destination.ExistsAsync(),
            Is.False);

        await source.CopyToAsync(destination);

        Assert.That(
            await destination.ExistsAsync(),
            Is.True);

        Assert.That(
            await destination.ReadAllTextAsync(),
            Is.EqualTo(await source.ReadAllTextAsync()));

        await destination.DeleteAsync();
        Assert.That(
            await destination.ExistsAsync(),
            Is.False);
    }

    [Test]
    public async Task File_CopyTo_File_NotSameFileSystems()
    {
        using var fs1 = GetFileSystem();
        using var fs2 = GetFileSystem();

        if (fs1.IsReadOnly)
            return;

        var source = fs1.GetFile("/project/README.md");
        var destination = fs2.GetFile("/project/README - Copy.md");

        Assert.That(
            await destination.ExistsAsync(),
            Is.False);

        await source.CopyToAsync(destination);

        Assert.That(
            await destination.ExistsAsync(),
            Is.True);

        Assert.That(
            await destination.ReadAllTextAsync(),
            Is.EqualTo(await source.ReadAllTextAsync()));

        await destination.DeleteAsync();
        Assert.That(
            await destination.ExistsAsync(),
            Is.False);
    }

    [Test]
    public async Task File_CopyTo_File_NoOverwrite_ThrowsException()
    {
        using var fs = GetFileSystem();
        if (fs.IsReadOnly)
            return;

        var source = fs.GetFile("/project/README.md");
        var destination = fs.GetFile("/project/README - Copy.md");

        Assert.That(
            await destination.ExistsAsync(),
            Is.False);

        await source.CopyToAsync(destination);

        Assert.That(
            await destination.ExistsAsync(),
            Is.True);

        await Assert.ThatAsync(
            async () => await source.CopyToAsync(destination),
            Throws.Exception);

        await destination.DeleteAsync();
        Assert.That(
            await destination.ExistsAsync(),
            Is.False);
    }

    [Test]
    public async Task File_CopyTo_ThrowsException_When_CopyingToItself()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var file = fs.GetFile("/project/README.md");
        await Assert.ThatAsync(async () => await file.CopyToAsync(file.FullName), Throws.Exception);
        await Assert.ThatAsync(async () => await file.CopyToAsync(file), Throws.Exception);
        await Assert.ThatAsync(async () => await file.CopyToAsync(fs.GetFile(file.FullName)), Throws.Exception);
    }

    [Test]
    public async Task File_CopyTo_ThrowException_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        await Assert.ThatAsync(
            async () => await fs.CopyFileAsync("/project/800569b11aaa0438.txt", "/project/test-b11aaa.txt"),
            Throws.Exception);

        Assert.That(
            await fs.FileExistsAsync("/project/test-b11aaa.txt"),
            Is.False);
    }

    [Test]
    public async Task File_Readonly_CopyTo_ThrowException_For_NonExistingFile()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        await Assert.ThatAsync(
            async () => await fs.CopyFileAsync("/project/README.md", "/project/README - Copy.md"),
            Throws.Exception);

        Assert.That(
            await fs.FileExistsAsync("/project/README - Copy.md"),
            Is.False);
    }

    [Test]
    public async Task File_ReadAllBytes()
    {
        using var fs = GetFileSystem();

        var file = fs.GetFile("/project/README.md");
        using var reader = new BinaryReader(await file.OpenReadAsync());

        var bytes = await fs.ReadAllBytesAsync(file.FullName);
        var expected = reader.ReadBytes(4096);
        var properties = await file.GetPropertiesAsync();

        Assert.That(
            bytes.SequenceEqual(expected),
            Is.True);

        Assert.That(
            bytes.Length,
            Is.EqualTo(properties.Length));
    }

    [Test]
    public async Task File_ReadAllText()
    {
        using var fs = GetFileSystem();

        var file = fs.GetFile("/project/README.md");
        var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream);

        Assert.That(
            await fs.ReadAllTextAsync(file.FullName),
            Is.EqualTo(await reader.ReadToEndAsync()));
    }

    [Test]
    public async Task File_ReadAllLines()
    {
        using var fs = GetFileSystem();

        var file = fs.GetFile("/project/README.md");
        var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream);

        var lines = new List<string>();
        while (await reader.ReadLineAsync() is {} line)
            lines.Add(line);

        Assert.That(
            await fs.ReadAllLinesAsync(file.FullName),
            Is.EquivalentTo(lines));
    }

    [Test]
    public async Task File_WriteAllBytes()
    {
        using var fs = GetFileSystem();
        if (fs.IsReadOnly)
            return;


        var path = "/project/b02a67d8.bin";
        var data = RandomNumberGenerator.GetBytes(10_000);

        await fs.WriteAllBytesAsync(path, data);

        Assert.That(
            await fs.ReadAllBytesAsync(path),
            Is.EquivalentTo(data));

        await fs.DeleteFileAsync(path);
    }

    [Test]
    public async Task File_WriteAllText()
    {
        using var fs = GetFileSystem();
        if (fs.IsReadOnly)
            return;

        var list = new List<string>();
        for (var i = 0; i < 1000; i++)
            list.Add($"Hello, 世界! Unicode test: café, weiß, Привет, ёжик! こんにちは! {Guid.NewGuid()}");

        var data = string.Join(Environment.NewLine, list);
        var path = "/project/5cc34e90a7c9.txt";

        await fs.WriteAllTextAsync(path, data);

        Assert.That(
            await fs.ReadAllTextAsync(path),
            Is.EqualTo(data));

        await fs.DeleteFileAsync(path);
    }

    [Test]
    public async Task File_WriteAllLines()
    {
        using var fs = GetFileSystem();
        if (fs.IsReadOnly)
            return;

        var list = new List<string>();
        for (var i = 0; i < 1000; i++)
            list.Add($"Hello, 世界! Unicode test: café, weiß, Привет, ёжик! こんにちは! {Guid.NewGuid()}");

        var path = "/project/76bb51ee6cb7.txt";
        await fs.WriteAllLinesAsync(path, list);

        Assert.That(
            await fs.ReadAllLinesAsync(path),
            Is.EquivalentTo(list));

        await fs.DeleteFileAsync(path);
    }

    [Test]
    public async Task Directory_GetFiles()
    {
        using var fs = GetFileSystem();

        Assert.That(
            await fs.GetDirectory("/project/assets").GetFilesAsync().AnyAsync(),
            Is.False);

        Assert.That(
            await fs
                .GetDirectory("/project/assets/images")
                .GetFilesAsync()
                .Select(f => f.FullName)
                .OrderBy(p => p)
                .ToArrayAsync(),
            Is.EquivalentTo(
            [
                "/project/assets/images/icon.svg",
                "/project/assets/images/logo.png"
            ]));
    }

    [Test]
    public async Task Directory_GetDirectories()
    {
        using var fs = GetFileSystem();

        Assert.That(
            await fs.GetDirectory("/project/assets/fonts").GetDirectoriesAsync().AnyAsync(),
            Is.False);

        Assert.That(
            await fs
                .GetDirectory("/project/assets")
                .GetDirectoriesAsync()
                .Select(f => f.FullName)
                .OrderBy(p => p)
                .ToArrayAsync(),
            Is.EquivalentTo(
            [
                "/project/assets/fonts",
                "/project/assets/images",
                "/project/assets/styles"
            ])
        );
    }

    [Test]
    public async Task Directory_GetFileNodes()
    {
        using var fs = GetFileSystem();

        Assert.That(
            await fs.GetDirectory("/project/assets/scripts").GetFileNodesAsync().AnyAsync(),
            Is.False);

        Assert.That(
            await fs
                .GetDirectory("/project/assets/images")
                .GetFileNodesAsync()
                .Select(f => f.FullName)
                .OrderBy(p => p)
                .ToArrayAsync(),
            Is.EquivalentTo(
            [
                "/project/assets/images/backgrounds",
                "/project/assets/images/icon.svg",
                "/project/assets/images/logo.png"
            ])
        );
    }

    [Test]
    public async Task Directory_Glob_GetFiles()
    {
        using var fs = GetFileSystem();

        Assert.That(
            await fs.GetDirectory("/project/assets").GetFilesAsync("*/*.otf").AnyAsync(),
            Is.False);

        Assert.That(
            await fs
                .GetDirectory("/project")
                .GetFilesAsync(
                    pattern: "[a][s]set[s]/{images,styles}/{main.css,logo.png}")
                .Select(f => f.FullName)
                .OrderBy(p => p)
                .ToArrayAsync(),
            Is.EquivalentTo(
            [
                "/project/assets/images/logo.png",
                "/project/assets/styles/main.css"
            ])
        );

        Assert.That(
            await fs
                .GetDirectory("/project")
                .GetFilesAsync(
                    patterns: ["[a]sset{,s}/*/*.{svg,png}", "assets/{images,styles}/*.css"],
                    excludes: ["assets/imag*/*icon*", "**/style*/print.css"])
                .Select(f => f.FullName)
                .OrderBy(p => p)
                .ToArrayAsync(),
            Is.EquivalentTo(
            [
                "/project/assets/images/logo.png",
                "/project/assets/styles/main.css"
            ])
        );
    }

    [Test]
    public async Task Directory_Glob_GetDirectories()
    {
        using var fs = GetFileSystem();

        Assert.That(
            await fs.GetDirectory("/project/assets").GetDirectoriesAsync("{styles,fonts}/*").AnyAsync(),
            Is.False);

        Assert.That(
            await fs
                .GetDirectory("/project")
                .GetDirectoriesAsync("[a]sset{,s}/*/*")
                .Select(f => f.FullName)
                .OrderBy(p => p)
                .ToArrayAsync(),
            Is.EquivalentTo(["/project/assets/images/backgrounds"]));

        Assert.That(
            await fs
                .GetDirectory("/project")
                .GetDirectoriesAsync(
                    patterns: ["[a]sset{,s}/*/*", "*/**/*s"],
                    excludes: ["src/**", "tests/**/Fixtures"])
                .Select(f => f.FullName)
                .OrderBy(p => p)
                .ToArrayAsync(),
            Is.EquivalentTo(
            [
                "/project/assets/fonts",
                "/project/assets/images",
                "/project/assets/images/backgrounds",
                "/project/assets/styles"
            ])
        );
    }

    [Test]
    public async Task Directory_Glob_GetFileNodes()
    {
        using var fs = GetFileSystem();

        Assert.That(
            await fs.GetDirectory("/project/assets").GetFileNodesAsync("{styles,fonts}/*/*").AnyAsync(),
            Is.False);

        Assert.That(
            await fs
                .GetDirectory("/project")
                .GetFileNodesAsync("[a]sset{,s}/*/*")
                .Select(f => f.FullName)
                .OrderBy(p => p)
                .ToArrayAsync(),
            Is.EquivalentTo(
            [
                "/project/assets/fonts/Arial.ttf",
                "/project/assets/fonts/Roboto.ttf",
                "/project/assets/images/backgrounds",
                "/project/assets/images/icon.svg",
                "/project/assets/images/logo.png",
                "/project/assets/styles/main.css",
                "/project/assets/styles/print.css"
            ]));

        Assert.That(
            await fs
                .GetDirectory("/project")
                .GetFileNodesAsync(
                    patterns: ["[a]sset{,s}/*/*", "*/**/*s"],
                    excludes: ["src/**", "tests/**"])
                .Select(f => f.FullName)
                .OrderBy(p => p)
                .ToArrayAsync(),
            Is.EquivalentTo(
            [
                "/project/assets/fonts",
                "/project/assets/fonts/Arial.ttf",
                "/project/assets/fonts/Roboto.ttf",
                "/project/assets/images",
                "/project/assets/images/backgrounds",
                "/project/assets/images/icon.svg",
                "/project/assets/images/logo.png",
                "/project/assets/styles",
                "/project/assets/styles/main.css",
                "/project/assets/styles/print.css"
            ])
        );
    }

    [Test]
    public async Task Directory_Enumerate_ReturnsEmpty_For_NonExistingDirectory()
    {
        using var fs = GetFileSystem();

        var directory = fs.GetDirectory("/project/b02a67d85cc34e90");

        Assert.That(await directory.GetFileNodesAsync().AnyAsync(), Is.False);
        Assert.That(await directory.GetFilesAsync().AnyAsync(), Is.False);
        Assert.That(await directory.GetDirectoriesAsync().AnyAsync(), Is.False);
    }

    [Test]
    public async Task Directory_Create_For_ExistingDirectory()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        await fs.CreateDirectoryAsync("/project/assets");
    }

    [Test]
    public async Task Directory_Create_For_NonExistingDirectory()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        await fs.CreateDirectoryAsync("/project/dcc2d926/8ddb/4a79/8246/a60ed6146c53");

        Assert.That(
            await fs.DirectoryExistsAsync("/project/dcc2d926/8ddb/4a79/8246/a60ed6146c53"),
            Is.True);

        await fs.DeleteDirectoryAsync("/project/dcc2d926");
    }

    [Test]
    public async Task Directory_Delete()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var directory = fs.GetDirectory("/project/1111b18e");

        await fs.WriteAsync("/project/1111b18e/4bb69411.txt", Stream.Null);
        await fs.WriteAsync("/project/1111b18e/f1e5f79eb6be/909c4bb69411.txt", Stream.Null);

        Assert.That(
            await directory.GetFilesAsync("**").CountAsync(),
            Is.EqualTo(2));

        await directory.DeleteAsync();

        Assert.That(
            await directory.GetFilesAsync("**").AnyAsync(),
            Is.False);
    }

    [Test]
    public async Task Directory_Delete_For_NonExistingDirectory()
    {
        using var fs = GetFileSystem();

        if (fs.IsReadOnly)
            return;

        var directory = fs.GetDirectory("/project/73d0e99e");

        Assert.That(
            await directory.GetFileNodesAsync().AnyAsync(),
            Is.False);

        await directory.DeleteAsync();
    }

    [Test]
    public async Task Directory_Readonly_Delete_ThrowsException_For_NonExistingDirectory()
    {
        using var fs = GetFileSystem();

        if (!fs.IsReadOnly)
            return;

        await Assert.ThatAsync(
            async () => await fs.DeleteDirectoryAsync("/project/22ef456"),
            Throws.Exception);
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
}
