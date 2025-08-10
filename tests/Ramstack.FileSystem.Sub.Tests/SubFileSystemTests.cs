using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Prefixed;
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
        new SubFileSystem("/bin/app",
            new PrefixedFileSystem("/bin/app",
                new PhysicalFileSystem(_storage.Root)));

    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);
}
