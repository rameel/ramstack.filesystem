using Azure.Storage.Blobs.Models;

namespace Ramstack.FileSystem.Azure;

/// <summary>
/// Represents options for configuring an Azure Blob file system.
/// </summary>
public sealed class AzureFileSystemOptions
{
    /// <summary>
    /// Gets or sets the connection string for Azure Blob storage.
    /// </summary>
    public string? ConnectionString { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether blobs should be publicly accessible.
    /// </summary>
    public bool Public { get; init; }

    /// <summary>
    /// Occurs when blob HTTP headers need to be updated, allowing users to modify headers before AzureBlob is uploaded to Azure.
    /// </summary>
    public event EventHandler<HeadersUpdateEventArgs>? OnHeadersUpdate;

    /// <summary>
    /// Raises the <see cref="OnHeadersUpdate"/> event, notifying subscribers to update blob HTTP headers.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="BlobHttpHeaders"/> containing the headers to update.</param>
    internal void HeadersUpdate(object sender, HeadersUpdateEventArgs e) =>
        OnHeadersUpdate?.Invoke(sender, e);
}
