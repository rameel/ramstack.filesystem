using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Physical;

[TestFixture]
public class WriteablePhysicalFileSystemSpecificationTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeTearDown]
    public void Cleanup() =>
        _storage.Dispose();

    /// <inheritdoc />
    protected override IVirtualFileSystem GetFileSystem() =>
        new PhysicalFileSystem(_storage.Root, ExclusionFilters.None);

    /// <inheritdoc />
    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);
}
