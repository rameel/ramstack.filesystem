using System.Runtime.CompilerServices;

using Amazon.S3.Model;

using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Amazon;

internal sealed class AmazonDirectory : VirtualDirectory
{
    private readonly AmazonS3FileSystem _fs;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    public AmazonDirectory(AmazonS3FileSystem fileSystem, string path) : base(path)
    {
        _fs = fileSystem;
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
            Prefix = GetPrefix(FullName)
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
            Prefix = GetPrefix(FullName),
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

    /// <summary>
    /// Returns the prefix for the specified directory path.
    /// </summary>
    /// <param name="directoryPath">The directory path for which to get the prefix.</param>
    /// <returns>
    /// The prefix associated with the directory path.
    /// </returns>
    private static string GetPrefix(string directoryPath) =>
        directoryPath == "/" ? "" : $"{directoryPath[1..]}/";
}
