using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Sub;

[TestFixture]
public class SubFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeTearDown]
    public void Cleanup() =>
        _storage.Dispose();

    protected override IVirtualFileSystem GetFileSystem() =>
        new SubFileSystem("project/docs", new PhysicalFileSystem(_storage.Root, ExclusionFilters.None));

    protected override DirectoryInfo GetDirectoryInfo() =>
        new(Path.Join(_storage.Root, "project", "docs"));
}
