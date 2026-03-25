using System.Reflection;

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
    private readonly HashSet<string> _buckets = [];
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

        foreach (var name in _buckets.ToArray())
        {
            using var fs = CreateFileSystem(name);

            try
            {
                await fs.DeleteDirectoryAsync("/");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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

            // Write enough data to trigger automatic part upload (>= 5 MiB).
            await stream.WriteAsync(new byte[6 * 1024 * 1024]);

            // Simulates an internal buffer write error.
            await underlying.DisposeAsync();

            try
            {
                await stream.WriteAsync(new byte[1024]);
                Assert.Fail();
            }
            catch
            {
                // Ignore
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

        await source.DeleteAsync();
        await destination.DeleteAsync();
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

        await source.DeleteAsync();
        await destination.DeleteAsync();
    }

    [Test]
    public async Task File_OpenWrite_FlushDoesNotCauseUndersizedParts()
    {
        using var fs = GetFileSystem();

        const string Content = "Hello, World!";

        {
            await using var stream = await fs.OpenWriteAsync("/flush-test.txt");
            await using var writer = new StreamWriter(stream);

            // Write small data and flush multiple times.
            // Flush should be a no-op and not upload undersized parts.
            foreach (var ch in Content)
            {
                await writer.WriteAsync(ch);
                await writer.FlushAsync();
            }
        }
        {
            // ReSharper disable once UseAwaitUsing
            using var stream = await fs.OpenReadAsync("/flush-test.txt");
            using var reader = new StreamReader(stream);

            Assert.That(await reader.ReadToEndAsync(), Is.EqualTo(Content));
        }

        await fs.DeleteFileAsync("/flush-test.txt");
    }

    [Test]
    public async Task File_OpenWrite_FlushWithMultipartUpload()
    {
        using var fs = GetFileSystem();

        const int Count = 5;
        const string FileName = "/flush-multipart-test.bin";

        var chunk = new byte[3 * 1024 * 1024];
        Random.Shared.NextBytes(chunk);

        {
            await using var stream = await fs.OpenWriteAsync(FileName);
            for (var i = 0; i < Count; i++)
                await stream.WriteAsync(chunk);
        }
        {
            var file = fs.GetFile(FileName);

            Assert.That(await file.ExistsAsync(), Is.True);
            Assert.That(await file.GetLengthAsync(), Is.EqualTo(chunk.Length * Count));

            // ReSharper disable once UseAwaitUsing
            using var stream = await file.OpenReadAsync();

            var bytes = new byte[chunk.Length];

            for (var i = 0; i < Count; i++)
            {
                var n = await ReadBlockAsync(stream, bytes);
                Assert.That(n, Is.EqualTo(bytes.Length));

                Assert.That(
                    bytes.AsSpan().SequenceEqual(chunk),
                    Is.True);
            }
        }

        await fs.DeleteFileAsync(FileName);

        static async Task<int> ReadBlockAsync(Stream stream, Memory<byte> memory)
        {
            var count = memory.Length;

            while (!memory.IsEmpty)
            {
                var n = await stream.ReadAsync(memory);
                if (n == 0)
                    return 0;

                memory = memory[n..];
            }

            return count;
        }
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
            await fs.GetFilesAsync("/temp").AnyAsync(),
            Is.False);
    }

    protected override AmazonS3FileSystem GetFileSystem() =>
        CreateFileSystem("storage");

    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);

    private AmazonS3FileSystem CreateFileSystem(string storageName)
    {
        _buckets.Add(storageName);

        return new AmazonS3FileSystem(
            new BasicAWSCredentials("rustfsadmin", "rustfsadmin"),
            new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1,
                ServiceURL = "http://localhost:9000",
                ForcePathStyle = true
            },
            bucketName: storageName);
    }
}
