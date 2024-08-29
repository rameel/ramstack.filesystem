using System.Runtime.CompilerServices;

using Amazon.S3.Model;

using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Amazon;

/// <summary>
/// Represents an implementation of <see cref="VirtualDirectory"/> that maps a directory
/// to a path within a specified Amazon S3 bucket.
/// </summary>
internal sealed class AmazonDirectory : VirtualDirectory
{
    private readonly AmazonS3FileSystem _fs;
    private readonly string _prefix;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonDirectory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The path to the directory within the specified Amazon S3 bucket.</param>
    public AmazonDirectory(AmazonS3FileSystem fileSystem, string path) : base(path)
    {
        _fs = fileSystem;
        _prefix = FullName == "/" ? "" : $"{FullName[1..]}/";
    }

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        var properties = VirtualNodeProperties.CreateDirectoryProperties(default, default, default);
        return new ValueTask<VirtualNodeProperties?>(properties);
    }

    /// <inheritdoc />
    protected override ValueTask CreateCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override async ValueTask DeleteCoreAsync(CancellationToken cancellationToken)
    {
        var lr = new ListObjectsV2Request
        {
            BucketName = _fs.BucketName,
            Prefix = _prefix
        };

        var dr = new DeleteObjectsRequest
        {
            BucketName = _fs.BucketName
        };

        do
        {
            // The maximum number of objects returned is MaxKeys, which is 1000,
            // and the maximum number of objects that can be deleted at once is also 1000.
            // Therefore, we can rely (sure?) on this and avoid splitting
            // the retrieved objects into separate batches.

            var response = await _fs.AmazonClient
                .ListObjectsV2Async(lr, cancellationToken)
                .ConfigureAwait(false);

            foreach (var obj in response.S3Objects)
                dr.Objects.Add(new KeyVersion { Key = obj.Key });

            if (dr.Objects.Count != 0)
                await _fs.AmazonClient
                    .DeleteObjectsAsync(dr, cancellationToken)
                    .ConfigureAwait(false);

            dr.Objects.Clear();
            lr.ContinuationToken = response.NextContinuationToken;
        }
        while (lr.ContinuationToken is not null && !cancellationToken.IsCancellationRequested);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = _fs.BucketName,
            Prefix = _prefix,
            Delimiter = "/"
        };

        do
        {
            var response = await _fs.AmazonClient
                .ListObjectsV2Async(request, cancellationToken)
                .ConfigureAwait(false);

            foreach (var prefix in response.CommonPrefixes)
                yield return new AmazonDirectory(_fs, VirtualPath.Normalize(prefix));

            foreach (var obj in response.S3Objects)
                yield return new AmazonFile(_fs, VirtualPath.Normalize(obj.Key));

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken is not null && !cancellationToken.IsCancellationRequested);
    }
}
