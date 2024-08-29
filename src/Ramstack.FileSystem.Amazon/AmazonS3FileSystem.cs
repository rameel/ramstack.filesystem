using Amazon.S3;

namespace Ramstack.FileSystem.Amazon;

public sealed class AmazonS3FileSystem : IVirtualFileSystem
{
    internal IAmazonS3 AmazonClient { get; }
    internal string BucketName { get; }

    public bool IsReadOnly { get; init; }

    public AmazonS3FileSystem(AmazonS3Client client, string bucketName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(bucketName);

        (AmazonClient, BucketName) = (client, bucketName);
    }

    public AmazonS3FileSystem(AmazonS3Options options, string bucketName)
    {
        ArgumentNullException.ThrowIfNull(bucketName);

        AmazonClient = new AmazonS3Client(
            awsAccessKeyId: options.AccessKeyId,
            awsSecretAccessKey: options.AccessKeySecret,
            region: options.RegionEndpoint);
        BucketName = bucketName;
    }

    public VirtualFile GetFile(string path)
    {
        throw new NotImplementedException();
    }

    public VirtualDirectory GetDirectory(string path)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
