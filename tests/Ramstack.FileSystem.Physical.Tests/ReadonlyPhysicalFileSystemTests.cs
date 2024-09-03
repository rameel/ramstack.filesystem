using Ramstack.FileSystem.Null;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Physical;

[TestFixture]
public class ReadonlyPhysicalFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeTearDown]
    public void Cleanup() =>
        _storage.Dispose();

    [TestCase(".dot/")]
    [TestCase(".dot/00/")]
    [TestCase(".dot/00/data.txt")]
    [TestCase(".editorconfig")]
    [TestCase("system/")]
    [TestCase("system/00/")]
    [TestCase("system/00/data.txt")]
    [TestCase("system.bin")]
    [TestCase("hidden/")]
    [TestCase("hidden/00/")]
    [TestCase("hidden/00/data.txt")]
    [TestCase("hidden.bin")]
    public void ExclusionFilters_Exclude_Matching(string path)
    {
        if (Path.DirectorySeparatorChar == '/' && !path.StartsWith('.'))
            return;

        var root = InitializeSensitiveFiles();
        try
        {
            // ReSharper disable once RedundantArgumentDefaultValue
            var fs = new PhysicalFileSystem(root, ExclusionFilters.Sensitive);
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

        if (Path.DirectorySeparatorChar != '/')
        {
            foreach (var path in Directory.GetFileSystemEntries(root))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                switch (name)
                {
                    case "system":
                        File.SetAttributes(path, FileAttributes.System);
                        break;
                    case "hidden":
                        File.SetAttributes(path, FileAttributes.Hidden);
                        break;
                }
            }
        }

        return root;
    }
}
