using System.Net;

using Amazon.S3;
using Amazon.S3.Model;

namespace Ramstack.FileSystem.Amazon;

internal sealed class AmazonFile : VirtualFile
{
    private readonly AmazonS3FileSystem _fs;

    public override IVirtualFileSystem FileSystem => _fs;

    internal string Key => FullName[1..];

    public AmazonFile(AmazonS3FileSystem fileSystem, string path) : base(path)
    {
        _fs = fileSystem;
    }

    /// <inheritdoc />
    protected override async ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        var metadata = await _fs.AmazonClient
            .GetObjectMetadataAsync(
                new GetObjectMetadataRequest { BucketName = _fs.BucketName, Key = Key },
                cancellationToken)
            .ConfigureAwait(false);

        return VirtualNodeProperties.CreateFileProperties(
            creationTime: default,
            lastAccessTime: default,
            lastWriteTime: metadata.LastModified,
            length: metadata.ContentLength);
    }

    /// <inheritdoc />
    protected override async ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken)
    {
        var response = await _fs.AmazonClient
            .GetObjectAsync(
                new GetObjectRequest { BucketName = _fs.BucketName, Key = Key },
                cancellationToken)
            .ConfigureAwait(false);

        return response.ResponseStream;
    }

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenWriteCoreAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    protected override async ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = _fs.BucketName,
            Key = Key,
            InputStream = stream,
            AutoCloseStream = false
        };

        if (!overwrite)
            request.IfNoneMatch = "*";

        await _fs.AmazonClient
            .PutObjectAsync(request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask DeleteCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _fs.AmazonClient
                .DeleteObjectAsync(
                    new DeleteObjectRequest { BucketName = _fs.BucketName, Key = Key },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
        }
    }
}
