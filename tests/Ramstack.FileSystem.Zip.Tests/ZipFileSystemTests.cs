using System.IO.Compression;

using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Zip;

[TestFixture]
public class ZipFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly TempFileStorage _storage = new();

    [OneTimeSetUp]
    public void Setup()
    {
        ZipFile.CreateFromDirectory(_storage.Root, _path, CompressionLevel.Optimal, includeBaseDirectory: false);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        _storage.Dispose();
        File.Delete(_path);
    }

    /// <inheritdoc />
    protected override IVirtualFileSystem GetFileSystem() =>
        new ZipFileSystem(_path);

    /// <inheritdoc />
    protected override DirectoryInfo GetDirectoryInfo() =>
        new(_storage.Root);
}
