using System.Net;
using System.Runtime.CompilerServices;

using Google;
using Google.Cloud.Storage.V1;

namespace Ramstack.FileSystem.Google;

/// <summary>
/// Represents an implementation of <see cref="VirtualDirectory"/> that maps a directory
/// to the path within a Google Cloud Storage bucket.
/// </summary>
internal sealed class GcsDirectory : VirtualDirectory
{
    private readonly GoogleFileSystem _fs;
    private readonly string _prefix;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="GcsDirectory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The path to the directory within the Google Cloud Storage bucket.</param>
    public GcsDirectory(GoogleFileSystem fileSystem, string path) : base(path)
    {
        _fs = fileSystem;
        _prefix = path == "/" ? "" : $"{path[1..]}/";
    }

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken) =>
        new ValueTask<VirtualNodeProperties?>(VirtualNodeProperties.None);

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken) =>
        new ValueTask<bool>(true);

    /// <inheritdoc />
    protected override ValueTask CreateCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override async ValueTask DeleteCoreAsync(CancellationToken cancellationToken)
    {
        await foreach (var obj in _fs.StorageClient.ListObjectsAsync(_fs.BucketName, _prefix).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                await _fs.StorageClient
                    .DeleteObjectAsync(_fs.BucketName, obj.Name, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (GoogleApiException e) when (e.HttpStatusCode == HttpStatusCode.NotFound)
            {
            }
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = new ListObjectsOptions
        {
            Delimiter = "/",
            IncludeFoldersAsPrefixes = true,
            IncludeTrailingDelimiter = true
        };

        var responses = _fs.StorageClient
            .ListObjectsAsync(_fs.BucketName, _prefix, options)
            .AsRawResponses();

        await foreach (var page in responses.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (page.Prefixes is not null)
                foreach (var prefix in page.Prefixes)
                    yield return new GcsDirectory(_fs, VirtualPath.Normalize(prefix));

            if (page.Items is null)
                continue;

            foreach (var obj in page.Items)
            {
                var properties = VirtualNodeProperties.CreateFileProperties(
                    obj.TimeCreatedDateTimeOffset.GetValueOrDefault(),
                    DateTimeOffset.MinValue,
                    obj.UpdatedDateTimeOffset.GetValueOrDefault(),
                    (long)(obj.Size ?? 0));
                yield return new GcsFile(_fs, VirtualPath.Normalize(obj.Name), properties);
            }
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = new ListObjectsOptions
        {
            Delimiter = "/",
            IncludeFoldersAsPrefixes = true,
            IncludeTrailingDelimiter = true
        };

        var response = _fs.StorageClient.ListObjectsAsync(_fs.BucketName, _prefix, options);

        await foreach (var obj in response.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var properties = VirtualNodeProperties.CreateFileProperties(
                obj.TimeCreatedDateTimeOffset.GetValueOrDefault(),
                DateTimeOffset.MinValue,
                obj.UpdatedDateTimeOffset.GetValueOrDefault(),
                (long)(obj.Size ?? 0));

            yield return new GcsFile(_fs, VirtualPath.Normalize(obj.Name), properties);
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = new ListObjectsOptions
        {
            Delimiter = "/",
            IncludeFoldersAsPrefixes = true,
            IncludeTrailingDelimiter = true
        };

        var response = _fs.StorageClient
            .ListObjectsAsync(_fs.BucketName, _prefix, options)
            .AsRawResponses();

        await foreach (var page in response.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (page.Prefixes is null)
                continue;

            foreach (var prefix in page.Prefixes)
                yield return new GcsDirectory(_fs, VirtualPath.Normalize(prefix));
        }
    }
}
