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
}
