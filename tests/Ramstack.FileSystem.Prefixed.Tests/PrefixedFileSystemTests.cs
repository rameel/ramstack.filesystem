using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Prefixed;

[TestFixture]
public class PrefixedFileSystemTests() : VirtualFileSystemSpecificationTests(Prefix)
{
    private const string Prefix = "solution/app";

    private readonly TempFileStorage _storage = new TempFileStorage(Prefix);

    protected override IVirtualFileSystem GetFileSystem() =>
        new PrefixedFileSystem(Prefix, new PhysicalFileSystem(_storage.PrefixedPath, ExclusionFilters.None));

    /// <inheritdoc />
    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);
}
