using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Azure;

[TestFixture]
[Category("Cloud:Azure")]
public class WritableAzureFileSystemSpecificationTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeSetUp]
    public async Task Setup()
    {
        using var fs = GetFileSystem();

        await fs.CreateContainerAsync();

        foreach (var path in Directory.EnumerateFiles(_storage.Root, "*", SearchOption.AllDirectories))
        {
            await using var stream = File.OpenRead(path);
            await fs.WriteFileAsync(path[_storage.Root.Length..], stream, overwrite: true);
        }
    }

    [OneTimeTearDown]
    public async Task Cleanup()
    {
        _storage.Dispose();

        using var fs = GetFileSystem();
        await fs.DeleteDirectoryAsync("/");
    }

    [Test]
    public async Task Directory_BatchDeleting()
    {
        // 1. Page size is a maximum of 5000 items.
        // 2. Each batch request supports a maximum of 256 sub-requests.
        //
        // We'll set it slightly higher to test pagination and batching functionality.
        const int Count = 5100;

        using var fs = GetFileSystem();
        for (var i = 0; i < Count; i++)
            await fs.WriteFileAsync($"/temp/{i:0000}", Stream.Null);

        Assert.That(
            await fs.GetFilesAsync("/temp", "**").CountAsync(),
            Is.EqualTo(Count));

        await fs.DeleteDirectoryAsync("/temp");

        Assert.That(
            await fs.GetFilesAsync("/temp", "**").CountAsync(),
            Is.EqualTo(0));
    }

    protected override AzureFileSystem GetFileSystem()
    {
        return new AzureFileSystem("storage", new AzureFileSystemOptions
        {
            ConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
            Public = false
        });
    }

    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);
}
