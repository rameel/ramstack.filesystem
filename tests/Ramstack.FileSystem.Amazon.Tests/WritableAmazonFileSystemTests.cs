using Amazon;
using Amazon.Runtime;
using Amazon.S3;

using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Amazon;

[TestFixture]
[Category("Cloud:Amazon")]
public class WritableAmazonFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly HashSet<string> _list = [];
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeSetUp]
    public async Task Setup()
    {
        using var fs = GetFileSystem();
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

        foreach (var name in _list.ToArray())
        {
            using var fs = CreateFileSystem(name);

            try
            {
                await fs.DeleteDirectoryAsync("/");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }

    [Test]
    public async Task File_WriteHugeFile()
    {
        const int TotalLines = 500;

        using var fs = GetFileSystem();

        var value = new string('a', 100 * 1024);

        {
            await using var stream = await fs.OpenWriteAsync("/test.txt");
            await using var writer = new StreamWriter(stream);

            for (var i = 0; i < TotalLines; i++)
                await writer.WriteLineAsync(value);
        }

        {
            var file = fs.GetFile("/test.txt");

            Assert.That(
                await file.ExistsAsync(),
                Is.True);

            Assert.That(
                await file.GetLengthAsync(),
                Is.EqualTo(TotalLines * value.Length + TotalLines * Environment.NewLine.Length));

            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);

            var count = 0;

            while (!reader.EndOfStream)
            {
                Assert.That(
                    await reader.ReadLineAsync(),
                    Is.EqualTo(value));

                count++;
            }

            Assert.That(count, Is.EqualTo(TotalLines));

            await file.DeleteAsync();

            Assert.That(
                await file.ExistsAsync(),
                Is.False);
        }
    }

    [Test]
    public async Task File_CopyTo_File_DifferentFileSystems()
    {
        using var fs1 = new PhysicalFileSystem(_storage.Root);
        using var fs2 = GetFileSystem();

        var source = fs1.GetFile("/0000.txt");
        var destination = fs2.GetFile("/0000.txt");

        var content = Guid.NewGuid().ToString();

        await using (var stream = await source.OpenWriteAsync())
        await using (var writer = new StreamWriter(stream))
            await writer.WriteAsync(content);

        Assert.That(await destination.ExistsAsync(), Is.False);

        await source.CopyToAsync(destination);
        Assert.That(await destination.ExistsAsync(), Is.True);

        using var reader = await destination.OpenTextAsync();
        Assert.That(
            await reader.ReadToEndAsync(),
            Is.EqualTo(content));
    }

    [Test]
    public async Task File_CopyTo_File_DifferentStorages()
    {
        using var fs1 = CreateFileSystem("temp-storage");
        using var fs2 = GetFileSystem();

        await fs1.CreateBucketAsync();

        var source = fs1.GetFile("/1111.txt");
        var destination = fs2.GetFile("/1111.txt");

        var content = Guid.NewGuid().ToString();

        await using (var stream = await source.OpenWriteAsync())
        await using (var writer = new StreamWriter(stream))
            await writer.WriteAsync(content);

        Assert.That(await destination.ExistsAsync(), Is.False);

        await source.CopyToAsync(destination);
        Assert.That(await destination.ExistsAsync(), Is.True);

        using var reader = await destination.OpenTextAsync();
        Assert.That(
            await reader.ReadToEndAsync(),
            Is.EqualTo(content));
    }


    [Test]
    public async Task Directory_BatchDeleting()
    {
        // 1. The maximum list page size is 1000 items.
        // 2. The delete batch supports a maximum of 1000 keys.
        //
        // We'll set the count slightly higher to test pagination
        // and batching functionality.
        const int Count = 1100;

        using var fs = GetFileSystem();
        for (var i = 0; i < Count; i++)
            await fs.WriteAsync($"/temp/{i:0000}", Stream.Null);

        Assert.That(
            await fs.GetFilesAsync("/temp").CountAsync(),
            Is.EqualTo(Count));

        await fs.DeleteDirectoryAsync("/temp");

        Assert.That(
            await fs.GetFilesAsync("/temp").CountAsync(),
            Is.EqualTo(0));
    }

    protected override AmazonS3FileSystem GetFileSystem() =>
        CreateFileSystem("storage");

    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);

    private AmazonS3FileSystem CreateFileSystem(string storageName)
    {
        _list.Add(storageName);

        return new AmazonS3FileSystem(
            new BasicAWSCredentials("minioadmin", "minioadmin"),
            new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1,
                ServiceURL = "http://localhost:9000",
                ForcePathStyle = true
            },
            bucketName: storageName);
    }
}
