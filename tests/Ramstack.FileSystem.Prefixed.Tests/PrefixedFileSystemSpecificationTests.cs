using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Prefixed;

[TestFixture]
public class PrefixedFileSystemSpecificationTests() : VirtualFileSystemSpecificationTests("/solution/app")
{
    private readonly TempFileStorage _storage = new();

    protected override IVirtualFileSystem GetFileSystem() =>
        new PrefixedFileSystem("/solution/app", new PhysicalFileSystem(_storage.Root));

    /// <inheritdoc />
    protected override DirectoryInfo GetDirectoryInfo() =>
        new(_storage.Root);
}
