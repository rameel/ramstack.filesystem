using System.Diagnostics;
using System.Runtime.CompilerServices;

using Amazon.S3.Model;

using Ramstack.Globbing;

namespace Ramstack.FileSystem.Amazon;

/// <summary>
/// Represents an implementation of <see cref="VirtualDirectory"/> that maps a directory
/// to a path within a specified Amazon S3 bucket.
/// </summary>
internal sealed class S3Directory : VirtualDirectory
{
    private readonly AmazonS3FileSystem _fs;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="S3Directory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The path to the directory within the specified Amazon S3 bucket.</param>
    public S3Directory(AmazonS3FileSystem fileSystem, string path) : base(path) =>
        _fs = fileSystem;

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken) =>
        new ValueTask<VirtualNodeProperties?>(VirtualNodeProperties.None);

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
            Prefix = GetPrefix(FullName),
            Delimiter = "/"
        };

        do
        {
            var response = await _fs.AmazonClient
                .ListObjectsV2Async(request, cancellationToken)
                .ConfigureAwait(false);

            foreach (var prefix in response.CommonPrefixes)
                yield return new S3Directory(_fs, VirtualPath.Normalize(prefix));

            foreach (var obj in response.S3Objects)
                yield return CreateVirtualFile(obj);

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken is not null && !cancellationToken.IsCancellationRequested);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
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

            foreach (var obj in response.S3Objects)
                yield return CreateVirtualFile(obj);

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken is not null && !cancellationToken.IsCancellationRequested);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
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
                yield return new S3Directory(_fs, VirtualPath.Normalize(prefix));

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken is not null && !cancellationToken.IsCancellationRequested);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync(string[] patterns, string[]? excludes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        //
        // Cloud storage optimization strategy:
        // 1. List all objects in one batch using prefix filtering (matches our virtual directory).
        // 2. Perform pattern matching and exclusion filters locally.
        //
        // Benefits:
        // - Single API call instead of per-directory requests
        // - Reduced network latency, especially for deep directory structures
        // - More efficient than recursive directory scanning
        //

        var request = new ListObjectsV2Request
        {
            BucketName = _fs.BucketName,
            Prefix = GetPrefix(FullName)
        };

        var directories = new HashSet<string>
        {
            FullName
        };

        do
        {
            var response = await _fs.AmazonClient
                .ListObjectsV2Async(request, cancellationToken)
                .ConfigureAwait(false);

            foreach (var obj in response.S3Objects)
            {
                var path = VirtualPath.Normalize(obj.Key);
                var directoryPath = VirtualPath.GetDirectoryName(path);

                while (directoryPath.Length != 0 && directories.Add(directoryPath))
                {
                    //
                    // Directories are yielded in reverse order (deepest first).
                    //
                    // Note: We could use a Stack<string> to control the order,
                    // but since order isn't guaranteed anyway and to avoid
                    // unnecessary memory allocation, we process them directly.
                    //
                    if (IsMatched(directoryPath.AsSpan(FullName.Length), patterns, excludes))
                        yield return new S3Directory(_fs, directoryPath);

                    directoryPath = VirtualPath.GetDirectoryName(directoryPath);
                }

                if (IsMatched(obj.Key.AsSpan(request.Prefix.Length), patterns, excludes))
                    yield return CreateVirtualFile(obj, path);
            }

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken is not null && !cancellationToken.IsCancellationRequested);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync(string[] patterns, string[]? excludes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        //
        // Cloud storage optimization strategy:
        // 1. List all objects in one batch using prefix filtering (matches our virtual directory).
        // 2. Perform pattern matching and exclusion filters locally.
        //
        // Benefits:
        // - Single API call instead of per-directory requests
        // - Reduced network latency, especially for deep directory structures
        // - More efficient than recursive directory scanning
        //

        var request = new ListObjectsV2Request
        {
            BucketName = _fs.BucketName,
            Prefix = GetPrefix(FullName)
        };

        do
        {
            var response = await _fs.AmazonClient
                .ListObjectsV2Async(request, cancellationToken)
                .ConfigureAwait(false);

            foreach (var obj in response.S3Objects)
                if (IsMatched(obj.Key.AsSpan(request.Prefix.Length), patterns, excludes))
                    yield return CreateVirtualFile(obj);

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken is not null && !cancellationToken.IsCancellationRequested);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync(string[] patterns, string[]? excludes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        //
        // Cloud storage optimization strategy:
        // 1. List all objects in one batch using prefix filtering (matches our virtual directory).
        // 2. Perform pattern matching and exclusion filters locally.
        //
        // Benefits:
        // - Single API call instead of per-directory requests
        // - Reduced network latency, especially for deep directory structures
        // - More efficient than recursive directory scanning
        //

        var request = new ListObjectsV2Request
        {
            BucketName = _fs.BucketName,
            Prefix = GetPrefix(FullName)
        };

        var directories = new HashSet<string>
        {
            FullName
        };

        do
        {
            var response = await _fs.AmazonClient
                .ListObjectsV2Async(request, cancellationToken)
                .ConfigureAwait(false);

            foreach (var obj in response.S3Objects)
            {
                var directoryPath = VirtualPath.GetDirectoryName(
                    VirtualPath.Normalize(obj.Key));

                while (directoryPath.Length != 0 && directories.Add(directoryPath))
                {
                    //
                    // Directories are yielded in reverse order (deepest first).
                    //
                    // Note: We could use a Stack<string> to control the order,
                    // but since order isn't guaranteed anyway and to avoid
                    // unnecessary memory allocation, we process them directly.
                    //
                    if (IsMatched(directoryPath.AsSpan(FullName.Length), patterns, excludes))
                        yield return new S3Directory(_fs, directoryPath);

                    directoryPath = VirtualPath.GetDirectoryName(directoryPath);
                }
            }

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken is not null && !cancellationToken.IsCancellationRequested);
    }

    /// <summary>
    /// Creates a <see cref="S3File"/> instance based on the specified object.
    /// </summary>
    /// <param name="obj">The <see cref="S3Object"/> representing the file.</param>
    /// <param name="normalizedName">The normalized name of the object.</param>
    /// <returns>
    /// A new <see cref="S3File"/> instance representing the file.
    /// </returns>
    private S3File CreateVirtualFile(S3Object obj, string? normalizedName = null)
    {
        var properties = VirtualNodeProperties
            .CreateFileProperties(
                creationTime: default,
                lastAccessTime: default,
                lastWriteTime: obj.LastModified,
                length: obj.Size);

        var path = normalizedName ?? VirtualPath.Normalize(obj.Key);
        return new S3File(_fs, path, properties);
    }

    /// <summary>
    /// Determines whether the specified path matches any of the inclusion patterns and none of the exclusion patterns.
    /// </summary>
    /// <param name="path">The path to match.</param>
    /// <param name="patterns">The inclusion patterns to match against the path.</param>
    /// <param name="excludes">An optional array of exclusion patterns. If the path matches any of these,
    /// the method returns <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the path matches at least one inclusion pattern and no exclusion patterns;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMatched(scoped ReadOnlySpan<char> path, string[] patterns, string[]? excludes)
    {
        if (excludes is not null)
            foreach (var pattern in excludes)
                if (Matcher.IsMatch(path, pattern, MatchFlags.Unix))
                    return false;

        foreach (var pattern in patterns)
            if (Matcher.IsMatch(path, pattern, MatchFlags.Unix))
                return true;

        return false;
    }

    /// <summary>
    /// Returns a cloud storage compatible prefix for the specified directory path.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>
    /// A string formatted for cloud storage prefix filtering:
    /// <list type="bullet">
    ///   <item><description>Empty string for the root directory ("/")</description></item>
    ///   <item><description>Path without leading slash but with trailing slash for subdirectories</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    ///   GetPrefix("/")           // returns ""
    ///   GetPrefix("/folder")     // returns "folder/"
    ///   GetPrefix("/sub/folder") // returns "sub/folder/"
    /// </code>
    /// </example>
    private static string GetPrefix(string path)
    {
        Debug.Assert(VirtualPath.IsNormalized(path));
        return path == "/" ? "" : $"{path[1..]}/";
    }
}
