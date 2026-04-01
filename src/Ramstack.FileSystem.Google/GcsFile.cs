using System.Net;

using Google;
using Google.Cloud.Storage.V1;

namespace Ramstack.FileSystem.Google;

/// <summary>
/// Represents an implementation of <see cref="VirtualFile"/> that maps a file to a Google Cloud Storage object.
/// </summary>
internal sealed class GcsFile : VirtualFile
{
    private readonly GoogleFileSystem _fs;
    private string? _objectName;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="GcsFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path to the file.</param>
    public GcsFile(GoogleFileSystem fileSystem, string path) : base(path) =>
        _fs = fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="GcsFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path to the file.</param>
    /// <param name="properties">The properties of the file, if available.</param>
    public GcsFile(GoogleFileSystem fileSystem, string path, VirtualNodeProperties? properties) : base(path, properties) =>
        _fs = fileSystem;

    /// <inheritdoc />
    protected override async ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var obj = await _fs.StorageClient
                .GetObjectAsync(_fs.BucketName, GetObjectName(), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return VirtualNodeProperties.CreateFileProperties(
                creationTime: obj.TimeCreatedDateTimeOffset.GetValueOrDefault(),
                lastAccessTime: default,
                lastWriteTime: obj.UpdatedDateTimeOffset.GetValueOrDefault(),
                length: (long?)obj.Size ?? 0);
        }
        catch (GoogleApiException e) when (e.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override async ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken)
    {
        var stream = CreateTempFileStream();
        await _fs.StorageClient
            .DownloadObjectAsync(_fs.BucketName, GetObjectName(), stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        stream.Position = 0;
        return stream;

        static FileStream CreateTempFileStream()
        {
            const int BufferSize = 4096;
            const FileOptions Options = FileOptions.DeleteOnClose | FileOptions.Asynchronous;

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, BufferSize, Options);
        }
    }

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenWriteCoreAsync(CancellationToken cancellationToken) =>
        new ValueTask<Stream>(new GcsWriteStream(_fs, GetObjectName()));

    /// <inheritdoc />
    protected override async ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken)
    {
        var obj = new global::Google.Apis.Storage.v1.Data.Object
        {
            Bucket = _fs.BucketName,
            Name = GetObjectName()
        };

        var options = new UploadObjectOptions();
        if (!overwrite)
            options.IfGenerationMatch = 0;

        await _fs.StorageClient
            .UploadObjectAsync(obj, stream, options, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask DeleteCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _fs.StorageClient
                .DeleteObjectAsync(_fs.BucketName, GetObjectName(), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (GoogleApiException e) when (e.HttpStatusCode == HttpStatusCode.NotFound)
        {
            // Object doesn't exist, which is fine for this operation
        }
    }

    /// <inheritdoc />
    protected override ValueTask CopyToCoreAsync(string destinationPath, bool overwrite, CancellationToken cancellationToken) =>
        CopyObjectAsync(_fs.BucketName, GetObjectName(), _fs.BucketName, destinationPath[1..], overwrite, cancellationToken);

    /// <inheritdoc />
    protected override ValueTask CopyToCoreAsync(VirtualFile destination, bool overwrite, CancellationToken cancellationToken)
    {
        if (destination is GcsFile file)
            return CopyObjectAsync(_fs.BucketName, GetObjectName(), file._fs.BucketName, file.GetObjectName(), overwrite, cancellationToken);

        return base.CopyToCoreAsync(destination, overwrite, cancellationToken);
    }

    /// <summary>
    /// Returns the object name associated with this file.
    /// </summary>
    /// <returns>
    /// The object name used in Google Cloud Storage.
    /// </returns>
    internal string GetObjectName() =>
        _objectName ??= FullName[1..];

    /// <summary>
    /// Asynchronously copies a source object to the specified destination.
    /// </summary>
    /// <param name="sourceBucket">The name of the source GCS bucket.</param>
    /// <param name="sourceObjectName">The source object name.</param>
    /// <param name="destinationBucket">The name of the destination GCS bucket.</param>
    /// <param name="destinationObjectName">The destination object name.</param>
    /// <param name="overwrite">A boolean value indicating whether to overwrite the destination object if it already exists.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    private async ValueTask CopyObjectAsync(string sourceBucket, string sourceObjectName, string destinationBucket, string destinationObjectName, bool overwrite, CancellationToken cancellationToken)
    {
        if (sourceBucket == destinationBucket && sourceObjectName == destinationObjectName)
            throw new IOException($"Cannot copy a file '{FullName}' to itself.");

        var options = new CopyObjectOptions();
        if (!overwrite)
            options.IfGenerationMatch = 0;

        await _fs.StorageClient
            .CopyObjectAsync(sourceBucket, sourceObjectName, destinationBucket, destinationObjectName, options, cancellationToken)
            .ConfigureAwait(false);
    }
}
