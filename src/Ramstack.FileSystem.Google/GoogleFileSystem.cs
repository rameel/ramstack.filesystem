using Google;
using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;

namespace Ramstack.FileSystem.Google;

/// <summary>
/// Represents a file system implementation using Google Cloud Storage.
/// </summary>
public sealed class GoogleFileSystem : IVirtualFileSystem
{
    /// <summary>
    /// Gets the <see cref="StorageClient"/> instance.
    /// </summary>
    internal StorageClient StorageClient { get; }

    /// <summary>
    /// Gets the bucket name.
    /// </summary>
    internal string BucketName { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the file system is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleFileSystem"/> class.
    /// </summary>
    /// <param name="client">The <see cref="StorageClient"/> instance.</param>
    /// <param name="bucketName">The name of the Google Cloud Storage bucket.</param>
    public GoogleFileSystem(StorageClient client, string bucketName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(bucketName);

        StorageClient = client;
        BucketName = bucketName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleFileSystem"/> class.
    /// </summary>
    /// <param name="credential">The Google credential to use for authentication.</param>
    /// <param name="bucketName">The name of the Google Cloud Storage bucket.</param>
    public GoogleFileSystem(GoogleCredential credential, string bucketName)
        : this(StorageClient.Create(credential), bucketName)
    {
    }

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path) =>
        new GcsDirectory(this, VirtualPath.Normalize(path));

    /// <inheritdoc />
    public VirtualFile GetFile(string path) =>
        new GcsFile(this, VirtualPath.Normalize(path));

    /// <summary>
    /// Asynchronously creates the bucket in Google Cloud Storage if it does not already exist.
    /// </summary>
    /// <param name="projectId">The Google Cloud project ID.</param>
    /// <param name="options">Additional options for the operation.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public async ValueTask CreateBucketAsync(string projectId, CreateBucketOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageClient.GetBucketAsync(BucketName, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await StorageClient
                .CreateBucketAsync(projectId, BucketName, options, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    void IDisposable.Dispose() =>
        StorageClient.Dispose();
}
