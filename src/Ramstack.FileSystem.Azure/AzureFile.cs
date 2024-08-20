using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Ramstack.FileSystem.Azure;

/// <summary>
/// Represents an implementation of <see cref="VirtualFile"/> that maps a file to an Azure Storage blob.
/// </summary>
internal sealed class AzureFile : VirtualFile
{
    private readonly AzureFileSystem _fileSystem;
    private BlobClient? _client;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path to the file.</param>
    public AzureFile(AzureFileSystem fileSystem, string path) : base(path) =>
        _fileSystem = fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path to the file.</param>
    /// <param name="properties">The properties of the file, if available.</param>
    public AzureFile(AzureFileSystem fileSystem, string path, VirtualNodeProperties? properties) : base(path, properties) =>
        _fileSystem = fileSystem;

    /// <inheritdoc />
    protected override async ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var info = (await GetBlobClient().GetPropertiesAsync(cancellationToken: cancellationToken)).Value;
            return VirtualNodeProperties.File(info.CreatedOn, info.LastAccessed, info.LastModified, info.ContentLength);
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override async ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken) =>
        (await GetBlobClient().DownloadAsync(cancellationToken)).Value.Content;

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenWriteCoreAsync(CancellationToken cancellationToken) =>
        new ValueTask<Stream>(new AzureTempStream(this));

    /// <inheritdoc />
    protected override async ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken)
    {
        await GetBlobClient().UploadAsync(
            stream,
            _fileSystem.GetBlobHeaders(FullName),
            conditions: !overwrite
                ? new BlobRequestConditions { IfNoneMatch = new ETag("*") }
                : null,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken)
    {
        var task = GetBlobClient().DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            cancellationToken: cancellationToken);

        return new ValueTask(task);
    }

    /// <summary>
    /// Returns the <see cref="BlobClient"/> associated with this file.
    /// </summary>
    /// <returns>
    /// The <see cref="BlobClient"/> instance used to manage this blob.
    /// </returns>
    private BlobClient GetBlobClient() =>
        _client ??= _fileSystem.Container.GetBlobClient(FullName[1..]);
}
