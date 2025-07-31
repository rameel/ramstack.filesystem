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

    [Test]
    public async Task ExcludedDirectory_HasNoFileNodes()
    {
        using var storage = new TempFileStorage();
        using var fs = new GlobbingFileSystem(new PhysicalFileSystem(storage.Root), "**", exclude: "/project/src/**");

        var directories = new[]
        {
            "/project/src",
            "/project/src/App",
            "/project/src/Modules",
            "/project/src/Modules/Module1",
            "/project/src/Modules/Module1/Submodule",
            "/project/src/Modules/Module2"
        };

        foreach (var path in directories)
        {
            var directory = fs.GetDirectory(path);

            Assert.That(
                await directory.ExistsAsync(),
                Is.False);

            Assert.That(
                await directory.GetFileNodesAsync("**").CountAsync(),
                Is.Zero);
        }

        var files = new[]
        {
            "/project/src/App/App.csproj",
            "/project/src/App/Program.cs",
            "/project/src/App/Utils.cs",
            "/project/src/Modules/Module1/Module1.cs",
            "/project/src/Modules/Module1/Module1.csproj",
            "/project/src/Modules/Module1/Submodule/Submodule1.cs",
            "/project/src/Modules/Module1/Submodule/Submodule2.cs",
            "/project/src/Modules/Module1/Submodule/Submodule.csproj",
            "/project/src/Modules/Module2/Module2.cs",
            "/project/src/Modules/Module2/Module2.csproj",
            "/project/src/App.sln"
        };

        foreach (var path in files)
        {
            var file = fs.GetFile(path);
            Assert.That(await file.ExistsAsync(), Is.False);
        }
    }

    protected override IVirtualFileSystem GetFileSystem()
    {
        var fs = new PhysicalFileSystem(_storage.Root);
        return new GlobbingFileSystem(fs, "project/docs/**", exclude: "**/*.txt");
    }

    protected override DirectoryInfo GetDirectoryInfo() =>
        new(_storage.Root);
}
