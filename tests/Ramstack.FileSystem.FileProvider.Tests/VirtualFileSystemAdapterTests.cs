using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Adapters;

[TestFixture]
public class VirtualFileSystemAdapterTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeTearDown]
    public void Cleanup() =>
        _storage.Dispose();

    /// <inheritdoc />
    protected override IVirtualFileSystem GetFileSystem() =>
        new VirtualFileSystemAdapter(new PhysicalFileProvider(_storage.Root, ExclusionFilters.None));

    /// <inheritdoc />
    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);
}
