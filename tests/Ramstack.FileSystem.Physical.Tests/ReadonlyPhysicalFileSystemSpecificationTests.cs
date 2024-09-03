using Ramstack.FileSystem.Null;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Physical;

[TestFixture]
public class ReadonlyPhysicalFileSystemSpecificationTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeTearDown]
    public void Cleanup() =>
        _storage.Dispose();

    [TestCase(".dot/", ExclusionFilters.DotPrefixed)]
    [TestCase(".dot/00/", ExclusionFilters.DotPrefixed)]
    [TestCase(".dot/00/data.txt", ExclusionFilters.DotPrefixed)]
    [TestCase(".editorconfig", ExclusionFilters.DotPrefixed)]

    [TestCase("system/", ExclusionFilters.System)]
    [TestCase("system/00/", ExclusionFilters.System)]
    [TestCase("system/00/data.txt", ExclusionFilters.System)]
    [TestCase("system.bin", ExclusionFilters.System)]

    [TestCase("hidden/", ExclusionFilters.Hidden)]
    [TestCase("hidden/00/", ExclusionFilters.Hidden)]
    [TestCase("hidden/00/data.txt", ExclusionFilters.Hidden)]
    [TestCase("hidden.bin", ExclusionFilters.Hidden)]

    [TestCase(".dot/", ExclusionFilters.Sensitive)]
    [TestCase(".dot/00/", ExclusionFilters.Sensitive)]
    [TestCase(".dot/00/data.txt", ExclusionFilters.Sensitive)]
    [TestCase(".editorconfig", ExclusionFilters.Sensitive)]
    [TestCase("system/", ExclusionFilters.Sensitive)]
    [TestCase("system/00/", ExclusionFilters.Sensitive)]
    [TestCase("system/00/data.txt", ExclusionFilters.Sensitive)]
    [TestCase("system.bin", ExclusionFilters.Sensitive)]
    [TestCase("hidden/", ExclusionFilters.Sensitive)]
    [TestCase("hidden/00/", ExclusionFilters.Sensitive)]
    [TestCase("hidden/00/data.txt", ExclusionFilters.Sensitive)]
    [TestCase("hidden.bin", ExclusionFilters.Sensitive)]
    public void ExclusionFilters_Exclude_Matching(string path, ExclusionFilters exclusionFilters)
    {
        if (Path.DirectorySeparatorChar == '/')
            return;

        var root = InitializeSensitiveFiles();
        try
        {
            var fs = new PhysicalFileSystem(root, exclusionFilters);
            if (path.EndsWith('/'))
            {
                var directory = fs.GetDirectory(path);
                Assert.That(directory, Is.InstanceOf<NotFoundDirectory>());
            }
            else
            {
                var file = fs.GetFile(path);
                Assert.That(file, Is.InstanceOf<NotFoundFile>());
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestCase(".dot/", ExclusionFilters.Hidden)]
    [TestCase(".dot/00/", ExclusionFilters.Hidden)]
    [TestCase(".dot/00/data.txt", ExclusionFilters.Hidden)]
    [TestCase(".editorconfig", ExclusionFilters.Hidden)]

    [TestCase("system/", ExclusionFilters.DotPrefixed)]
    [TestCase("system/00/", ExclusionFilters.DotPrefixed)]
    [TestCase("system/00/data.txt", ExclusionFilters.DotPrefixed)]
    [TestCase("system.bin", ExclusionFilters.DotPrefixed)]

    [TestCase("hidden/", ExclusionFilters.System)]
    [TestCase("hidden/00/", ExclusionFilters.System)]
    [TestCase("hidden/00/data.txt", ExclusionFilters.System)]
    [TestCase("hidden.bin", ExclusionFilters.System)]
    public void ExclusionFilters_NotExclude_NonMatching(string path, ExclusionFilters exclusionFilters)
    {
        if (Path.DirectorySeparatorChar == '/')
            return;

        var root = InitializeSensitiveFiles();
        try
        {
            var fs = new PhysicalFileSystem(root, exclusionFilters);
            if (path.EndsWith('/'))
            {
                var directory = fs.GetDirectory(path);
                Assert.That(directory, Is.Not.InstanceOf<NotFoundDirectory>());
            }
            else
            {
                var file = fs.GetFile(path);
                Assert.That(file, Is.Not.InstanceOf<NotFoundFile>());
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <inheritdoc />
    protected override IVirtualFileSystem GetFileSystem()
    {
        return new PhysicalFileSystem(_storage.Root, ExclusionFilters.None)
        {
            IsReadOnly = true
        };
    }

    /// <inheritdoc />
    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);

    private string InitializeSensitiveFiles()
    {
        var list = new[]
        {
            ".editorconfig",
            ".dot/00/data.txt",
            "system.bin",
            "system/00/data.txt",
            "hidden.bin",
            "hidden/00/data.txt"
        };

        var root = Path.Combine(_storage.Root, "0000");
        Directory.CreateDirectory(root);

        foreach (var path in list.Select(p => Path.Combine(root, p)))
        {
            var directory = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(directory);

            File.WriteAllText(path, "");
        }

        foreach (var path in Directory.GetFileSystemEntries(root))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            switch (name)
            {
                case "system": File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.System); break;
                case "hidden": File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden); break;
            }
        }

        return root;
    }
}
