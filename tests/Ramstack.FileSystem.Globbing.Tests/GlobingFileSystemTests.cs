using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Globbing;

[TestFixture]
public class GlobingFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage1;
    private readonly TempFileStorage _storage2;

    public GlobingFileSystemTests()
    {
        _storage1 = new TempFileStorage();
        _storage2 = new TempFileStorage(_storage1);
    }

    [OneTimeSetUp]
    public void Setup()
    {
        var directory = new DirectoryInfo(Path.Join(_storage1.Root, "project"));

        foreach (var di in directory.GetDirectories("*", SearchOption.TopDirectoryOnly))
            if (di.Name != "assets")
                di.Delete(recursive: true);

        foreach (var fi in directory.GetFiles("*", SearchOption.TopDirectoryOnly))
            if (fi.Name != "README.md")
                fi.Delete();
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        _storage1.Dispose();
        _storage2.Dispose();
    }

    [Test]
    public async Task Glob_MatchStructures()
    {
        using var fs = GetFileSystem();

        Assert.That(
            await fs
                .GetDirectoriesAsync("/", "**")
                .OrderBy(f => f.FullName)
                .Select(f => f.FullName)
                .ToArrayAsync(),
            Is.EquivalentTo(
            [
                "/project",
                "/project/assets",
                "/project/assets/fonts",
                "/project/assets/images",
                "/project/assets/images/backgrounds",
                "/project/assets/styles"
            ]));

        Assert.That(
            await fs
                .GetFilesAsync("/", "**")
                .OrderBy(f => f.FullName)
                .Select(f => f.FullName)
                .ToArrayAsync(),
            Is.EquivalentTo(
            [
                "/project/assets/fonts/Arial.ttf",
                "/project/assets/fonts/Roboto.ttf",
                "/project/assets/images/backgrounds/dark.jpeg",
                "/project/assets/images/backgrounds/light.jpg",
                "/project/assets/images/icon.svg",
                "/project/assets/images/logo.png",
                "/project/assets/styles/main.css",
                "/project/assets/styles/print.css",
                "/project/README.md"
            ]));
    }

    [Test]
    public async Task ExcludedDirectory_HasNoFileNodes()
    {
        using var fs = new GlobbingFileSystem(
            new PhysicalFileSystem(_storage2.Root),
            pattern: "**",
            exclude: "/project/src/**");

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
            Assert.That(
                await fs.DirectoryExistsAsync(path),
                Is.False);

            Assert.That(
                await fs.GetFileNodesAsync(path, "**").AnyAsync(),
                Is.False);
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
            Assert.That(await fs.FileExistsAsync(path), Is.False);
    }

    protected override IVirtualFileSystem GetFileSystem()
    {
        var fs = new PhysicalFileSystem(_storage2.Root);
        return new GlobbingFileSystem(fs,
            patterns: ["project/assets/**", "project/README.md", "project/global.json"],
            excludes: ["project/*.json"]);
    }

    protected override DirectoryInfo GetDirectoryInfo() =>
        new(_storage1.Root);
}
