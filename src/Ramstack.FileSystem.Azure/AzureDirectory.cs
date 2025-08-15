using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

using Ramstack.Globbing;

namespace Ramstack.FileSystem.Azure;

/// <summary>
/// Represents an implementation of <see cref="VirtualDirectory"/> that maps a directory
/// to the path within an Azure Blob Storage container.
/// </summary>
internal sealed class AzureDirectory : VirtualDirectory
{
    private readonly AzureFileSystem _fs;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureDirectory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The path to the directory within the Azure Blob Storage container.</param>
    public AzureDirectory(AzureFileSystem fileSystem, string path) : base(path) =>
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
        var collection = _fs.AzureClient
            .GetBlobsAsync(
                prefix: GetPrefix(FullName),
                cancellationToken: cancellationToken);

        var client = _fs.AzureClient.GetBlobBatchClient();
        var batch = client.CreateBatch();

        // https://learn.microsoft.com/en-us/rest/api/storageservices/blob-batch#remarks
        // Each batch request supports a maximum of 256 sub requests.
        const int MaxSubRequests = 256;

        await foreach (var page in collection.AsPages().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            foreach (var blob in page.Values)
            {
                batch.DeleteBlob(_fs.AzureClient.Name, blob.Name, DeleteSnapshotsOption.IncludeSnapshots, conditions: null);

                if (batch.RequestCount != MaxSubRequests)
                    continue;

                await DeleteBlobsAsync(client, batch, cancellationToken).ConfigureAwait(false);
                batch = client.CreateBatch();
            }
        }

        if (batch.RequestCount != 0)
            await DeleteBlobsAsync(client, batch, cancellationToken).ConfigureAwait(false);

        static async ValueTask DeleteBlobsAsync(BlobBatchClient client, BlobBatch batch, CancellationToken cancellationToken)
        {
            try
            {
                await client.SubmitBatchAsync(batch, throwOnAnyFailure: true, cancellationToken).ConfigureAwait(false);
            }
            catch (AggregateException e) when (Processed(e.InnerExceptions))
            {
            }
            finally
            {
                batch.Dispose();
            }

            static bool Processed(ReadOnlyCollection<Exception> exceptions)
            {
                for (int count = exceptions.Count, i = 0; i < count; i++)
                    if (exceptions[i] is not RequestFailedException { Status: 404 })
                        return false;

                return true;
            }
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var collection = _fs.AzureClient
            .GetBlobsByHierarchyAsync(
                delimiter: "/",
                prefix: GetPrefix(FullName),
                cancellationToken: cancellationToken);

        await foreach (var page in collection.AsPages().WithCancellation(cancellationToken).ConfigureAwait(false))
        foreach (var item in page.Values)
            yield return item.Prefix is not null
                ? new AzureDirectory(_fs, VirtualPath.Normalize(item.Prefix))
                : CreateVirtualFile(item.Blob);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var collection = _fs.AzureClient
            .GetBlobsByHierarchyAsync(
                delimiter: "/",
                prefix: GetPrefix(FullName),
                cancellationToken: cancellationToken);

        await foreach (var page in collection.AsPages().WithCancellation(cancellationToken).ConfigureAwait(false))
        foreach (var item in page.Values)
            if (item.Prefix is null)
                yield return CreateVirtualFile(item.Blob);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var collection = _fs.AzureClient
            .GetBlobsByHierarchyAsync(
                delimiter: "/",
                prefix: GetPrefix(FullName),
                cancellationToken: cancellationToken);

        await foreach (var page in collection.AsPages().WithCancellation(cancellationToken).ConfigureAwait(false))
        foreach (var item in page.Values)
            if (item.Prefix is not null)
                yield return new AzureDirectory(_fs, VirtualPath.Normalize(item.Prefix));
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync(string[] patterns, string[]? excludes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        //
        // Cloud storage optimization strategy:
        // 1. List all blobs in one batch using prefix filtering (matches our virtual directory).
        // 2. Perform pattern matching and exclusion filters locally.
        //
        // Benefits:
        // - Single API call instead of per-directory requests
        // - Reduced network latency, especially for deep directory structures
        // - More efficient than recursive directory scanning
        //

        var prefix = GetPrefix(FullName);
        var directories = new HashSet<string> { FullName };

        await foreach (var page in _fs.AzureClient
            .GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken)
            .AsPages()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            foreach (var blob in page.Values)
            {
                var path = VirtualPath.Normalize(blob.Name);
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
                        yield return new AzureDirectory(_fs, directoryPath);

                    directoryPath = VirtualPath.GetDirectoryName(directoryPath);
                }

                if (IsMatched(blob.Name.AsSpan(prefix.Length), patterns, excludes))
                    yield return CreateVirtualFile(blob, path);
            }
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync(string[] patterns, string[]? excludes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        //
        // Cloud storage optimization strategy:
        // 1. List all blobs in one batch using prefix filtering (matches our virtual directory).
        // 2. Perform pattern matching and exclusion filters locally.
        //
        // Benefits:
        // - Single API call instead of per-directory requests
        // - Reduced network latency, especially for deep directory structures
        // - More efficient than recursive directory scanning
        //

        var prefix = GetPrefix(FullName);

        await foreach (var page in _fs.AzureClient
            .GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken)
            .AsPages()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            foreach (var blob in page.Values)
                if (IsMatched(blob.Name.AsSpan(prefix.Length), patterns, excludes))
                    yield return CreateVirtualFile(blob);
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync(string[] patterns, string[]? excludes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        //
        // Cloud storage optimization strategy:
        // 1. List all blobs in one batch using prefix filtering (matches our virtual directory).
        // 2. Perform pattern matching and exclusion filters locally.
        //
        // Benefits:
        // - Single API call instead of per-directory requests
        // - Reduced network latency, especially for deep directory structures
        // - More efficient than recursive directory scanning
        //

        var prefix = GetPrefix(FullName);
        var directories = new HashSet<string> { FullName };

        await foreach (var page in _fs.AzureClient
            .GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken)
            .AsPages()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            foreach (var blob in page.Values)
            {
                var directoryPath = VirtualPath.GetDirectoryName(
                    VirtualPath.Normalize(blob.Name));

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
                        yield return new AzureDirectory(_fs, directoryPath);

                    directoryPath = VirtualPath.GetDirectoryName(directoryPath);
                }
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="AzureFile"/> instance based on the specified blob item.
    /// </summary>
    /// <param name="blob">The <see cref="BlobItem"/> representing the file.</param>
    /// <param name="normalizedPath">The normalized name of the blob.</param>
    /// <returns>
    /// A new <see cref="AzureFile"/> instance representing the file.
    /// </returns>
    private AzureFile CreateVirtualFile(BlobItem blob, string? normalizedPath = null)
    {
        var info = blob.Properties;
        var properties = VirtualNodeProperties.CreateFileProperties(
            creationTime: info.CreatedOn.GetValueOrDefault(),
            lastAccessTime: info.LastAccessedOn.GetValueOrDefault(),
            lastWriteTime: info.LastModified.GetValueOrDefault(),
            length: info.ContentLength.GetValueOrDefault(defaultValue: -1));

        var path = normalizedPath ?? VirtualPath.Normalize(blob.Name);
        return new AzureFile(_fs, path, properties);
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
    private static string GetPrefix(string path) =>
        path == "/" ? "" : $"{path[1..]}/";
}
