using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Prefixed;

[TestFixture]
public class PrefixedFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

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
