using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Readonly;

[TestFixture]
public class ReadOnlyFileSystemSpecificationTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new();

    [OneTimeTearDown]
    public void Cleanup() =>
        _storage.Dispose();

    /// <inheritdoc />
    protected override IVirtualFileSystem GetFileSystem() =>
        new ReadonlyFileSystem(new PhysicalFileSystem(_storage.Root));

    /// <inheritdoc />
    protected override DirectoryInfo GetDirectoryInfo() =>
        new(_storage.Root);
}
