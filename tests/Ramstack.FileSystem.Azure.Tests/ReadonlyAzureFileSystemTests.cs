using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Azure;

[TestFixture]
[Category("Cloud:Azure")]
public class ReadonlyAzureFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeSetUp]
    public async Task Setup()
    {
        using var fs = CreateFileSystem(isReadonly: false);

        await fs.CreateContainerAsync();

        foreach (var path in Directory.EnumerateFiles(_storage.Root, "*", SearchOption.AllDirectories))
        {
            await using var stream = File.OpenRead(path);
            await fs.WriteAsync(path[_storage.Root.Length..], stream, overwrite: true);
        }
    }

    [OneTimeTearDown]
    public async Task Cleanup()
    {
        _storage.Dispose();

        using var fs = CreateFileSystem(isReadonly: false);
        await fs.DeleteDirectoryAsync("/");
    }

    protected override IVirtualFileSystem GetFileSystem() =>
        CreateFileSystem(isReadonly: true);

    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);

    private static AzureFileSystem CreateFileSystem(bool isReadonly)
    {
        const string ConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

        return new AzureFileSystem(ConnectionString, "storage")
        {
            IsReadOnly = isReadonly
        };
    }
}
