using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Physical;

[TestFixture]
public class ReadonlyPhysicalFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeTearDown]
    public void Cleanup() =>
        _storage.Dispose();

    /// <inheritdoc />
    protected override IVirtualFileSystem GetFileSystem()
    {
        return new PhysicalFileSystem(_storage.Root)
        {
            IsReadOnly = true
        };
    }

    /// <inheritdoc />
    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);
}
