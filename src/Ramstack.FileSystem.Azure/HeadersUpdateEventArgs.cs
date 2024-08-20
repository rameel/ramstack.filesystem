using Azure.Storage.Blobs.Models;

namespace Ramstack.FileSystem.Azure;

/// <summary>
/// Provides event arguments containing the path and blob HTTP headers.
/// </summary>
/// <param name="path">The path associated with the blob.</param>
/// <param name="headers">The blob HTTP headers to update.</param>
public sealed class HeadersUpdateEventArgs(string path, BlobHttpHeaders headers) : EventArgs
{
    /// <summary>
    /// Gets the path associated with the blob.
    /// </summary>
    public string Path => path;

    /// <summary>
    /// Gets the blob HTTP headers to update.
    /// </summary>
    public BlobHttpHeaders Headers => headers;
}
