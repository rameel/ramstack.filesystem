using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Physical;

[TestFixture]
public class WriteablePhysicalFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [Test]
    public async Task Directory_GetProperties_ReturnsConsistentTimezones()
    {
        using var fs = GetFileSystem();

        var directory = await fs
            .GetDirectory("/project")
            .GetPropertiesAsync();

        Assert.That(
            directory.LastAccessTime.Offset,
            Is.EqualTo(directory.LastWriteTime.Offset));

        Assert.That(
            directory.CreationTime.Offset,
            Is.EqualTo(directory.LastWriteTime.Offset));
    }

    [Test]
    public async Task File_GetProperties_ReturnsConsistentTimezones()
    {
        using var fs = GetFileSystem();

        var file = await fs
            .GetFile("/project/docs/api_reference.md")
            .GetPropertiesAsync();

        Assert.That(
            file.LastAccessTime.Offset,
            Is.EqualTo(file.LastWriteTime.Offset));

        Assert.That(
            file.CreationTime.Offset,
            Is.EqualTo(file.LastWriteTime.Offset));
    }

    [OneTimeTearDown]
    public void Cleanup() =>
        _storage.Dispose();

    /// <inheritdoc />
    protected override IVirtualFileSystem GetFileSystem() =>
        new PhysicalFileSystem(_storage.Root);

    /// <inheritdoc />
    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);
}
