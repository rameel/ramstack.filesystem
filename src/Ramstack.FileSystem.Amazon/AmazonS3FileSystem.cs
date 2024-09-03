using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Amazon;

/// <summary>
/// Represents a file system implementation using Amazon S3 storage.
/// </summary>
public sealed class AmazonS3FileSystem : IVirtualFileSystem
{
    /// <summary>
    /// Gets the <see cref="IAmazonS3"/> instance.
    /// </summary>
    internal IAmazonS3 AmazonClient { get; }

    /// <summary>
    /// Gets the name of the S3 bucket used by this file system.
    /// </summary>
    internal string BucketName { get; }

    /// <inheritdoc />
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonS3FileSystem"/> class using an existing Amazon S3 client.
    /// </summary>
    /// <param name="client">The Amazon S3 client instance.</param>
    /// <param name="bucketName">The name of the S3 bucket.</param>
    public AmazonS3FileSystem(AmazonS3Client client, string bucketName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(bucketName);

        (AmazonClient, BucketName) = (client, bucketName);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonS3FileSystem"/> class using AWS credentials and configuration.
    /// </summary>
    /// <param name="credentials">The AWS credentials to use.</param>
    /// <param name="config">The Amazon S3 configuration settings.</param>
    /// <param name="bucketName">The name of the S3 bucket.</param>
    public AmazonS3FileSystem(AWSCredentials credentials, AmazonS3Config config, string bucketName)
    {
        ArgumentNullException.ThrowIfNull(bucketName);

        AmazonClient = new AmazonS3Client(credentials, config);
        BucketName = bucketName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonS3FileSystem"/> class using AWS access keys and region name.
    /// </summary>
    /// <param name="awsAccessKeyId">The AWS access key ID.</param>
    /// <param name="awsSecretAccessKey">The AWS secret access key.</param>
    /// <param name="regionName">The name of the AWS region.</param>
    /// <param name="bucketName">The name of the S3 bucket.</param>
    public AmazonS3FileSystem(string awsAccessKeyId, string awsSecretAccessKey, string regionName, string bucketName)
        : this(awsAccessKeyId, awsSecretAccessKey, RegionEndpoint.GetBySystemName(regionName), bucketName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonS3FileSystem"/> class using AWS access keys and region endpoint.
    /// </summary>
    /// <param name="awsAccessKeyId">The AWS access key ID.</param>
    /// <param name="awsSecretAccessKey">The AWS secret access key.</param>
    /// <param name="region">The AWS region endpoint.</param>
    /// <param name="bucketName">The name of the S3 bucket.</param>
    public AmazonS3FileSystem(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region, string bucketName)
        : this(new BasicAWSCredentials(awsAccessKeyId, awsSecretAccessKey), new AmazonS3Config { RegionEndpoint = region }, bucketName)
    {
    }

    /// <inheritdoc />
    public VirtualFile GetFile(string path)
    {
        path = VirtualPath.GetFullPath(path);
        return new S3File(this, path);
    }

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path)
    {
        path = VirtualPath.GetFullPath(path);
        return new S3Directory(this, path);
    }

    /// <inheritdoc />
    public void Dispose() =>
        AmazonClient.Dispose();

    /// <summary>
    /// Creates the S3 bucket if it does not already exist.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public ValueTask CreateBucketAsync(CancellationToken cancellationToken = default) =>
        CreateBucketAsync(AccessControl.NoAcl, cancellationToken);

    /// <summary>
    /// Creates the S3 bucket if it does not already exist.
    /// </summary>
    /// <param name="accessControl">The ACL to apply to the bucket.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public async ValueTask CreateBucketAsync(AccessControl accessControl, CancellationToken cancellationToken = default)
    {
        var exists = AmazonS3Util
            .DoesS3BucketExistV2Async(AmazonClient, BucketName)
            .ConfigureAwait(false);

        if (await exists)
            return;

        var request = new PutBucketRequest
        {
            BucketName = BucketName,
            UseClientRegion = true,
            CannedACL = accessControl switch
            {
                AccessControl.NoAcl => S3CannedACL.NoACL,
                AccessControl.Private => S3CannedACL.Private,
                AccessControl.PublicRead => S3CannedACL.PublicRead,
                AccessControl.PublicReadWrite => S3CannedACL.PublicReadWrite,
                AccessControl.AuthenticatedRead => S3CannedACL.AuthenticatedRead,
                AccessControl.AwsExecRead => S3CannedACL.AWSExecRead,
                AccessControl.BucketOwnerRead => S3CannedACL.BucketOwnerRead,
                AccessControl.BucketOwnerFullControl => S3CannedACL.BucketOwnerFullControl,
                AccessControl.LogDeliveryWrite => S3CannedACL.LogDeliveryWrite,
                _ => throw new ArgumentOutOfRangeException(nameof(accessControl))
            }
        };

        await AmazonClient
            .PutBucketAsync(request, cancellationToken)
            .ConfigureAwait(false);
    }
}
