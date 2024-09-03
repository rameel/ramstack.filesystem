using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Ramstack.FileSystem.Azure;

/// <summary>
/// Represents an implementation of <see cref="VirtualFile"/> that maps a file to an Azure Storage blob.
/// </summary>
internal sealed class AzureFile : VirtualFile
{
    private readonly AzureFileSystem _fs;
    private BlobClient? _client;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path to the file.</param>
    public AzureFile(AzureFileSystem fileSystem, string path) : base(path) =>
        _fs = fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path to the file.</param>
    /// <param name="properties">The properties of the file, if available.</param>
    public AzureFile(AzureFileSystem fileSystem, string path, VirtualNodeProperties? properties) : base(path, properties) =>
        _fs = fileSystem;

    /// <inheritdoc />
    protected override async ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            BlobProperties info = await GetBlobClient()
                .GetPropertiesAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return VirtualNodeProperties.CreateFileProperties(
                creationTime: info.CreatedOn,
                lastAccessTime: info.LastAccessed,
                lastWriteTime: info.LastModified,
                length: info.ContentLength);
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken)
    {
        var task = GetBlobClient().OpenReadAsync(cancellationToken: cancellationToken);
        return new ValueTask<Stream>(task);
    }

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenWriteCoreAsync(CancellationToken cancellationToken)
    {
        var task = GetBlobClient().OpenWriteAsync(overwrite: true, cancellationToken: cancellationToken);
        return new ValueTask<Stream>(task);
    }

    /// <inheritdoc />
    protected override ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken)
    {
        var options = new BlobUploadOptions
        {
            HttpHeaders = _fs.GetBlobHeaders(FullName)
        };

        if (!overwrite)
        {
            options.Conditions = new BlobRequestConditions
            {
                IfNoneMatch = new ETag("*")
            };
        }


        var task = GetBlobClient().UploadAsync(stream, options, cancellationToken);
        return new ValueTask(task);
    }

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken)
    {
        var task = GetBlobClient().DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            cancellationToken: cancellationToken);

        return new ValueTask(task);
    }

    /// <inheritdoc />
    protected override ValueTask CopyCoreAsync(string destinationPath, bool overwrite, CancellationToken cancellationToken)
    {
        var source = GetBlobClient();
        var destination = _fs.CreateBlobClient(destinationPath);
        return CopyBlobAsync(source, destination, overwrite, cancellationToken);
    }

    /// <inheritdoc />
    protected override ValueTask CopyToCoreAsync(VirtualFile destination, bool overwrite, CancellationToken cancellationToken)
    {
        return destination switch
        {
            AzureFile destinationFile => CopyBlobAsync(GetBlobClient(), destinationFile.GetBlobClient(), overwrite, cancellationToken),
            _ => base.CopyToCoreAsync(destination, overwrite, cancellationToken)
        };
    }

    /// <summary>
    /// Asynchronously copies a source blob to the specified destination.
    /// </summary>
    /// <param name="source">The source blob client.</param>
    /// <param name="destination">The destination blob client.</param>
    /// <param name="overwrite">A boolean value indicating whether to overwrite the destination blob if it already exists.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    private static async ValueTask CopyBlobAsync(BlobClient source, BlobClient destination, bool overwrite, CancellationToken cancellationToken)
    {
        var conditions = !overwrite
            ? new BlobRequestConditions { IfNoneMatch = new ETag("*") }
            : null;

        var operation = await destination
            .StartCopyFromUriAsync(
                source.Uri,
                destinationConditions: conditions,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await operation
            .WaitForCompletionAsync(
                pollingInterval: TimeSpan.FromMilliseconds(100),
                cancellationToken)
            .ConfigureAwait(false);

        BlobProperties properties = await destination
            .GetPropertiesAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (properties.CopyStatus != CopyStatus.Success)
        {
            var message = $"Error while copying file. {properties.CopyStatus}: {properties.CopyStatusDescription}";
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Returns the <see cref="BlobClient"/> associated with this file.
    /// </summary>
    /// <returns>
    /// The <see cref="BlobClient"/> instance used to manage this blob.
    /// </returns>
    private BlobClient GetBlobClient() =>
        _client ??= _fs.CreateBlobClient(FullName);
}
