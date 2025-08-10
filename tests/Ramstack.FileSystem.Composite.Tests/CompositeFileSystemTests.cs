using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Prefixed;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Composite;

[TestFixture]
public class CompositeFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage1;
    private readonly TempFileStorage _storage2;
    private readonly TempFileStorage _storage3;
    private readonly CompositeFileSystem _fs;

    public CompositeFileSystemTests()
    {
        _storage1 = new TempFileStorage();
        _storage2 = new TempFileStorage(_storage1);
        _storage3 = new TempFileStorage(_storage1);

        foreach (var directory in Directory.GetDirectories(Path.Join(_storage2.Root, "project")))
            Directory.Delete(directory, recursive: true);

        foreach (var file in Directory.GetFiles(Path.Join(_storage3.Root, "project")))
            File.Delete(file);

        var list = new List<IVirtualFileSystem>
        {
            new PhysicalFileSystem(_storage2.Root)
        };

        foreach (var directory in Directory.GetDirectories(Path.Join(_storage3.Root, "project")))
        {
            var fs = new PrefixedFileSystem(
                $"/project/{Path.GetFileName(directory)}",
                new PhysicalFileSystem(directory)
                );
            list.Add(fs);
        }

        _fs = new CompositeFileSystem(list);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        _fs.Dispose();
        _storage1.Dispose();
        _storage2.Dispose();
        _storage3.Dispose();
    }

    protected override IVirtualFileSystem GetFileSystem() =>
        _fs;

    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage1.Root);
}
