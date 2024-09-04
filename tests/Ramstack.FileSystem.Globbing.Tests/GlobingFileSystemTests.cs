using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Globbing;

[TestFixture]
public class GlobingFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeSetUp]
    public void Setup()
    {
        var path = Path.Join(_storage.Root, "project");
        var directory = new DirectoryInfo(path);

        foreach (var di in directory.GetDirectories("*", SearchOption.TopDirectoryOnly))
            if (di.Name != "docs")
                di.Delete(recursive: true);

        foreach (var fi in directory.GetFiles("*", SearchOption.TopDirectoryOnly))
            fi.Delete();

        File.Delete(Path.Join(_storage.Root, "project/docs/troubleshooting/common_issues.txt"));
    }

    [OneTimeTearDown]
    public void Cleanup() =>
        _storage.Dispose();

    protected override IVirtualFileSystem GetFileSystem()
    {
        var fs = new PhysicalFileSystem(_storage.Root);
        return new GlobbingFileSystem(fs, "project/docs/**", exclude: "**/*.txt");
    }

    protected override DirectoryInfo GetDirectoryInfo() =>
        new(_storage.Root);
}
