using System.Diagnostics;

using Azure.Core;
using Azure.Storage;
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
    /// Gets the <see cref="BlobContainerClient"/> instance.
    /// </summary>
    internal BlobContainerClient AzureClient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFileSystem"/> class.
    /// </summary>
    /// <param name="connectionString">A connection string includes the authentication information required for your application
    /// to access data in an Azure Storage account at runtime.
    ///
    /// For more information, <see href="https://docs.microsoft.com/azure/storage/common/storage-configure-connection-string">
    /// Configure Azure Storage connection strings</see>
    /// </param>
    /// <param name="containerName">The name of the blob container in the storage account to reference.</param>
    public AzureFileSystem(string connectionString, string containerName) =>
        AzureClient = new BlobContainerClient(connectionString, containerName);

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFileSystem"/> class.
    /// </summary>
    /// <param name="connectionString">A connection string includes the authentication information required for your application
    /// to access data in an Azure Storage account at runtime.
    ///
    /// For more information, <see href="https://docs.microsoft.com/azure/storage/common/storage-configure-connection-string">
    /// Configure Azure Storage connection strings</see>
    /// </param>
    /// <param name="containerName">The name of the blob container in the storage account to reference.</param>
    /// <param name="options">Optional client options that define the transport pipeline policies
    /// for authentication, retries, etc., that are applied to every request.</param>
    public AzureFileSystem(string connectionString, string containerName, BlobClientOptions? options) =>
        AzureClient = new BlobContainerClient(connectionString, containerName, options);

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFileSystem"/> class.
    /// </summary>
    /// <param name="containerUri">A <see cref="BlobContainerClient.Uri" /> referencing the blob container
    /// that includes the name of the account and the name of the container. This is likely to be similar
    /// to <c>https://{account_name}.blob.core.windows.net/{container_name}</c>.</param>
    /// <param name="options">Optional client options that define the transport pipeline policies
    /// for authentication, retries, etc., that are applied to every request.</param>
    public AzureFileSystem(Uri containerUri, BlobClientOptions? options = null) =>
        AzureClient = new BlobContainerClient(containerUri, options);

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFileSystem"/> class.
    /// </summary>
    /// <param name="containerUri">A <see cref="BlobContainerClient.Uri" /> referencing the blob container
    /// that includes the name of the account and the name of the container. This is likely to be similar
    /// to <c>https://{account_name}.blob.core.windows.net/{container_name}</c>.</param>
    /// <param name="credential">The shared key credential used to sign requests.</param>
    /// <param name="options">Optional client options that define the transport pipeline policies
    /// for authentication, retries, etc., that are applied to every request.</param>
    public AzureFileSystem(Uri containerUri, StorageSharedKeyCredential credential, BlobClientOptions? options = null) =>
        AzureClient = new BlobContainerClient(containerUri, credential, options);

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFileSystem"/> class.
    /// </summary>
    /// <param name="containerUri">A <see cref="BlobContainerClient.Uri" /> referencing the blob container
    /// that includes the name of the account and the name of the container. This is likely to be similar
    /// to <c>https://{account_name}.blob.core.windows.net/{container_name}</c>.</param>
    /// <param name="credential">The token credential used to sign requests.</param>
    /// <param name="options">Optional client options that define the transport pipeline policies
    /// for authentication, retries, etc., that are applied to every request.</param>
    public AzureFileSystem(Uri containerUri, TokenCredential credential, BlobClientOptions? options = null) =>
        AzureClient = new BlobContainerClient(containerUri, credential, options);

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFileSystem"/> class.
    /// </summary>
    /// <param name="client">The <see cref="BlobContainerClient"/> instance used to interact with the Azure Blob storage container.</param>
    public AzureFileSystem(BlobContainerClient client) =>
        AzureClient = client;

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
    public ValueTask CreateContainerAsync(CancellationToken cancellationToken = default) =>
        CreateContainerAsync(PublicAccessType.None, cancellationToken);

    /// <summary>
    /// Asynchronously creates the container in Azure Blob Storage if it does not already exist.
    /// </summary>
    /// <param name="accessType"> Specifies whether data in the container may be accessed publicly and the level of access.
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="PublicAccessType.BlobContainer" />: Specifies full public read access for both the container and blob data.
    ///       Clients can enumerate blobs within the container via anonymous requests, but cannot enumerate containers within the storage account.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="PublicAccessType.Blob" />: Specifies public read access for blobs only. Blob data within this container can be
    ///       read via anonymous requests, but container data is not available. Clients cannot enumerate blobs within the container via anonymous requests.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="PublicAccessType.None" />: Specifies that the container data is private to the account owner.
    ///     </description>
    ///   </item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public ValueTask CreateContainerAsync(PublicAccessType accessType, CancellationToken cancellationToken = default)
    {
        var task = AzureClient.CreateIfNotExistsAsync(accessType, cancellationToken: cancellationToken);
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
        return AzureClient.GetBlobClient(path[1..]);
    }
}
