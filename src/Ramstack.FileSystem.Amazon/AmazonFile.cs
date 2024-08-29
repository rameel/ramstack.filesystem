﻿using System.Net;

using Amazon.S3;
using Amazon.S3.Model;

namespace Ramstack.FileSystem.Amazon;

/// <summary>
/// Represents an implementation of <see cref="VirtualFile"/> that maps a file to an object in Amazon S3.
/// </summary>
internal sealed class AmazonFile : VirtualFile
{
    private readonly AmazonS3FileSystem _fs;
    private readonly string _key;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path to the file.</param>
    public AmazonFile(AmazonS3FileSystem fileSystem, string path) : base(path) =>
        (_fs, _key) = (fileSystem, path[1..]);

    /// <inheritdoc />
    protected override async ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _fs.AmazonClient
                .GetObjectMetadataAsync(
                    new GetObjectMetadataRequest { BucketName = _fs.BucketName, Key = _key },
                    cancellationToken)
                .ConfigureAwait(false);

            return VirtualNodeProperties.CreateFileProperties(
                creationTime: default,
                lastAccessTime: default,
                lastWriteTime: metadata.LastModified,
                length: metadata.ContentLength);
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override async ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken)
    {
        var response = await _fs.AmazonClient
            .GetObjectAsync(
                new GetObjectRequest { BucketName = _fs.BucketName, Key = _key },
                cancellationToken)
            .ConfigureAwait(false);

        return response.ResponseStream;
    }

    /// <inheritdoc />
    protected override async ValueTask<Stream> OpenWriteCoreAsync(CancellationToken cancellationToken)
    {
        var response = await _fs.AmazonClient
            .InitiateMultipartUploadAsync(_fs.BucketName, _key, cancellationToken)
            .ConfigureAwait(false);

        return new AmazonS3UploadStream(_fs.AmazonClient, _fs.BucketName, _key, response.UploadId);
    }

    /// <inheritdoc />
    protected override async ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = _fs.BucketName,
            Key = _key,
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
                    new DeleteObjectRequest { BucketName = _fs.BucketName, Key = _key },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
        }
    }
}