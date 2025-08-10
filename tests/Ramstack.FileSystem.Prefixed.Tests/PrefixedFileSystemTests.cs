using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Prefixed;

[TestFixture]
public class PrefixedFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();


    protected override IVirtualFileSystem GetFileSystem() =>
        new PrefixedFileSystem("/project",
            new PhysicalFileSystem(
                Path.Join(_storage.Root, "project")));

    /// <inheritdoc />
    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);
}
