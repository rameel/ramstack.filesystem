using System.Diagnostics;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Azure;

/// <summary>
/// Represents a file system implementation using Azure Blob Storage.
/// </summary>
public sealed class AzureFileSystem : IVirtualFileSystem
{
    /// <summary>
    /// Gets or sets a value indicating whether the file system is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Gets the options used to configure the file system.
    /// </summary>
    public AzureFileSystemOptions Options { get; }

    /// <summary>
    /// Gets the <see cref="BlobContainerClient"/> instance.
    /// </summary>
    internal BlobContainerClient Container { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFileSystem"/> class using the specified container name and configuration options.
    /// </summary>
    /// <param name="containerName">The name of the container in Azure Blob Storage.</param>
    /// <param name="options">The <see cref="AzureFileSystemOptions"/> used to configure the file system.</param>
    public AzureFileSystem(string containerName, AzureFileSystemOptions options)
    {
        Options = options;
        Container = new BlobContainerClient(options.ConnectionString, containerName);
    }

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path) =>
        new AzureDirectory(this, VirtualPath.GetFullPath(path));

    /// <inheritdoc />
    public VirtualFile GetFile(string path) =>
        new AzureFile(this, VirtualPath.GetFullPath(path));

    /// <summary>
    /// Asynchronously creates the container in Azure Blob Storage if it does not already exist.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public ValueTask CreateContainerAsync(CancellationToken cancellationToken = default)
    {
        var task = Container.CreateIfNotExistsAsync(
            Options.Public ? PublicAccessType.Blob : PublicAccessType.None,
            cancellationToken: cancellationToken);
        return new ValueTask(task);
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
    }

    /// <summary>
    /// Creates a new <see cref="BlobClient"/> object for the specified path.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>
    /// The <see cref="BlobClient"/> object.
    /// </returns>
    internal BlobClient CreateBlobClient(string path)
    {
        Debug.Assert(path == VirtualPath.GetFullPath(path));
        return Container.GetBlobClient(path[1..]);
    }

    /// <summary>
    /// Returns the <see cref="BlobHttpHeaders"/> for the blob located at the specified path.
    /// </summary>
    /// <param name="path">The path to the blob.</param>
    /// <returns>
    /// The <see cref="BlobHttpHeaders"/> associated with the blob at the specified path.
    /// </returns>
    internal BlobHttpHeaders GetBlobHeaders(string path)
    {
        var headers = new BlobHttpHeaders();
        Options.HeadersUpdate(this, new HeadersUpdateEventArgs(path, headers));
        return headers;
    }
}
