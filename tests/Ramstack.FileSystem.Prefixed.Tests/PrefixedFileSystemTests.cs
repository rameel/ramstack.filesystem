using System.Reflection;

using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Prefixed;

[TestFixture]
public class PrefixedFileSystemTests : VirtualFileSystemSpecificationTests
{
    private static readonly Func<string, string, string?> s_unwrapPrefix =
        typeof(PrefixedFileSystem)
            .GetMethod("TryUnwrapPrefix", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<string, string, string?>>();

    private readonly TempFileStorage _storage = new TempFileStorage();

    [TestCase("/", "/",            ExpectedResult = "/")]
    [TestCase("/", "/foo",         ExpectedResult = "/foo")]
    [TestCase("/", "/a/b/c",       ExpectedResult = "/a/b/c")]

    [TestCase("/a/b", "/a/b",      ExpectedResult = "/")]

    [TestCase("/a/b", "/a/b/c",    ExpectedResult = "/c")]
    [TestCase("/a/b", "/a/b/c/d",  ExpectedResult = "/c/d")]

    [TestCase("/a/b", "/a/bc",     ExpectedResult = null)]

    [TestCase("/a/b", "/a/c",      ExpectedResult = null)]
    [TestCase("/a/b", "/a",        ExpectedResult = null)]
    public string? UnwrapPrefix(string prefix, string path) =>
        s_unwrapPrefix(path, prefix);

    [Test]
    public async Task File_RootPrefix_DelegatesToInnerProvider()
    {
        using var fs = new PrefixedFileSystem("/",
            new PhysicalFileSystem(_storage.Root));

        var file = fs.GetFile("/project/README.md");
        Assert.That(await file.ExistsAsync(), Is.True);
    }

    [Test]
    public async Task File_RootPrefix_MissingFile_ReturnsNotFound()
    {
        using var fs = new PrefixedFileSystem("/",
            new PhysicalFileSystem(_storage.Root));

        var file = fs.GetFile("/project/nonexistent.txt");
        Assert.That(await file.ExistsAsync(), Is.False);
    }

    [Test]
    public async Task Directory_RootPrefix_DelegatesToInnerProvider()
    {
        using var fs = new PrefixedFileSystem("/",
            new PhysicalFileSystem(_storage.Root));

        var file = fs.GetDirectory("/project");
        Assert.That(await file.ExistsAsync(), Is.True);
    }

    [Test]
    public async Task File_Create_InsideArtificialDirectory_ThrowsException()
    {
        using var fs = GetFileSystem();

        await Assert.ThatAsync(
            async () => await fs.WriteAsync("/ea5fd219.txt", Stream.Null),
            Throws.Exception);

        Assert.That(
            await fs.FileExistsAsync("/ea5fd219.txt"),
            Is.False);
    }

    [Test]
    public async Task Directory_Delete_ArtificialDirectory_ThrowsException()
    {
        using var storage = new TempFileStorage();
        using var fs = new PrefixedFileSystem("/bin/apps/myapp1", new PhysicalFileSystem(storage.Root));

        await Assert.ThatAsync(
            async () => await fs.DeleteDirectoryAsync("/"),
            Throws.Exception);

        await Assert.ThatAsync(
            async () => await fs.DeleteDirectoryAsync("/bin"),
            Throws.Exception);

        await Assert.ThatAsync(
            async () => await fs.DeleteDirectoryAsync("/bin/apps"),
            Throws.Exception);

        Assert.That(
            await fs.FileExistsAsync("/bin/apps/myapp1/project/README.md"),
            Is.True);

        await Assert.ThatAsync(
            async () => await fs.DeleteDirectoryAsync("/bin/apps/myapp1"),
            Throws.Nothing);

        Assert.That(
            await fs.FileExistsAsync("/bin/apps/myapp1/project/README.md"),
            Is.False);
    }

    [OneTimeTearDown]
    public void Cleanup() =>
        _storage.Dispose();

    protected override IVirtualFileSystem GetFileSystem() =>
        new PrefixedFileSystem("/project",
            new PhysicalFileSystem(
                Path.Join(_storage.Root, "project")));

    /// <inheritdoc />
    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);
}
