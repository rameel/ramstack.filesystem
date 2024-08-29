using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

using Ramstack.FileSystem.Internal;

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
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        var properties = VirtualNodeProperties.CreateDirectoryProperties(default, default, default);
        return new ValueTask<VirtualNodeProperties?>(properties);
    }

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken) =>
        new ValueTask<bool>(true);

    /// <inheritdoc />
    protected override ValueTask CreateCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override async ValueTask DeleteCoreAsync(CancellationToken cancellationToken)
    {
        var collection = _fs.Container
            .GetBlobsAsync(
                prefix: GetPrefix(FullName),
                cancellationToken: cancellationToken);

        var blobs = new List<Uri>();
        var builder = new BlobUriBuilder(_fs.Container.Uri);
        var batch = _fs.Container.GetBlobBatchClient();

        await foreach (var blob in collection.ConfigureAwait(false))
        {
            blobs.Add(GenerateBlobUri(builder, blob.Name));

            // https://learn.microsoft.com/en-us/rest/api/storageservices/blob-batch#remarks
            // Each batch request supports a maximum of 256 sub requests.
            const int MaxSubRequests = 256;

            if (blobs.Count != MaxSubRequests)
                continue;

            await DeleteBlobsAsync(batch, blobs, cancellationToken).ConfigureAwait(false);
            blobs.Clear();
        }

        if (blobs.Count != 0)
            await DeleteBlobsAsync(batch, blobs, cancellationToken).ConfigureAwait(false);

        static async ValueTask DeleteBlobsAsync(BlobBatchClient batch, List<Uri> blobs, CancellationToken cancellationToken)
        {
            try
            {
                await batch
                    .DeleteBlobsAsync(
                        blobs,
                        DeleteSnapshotsOption.IncludeSnapshots,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (AggregateException e) when (Processed(e.InnerExceptions))
            {
            }

            static bool Processed(ReadOnlyCollection<Exception> exceptions)
            {
                for (var i = 0; i < exceptions.Count; i++)
                    if (exceptions[i] is not RequestFailedException { Status: 404 })
                        return false;

                return true;
            }
        }

        static Uri GenerateBlobUri(BlobUriBuilder builder, string blobName)
        {
            builder.BlobName = blobName;
            return builder.ToUri();
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var collection = _fs.Container
            .GetBlobsByHierarchyAsync(
                delimiter: "/",
                prefix: GetPrefix(FullName),
                cancellationToken: cancellationToken);

        await foreach (var item in collection.ConfigureAwait(false))
            yield return item.Prefix is not null
                ? new AzureDirectory(_fs, VirtualPath.Normalize(item.Prefix))
                : CreateVirtualFile(item.Blob);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var collection = _fs.Container
            .GetBlobsByHierarchyAsync(
                delimiter: "/",
                prefix: GetPrefix(FullName),
                cancellationToken: cancellationToken);

        await foreach (var item in collection.ConfigureAwait(false))
            if (item.Prefix is null)
                yield return CreateVirtualFile(item.Blob);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var collection = _fs.Container
            .GetBlobsByHierarchyAsync(
                delimiter: "/",
                prefix: GetPrefix(FullName),
                cancellationToken: cancellationToken);

        await foreach (var item in collection.ConfigureAwait(false))
            if (item.Prefix is not null)
                yield return new AzureDirectory(_fs, VirtualPath.Normalize(item.Prefix));
    }

    /// <summary>
    /// Creates a <see cref="VirtualFile"/> instance based on the specified blob item.
    /// </summary>
    /// <param name="blob">The <see cref="BlobItem"/> representing the file.</param>
    /// <returns>
    /// A new <see cref="AzureFile"/> instance representing the file.
    /// </returns>
    private VirtualFile CreateVirtualFile(BlobItem blob)
    {
        var info = blob.Properties;
        var properties = VirtualNodeProperties.CreateFileProperties(
            creationTime: info.CreatedOn.GetValueOrDefault(),
            lastAccessTime: info.LastAccessedOn.GetValueOrDefault(),
            lastWriteTime: info.LastModified.GetValueOrDefault(),
            length: info.ContentLength.GetValueOrDefault(defaultValue: -1));

        var path = VirtualPath.Normalize(blob.Name);
        return new AzureFile(_fs, path, properties);
    }

    /// <summary>
    /// Returns the blob prefix for the specified directory path.
    /// </summary>
    /// <param name="directoryPath">The directory path for which to get the prefix.</param>
    /// <returns>
    /// The blob prefix associated with the directory.
    /// </returns>
    private static string GetPrefix(string directoryPath) =>
        directoryPath == "/" ? "" : $"{directoryPath[1..]}/";
}
