using System.IO.Compression;

using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Ramstack.FileSystem.Zip;

[TestFixture]
public class ZipFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly TempFileStorage _storage = new();

    [OneTimeSetUp]
    public void Setup()
    {
        ZipFile.CreateFromDirectory(_storage.Root, _path, CompressionLevel.Optimal, includeBaseDirectory: false);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        _storage.Dispose();
        File.Delete(_path);
    }

    [Test]
    public async Task ZipArchive_WithIdenticalNameEntries()
    {
        using var fs = new ZipFileSystem(CreateArchive());

        var list = await fs
            .GetFilesAsync("/1")
            .ToArrayAsync();

        Assert.That(
            list.Length,
            Is.EqualTo(1));

        Assert.That(
            await list[0].ReadAllBytesAsync(),
            Is.EquivalentTo("Hello, World!"u8.ToArray()));

        static MemoryStream CreateArchive()
        {
            var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var a = archive.CreateEntry("1/text.txt");
                using (var writer = a.Open())
                    writer.Write("Hello, World!"u8);

                archive.CreateEntry("1/text.txt");
                archive.CreateEntry(@"1\text.txt");
            }

            stream.Position = 0;
            return stream;
        }
    }

    [Test]
    public async Task ZipArchive_PrefixedEntries()
    {
        var archive = new ZipArchive(CreateArchive(), ZipArchiveMode.Read, leaveOpen: true);
        using var fs = new ZipFileSystem(archive);

        var directories = await fs
            .GetDirectoriesAsync("/", "**")
            .Select(f =>
                f.FullName)
            .OrderBy(f => f)
            .ToArrayAsync();

        var files = await fs
            .GetFilesAsync("/", "**")
            .Select(f =>
                f.FullName)
            .OrderBy(f => f)
            .ToArrayAsync();

        Assert.That(files, Is.EquivalentTo(
        [
            "/1/text.txt",
            "/2/text.txt",
            "/3/text.txt",
            "/4/text.txt",
            "/5/text.txt",
            "/localhost/backup/text.txt",
            "/localhost/share/text.txt",
            "/server/backup/text.txt",
            "/server/share/text.txt",
            "/text.txt",
            "/text.xml"
        ]));

        Assert.That(directories, Is.EquivalentTo(
        [
            "/1",
            "/2",
            "/3",
            "/4",
            "/5",
            "/localhost",
            "/localhost/backup",
            "/localhost/share",
            "/server",
            "/server/backup",
            "/server/share"
        ]));

        static MemoryStream CreateArchive()
        {
            var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                archive.CreateEntry(@"D:\1/text.txt");
                archive.CreateEntry(@"D:2\text.txt");

                archive.CreateEntry(@"\\?\D:\text.txt");
                archive.CreateEntry(@"\\?\D:text.xml");
                archive.CreateEntry(@"\\.\D:\3\text.txt");
                archive.CreateEntry(@"//?/D:/4\text.txt");
                archive.CreateEntry(@"//./D:\5/text.txt");

                archive.CreateEntry(@"\\?\UNC\localhost\share\text.txt");
                archive.CreateEntry(@"\\.\unc\server\share\text.txt");
                archive.CreateEntry(@"//?/UNC/localhost/backup\text.txt");
                archive.CreateEntry(@"//./unc/server/backup\text.txt");
            }

            stream.Position = 0;
            return stream;
        }
    }

    [Test]
    public async Task ZipArchive_Directories()
    {
        using var fs = new ZipFileSystem(CreateArchive());

        var directories = await fs
            .GetDirectoriesAsync("/", "**")
            .Select(f =>
                f.FullName)
            .OrderBy(f => f)
            .ToArrayAsync();

        Assert.That(directories, Is.EquivalentTo(
        [
            "/1",
            "/2",
            "/2/3",
            "/4",
            "/4/5",
            "/4/5/6"
        ]));

        static MemoryStream CreateArchive()
        {
            var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                archive.CreateEntry(@"\1/");
                archive.CreateEntry(@"\2/");
                archive.CreateEntry(@"/2\");
                archive.CreateEntry(@"/2\");
                archive.CreateEntry(@"/2\");
                archive.CreateEntry(@"/2\3/");
                archive.CreateEntry(@"/2\3/");
                archive.CreateEntry(@"/2\3/");
                archive.CreateEntry(@"4\5/6\");
            }

            stream.Position = 0;
            return stream;
        }
    }


    /// <inheritdoc />
    protected override IVirtualFileSystem GetFileSystem() =>
        new ZipFileSystem(_path);

    /// <inheritdoc />
    protected override DirectoryInfo GetDirectoryInfo() =>
        new(_storage.Root);
}
