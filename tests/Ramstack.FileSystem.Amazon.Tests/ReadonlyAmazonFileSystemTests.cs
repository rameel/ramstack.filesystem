using Amazon;
using Amazon.Runtime;
using Amazon.S3;

using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Amazon;

[TestFixture]
[Category("Cloud:Amazon")]
public class ReadonlyAmazonFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeSetUp]
    public async Task Setup()
    {
        using var fs = CreateFileSystem(isReadOnly: false);
        await fs.CreateBucketAsync();

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

    protected override AmazonS3FileSystem GetFileSystem() =>
        CreateFileSystem(isReadOnly: true);

    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);

    private AmazonS3FileSystem CreateFileSystem(bool isReadOnly)
    {
        var credentials = new BasicAWSCredentials("minioadmin", "minioadmin");
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.USEast1,
            ServiceURL = "http://localhost:9000",
            ForcePathStyle = true,
        };

        return new AmazonS3FileSystem(credentials, config, bucketName: "storage")
        {
            IsReadOnly = isReadOnly
        };
    }
}
