using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Prefixed;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Composite;

[TestFixture]
public class CompositeFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();
    private readonly CompositeFileSystem _fs;

    public CompositeFileSystemTests()
    {
        var root = Path.Join(_storage.Root, "project");

        foreach (var file in Directory.GetFiles(root))
            File.Delete(file);

        var list = new List<IVirtualFileSystem>();
        foreach (var directory in Directory.GetDirectories(root))
        {
            var fileName = Path.GetFileName(directory);
            Console.WriteLine(fileName);

            var fs = new PrefixedFileSystem(fileName, new PhysicalFileSystem(directory, ExclusionFilters.None));
            list.Add(fs);
        }

        _fs = new CompositeFileSystem(list);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        _fs.Dispose();
    }

    protected override IVirtualFileSystem GetFileSystem() =>
        _fs;

    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(Path.Join(_storage.Root, "project"));
}
