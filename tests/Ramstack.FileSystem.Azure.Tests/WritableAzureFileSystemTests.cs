using Ramstack.FileSystem.Physical;
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Azure;

[TestFixture]
[Category("Cloud:Azure")]
public class WritableAzureFileSystemTests : VirtualFileSystemSpecificationTests
{
    private readonly HashSet<string> _list = [];
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

        await fs1.CreateContainerAsync();

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
        // 1. Page size is a maximum of 5000 items.
        // 2. Each batch request supports a maximum of 256 sub-requests.
        //
        // We'll set it slightly higher to test pagination and batching functionality.
        const int Count = 5100;

        using var fs = GetFileSystem();
        for (var i = 0; i < Count; i++)
            await fs.WriteFileAsync($"/temp/{i:0000}", Stream.Null);

        Assert.That(
            await fs.GetFilesAsync("/temp").CountAsync(),
            Is.EqualTo(Count));

        await fs.DeleteDirectoryAsync("/temp");

        Assert.That(
            await fs.GetFilesAsync("/temp").CountAsync(),
            Is.EqualTo(0));
    }

    protected override AzureFileSystem GetFileSystem() =>
        CreateFileSystem("storage");

    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);

    private AzureFileSystem CreateFileSystem(string storageName)
    {
        _list.Add(storageName);

        const string ConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

        return new AzureFileSystem(ConnectionString, storageName)
        {
            IsReadOnly = false
        };
    }
}
