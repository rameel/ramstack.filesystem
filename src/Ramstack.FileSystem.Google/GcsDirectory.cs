using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;

using Google;
using Google.Cloud.Storage.V1;

using Ramstack.Globbing;

using GcsObject = Google.Apis.Storage.v1.Data.Object;

namespace Ramstack.FileSystem.Google;

/// <summary>
/// Represents an implementation of <see cref="VirtualDirectory"/> that maps a directory
/// to the path within a Google Cloud Storage bucket.
/// </summary>
internal sealed class GcsDirectory : VirtualDirectory
{
    private readonly GoogleFileSystem _fs;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="GcsDirectory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The path to the directory within the Google Cloud Storage bucket.</param>
    public GcsDirectory(GoogleFileSystem fileSystem, string path) : base(path) =>
        _fs = fileSystem;

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
        await foreach (var obj in _fs.StorageClient.ListObjectsAsync(_fs.BucketName, GetPrefix(FullName)).WithCancellation(cancellationToken).ConfigureAwait(false))
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
            .ListObjectsAsync(_fs.BucketName, GetPrefix(FullName), options)
            .AsRawResponses();

        await foreach (var page in responses.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (page.Prefixes is not null)
                foreach (var prefix in page.Prefixes)
                    yield return new GcsDirectory(_fs, VirtualPath.Normalize(prefix));

            if (page.Items is null)
                continue;

            foreach (var obj in page.Items)
                if (!obj.Name.EndsWith('/'))
                    yield return CreateVirtualFile(obj);
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

        var response = _fs.StorageClient.ListObjectsAsync(_fs.BucketName, GetPrefix(FullName), options);

        await foreach (var obj in response.WithCancellation(cancellationToken).ConfigureAwait(false))
            if (!obj.Name.EndsWith('/'))
                yield return CreateVirtualFile(obj);
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
            .ListObjectsAsync(_fs.BucketName, GetPrefix(FullName), options)
            .AsRawResponses();

        await foreach (var page in response.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (page.Prefixes is null)
                continue;

            foreach (var prefix in page.Prefixes)
                yield return new GcsDirectory(_fs, VirtualPath.Normalize(prefix));
        }
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

        var prefix = GetPrefix(FullName);
        var directories = new HashSet<string> { FullName };
        var response = _fs.StorageClient.ListObjectsAsync(_fs.BucketName, prefix);

        await foreach (var obj in response.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var path = VirtualPath.Normalize(obj.Name);
            var directoryPath = obj.Name.EndsWith('/') ? path : VirtualPath.GetDirectoryName(path);

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
                    yield return new GcsDirectory(_fs, VirtualPath.Normalize(directoryPath));

                directoryPath = VirtualPath.GetDirectoryName(directoryPath);
            }

            if (!obj.Name.EndsWith('/'))
                if (IsMatched(obj.Name.AsSpan(prefix.Length), patterns, excludes))
                    yield return CreateVirtualFile(obj, path);
        }
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

        var prefix = GetPrefix(FullName);

        //
        // Google Cloud Storage supports native glob matching in ListObjects,
        // but we can't use this feature due to some limitation:
        // GCS's MatchGlob doesn't support brace expansion in the middle of paths.
        // For example, patterns like "project/{images,styles}/*.{svg,css}" won't work.
        //

        var response = _fs.StorageClient.ListObjectsAsync(_fs.BucketName, prefix);

        await foreach (var obj in response.WithCancellation(cancellationToken).ConfigureAwait(false))
            if (!obj.Name.EndsWith('/'))
                if (IsMatched(obj.Name.AsSpan(prefix.Length), patterns, excludes))
                    yield return CreateVirtualFile(obj);
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

        var response = _fs.StorageClient.ListObjectsAsync(_fs.BucketName, GetPrefix(FullName));
        var directories = new HashSet<string> { FullName };

        await foreach (var obj in response.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var path = VirtualPath.Normalize(obj.Name);
            var directoryPath = obj.Name.EndsWith('/') ? path : VirtualPath.GetDirectoryName(path);

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
                    yield return new GcsDirectory(_fs, VirtualPath.Normalize(directoryPath));

                directoryPath = VirtualPath.GetDirectoryName(directoryPath);
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="GcsFile"/> instance based on the specified object item.
    /// </summary>
    /// <param name="obj">The <see cref="GcsObject"/> representing the file.</param>
    /// <param name="normalizedPath">The normalized name of the object.</param>
    /// <returns>
    /// A new <see cref="GcsFile"/> instance representing the file.
    /// </returns>
    private GcsFile CreateVirtualFile(GcsObject obj, string? normalizedPath = null)
    {
        var properties = VirtualNodeProperties.CreateFileProperties(
            obj.TimeCreatedDateTimeOffset.GetValueOrDefault(),
            DateTimeOffset.MinValue,
            obj.UpdatedDateTimeOffset.GetValueOrDefault(),
            (long)(obj.Size ?? 0));

        var path = normalizedPath ?? VirtualPath.Normalize(obj.Name);
        return new GcsFile(_fs, path, properties);
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
