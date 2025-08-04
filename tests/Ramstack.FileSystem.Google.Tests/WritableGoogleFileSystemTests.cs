using System.Reflection;

using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Google;

[TestFixture]
[Category("Cloud:Google")]
[Ignore("This test requires a running Google Cloud Storage emulator")]
public class WritableGoogleFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly HashSet<string> _buckets = [];
    private readonly TempFileStorage _storage = new TempFileStorage();

    [OneTimeSetUp]
    public async Task Setup()
    {
        // if (Environment.GetEnvironmentVariable("STORAGE_EMULATOR_HOST") is null)
        //     Environment.SetEnvironmentVariable("STORAGE_EMULATOR_HOST", "http://localhost:4443/storage/v1/");

        using var fs = GetFileSystem();
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

        foreach (var bucket in _buckets.ToArray())
        {
            using var fs = CreateFileSystem(bucket);

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
    public async Task File_OpenWrite_InternalBufferWriteError_DoesNotCreateFile()
    {
        using var fs = GetFileSystem();

        await using (var stream = await fs.OpenWriteAsync("/error.log"))
        {
            var underlying = (FileStream)stream.GetType().GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(stream)!;
            Assert.That(underlying, Is.Not.Null);

            await stream.WriteAsync(new ReadOnlyMemory<byte>(new byte[1024]));

            // Forces to upload buffer.
            await stream.FlushAsync();

            // Simulates an internal buffer write error.
            await underlying.DisposeAsync();

            try
            {
                await stream.WriteAsync(new ReadOnlyMemory<byte>(new byte[1024]));
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception expected!");
                Console.WriteLine(exception);
            }
        }

        Assert.That(
            await fs.FileExistsAsync("/error.log"),
            Is.False);
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
        using var fs1 = CreateFileSystem("ramstack-temp-bucket-1");
        using var fs2 = CreateFileSystem("ramstack-temp-bucket-2");

        await fs1.CreateBucketAsync("ramstack-project");
        await fs2.CreateBucketAsync("ramstack-project");

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

    protected override GoogleFileSystem GetFileSystem() =>
        CreateFileSystem("ramstack-test-bucket");

    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);

    private GoogleFileSystem CreateFileSystem(string bucket)
    {
        _buckets.Add(bucket);

        // var client = new StorageClientBuilder
        // {
        //     BaseUri = Environment.GetEnvironmentVariable("STORAGE_EMULATOR_HOST"),
        //     UnauthenticatedAccess = true
        // }.Build();

        var client = StorageClient.Create(
            GoogleCredential.FromFile("credentials.json"));

        return new GoogleFileSystem(bucketName: bucket, client: client);
    }
}
