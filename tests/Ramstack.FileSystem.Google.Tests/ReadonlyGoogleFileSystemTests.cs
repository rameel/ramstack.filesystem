using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Google;

[TestFixture]
[Category("Cloud:Google")]
[Ignore("This test requires a running Google Cloud Storage emulator")]
public class ReadonlyGoogleFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeSetUp]
    public async Task Setup()
    {
        // if (Environment.GetEnvironmentVariable("STORAGE_EMULATOR_HOST") is null)
        //     Environment.SetEnvironmentVariable("STORAGE_EMULATOR_HOST", "http://localhost:4443/storage/v1/");

        using var fs = CreateFileSystem(isReadOnly: false);
        await fs.CreateBucketAsync("ramstack-project");

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

        using var fs = CreateFileSystem(isReadOnly: false);
        await fs.DeleteDirectoryAsync("/");
    }

    protected override GoogleFileSystem GetFileSystem() =>
        CreateFileSystem(isReadOnly: true);

    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);

    private static GoogleFileSystem CreateFileSystem(bool isReadOnly)
    {
        // var builder = new StorageClientBuilder
        // {
        //     BaseUri = Environment.GetEnvironmentVariable("STORAGE_EMULATOR_HOST"),
        //     UnauthenticatedAccess = true
        // };
        //
        // var client = builder.Build();

        var client = StorageClient.Create(
            GoogleCredential.FromFile("credentials.json"));

        return new GoogleFileSystem(bucketName: "ramstack-test-bucket", client: client)
        {
            IsReadOnly = isReadOnly
        };
    }
}
