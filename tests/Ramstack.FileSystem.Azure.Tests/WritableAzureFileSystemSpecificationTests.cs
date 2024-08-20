using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Azure;

[TestFixture]
public class WritableAzureFileSystemSpecificationTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeSetUp]
    public async Task Setup()
    {
        using var fs = GetFileSystem();

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

    protected override IVirtualFileSystem GetFileSystem()
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
