using Amazon;

namespace Ramstack.FileSystem.Amazon;

/// <summary>
/// Represents a set of options required for configuring access to Amazon S3 storage.
/// </summary>
/// <remarks>
/// This class holds the necessary credentials and configuration settings
/// for connecting to an Amazon S3 storage bucket.
/// </remarks>
public sealed class AmazonS3Options
{
    /// <summary>
    /// Gets or sets the access key ID used to authenticate with the Amazon S3 service.
    /// </summary>
    public /*required*/ string? AccessKeyId { get; init; }

    /// <summary>
    /// Gets or sets the access key secret used to authenticate with the Amazon S3 service.
    /// </summary>
    public /*required*/ string? AccessKeySecret { get; init; }

    /// <summary>
    /// Gets or sets the name of the Amazon S3 region where the storage bucket is located.
    /// </summary>
    public /*required*/ string? RegionName { get; init; }

    /// <summary>
    /// Gets the Amazon S3 region.
    /// </summary>
    public RegionEndpoint RegionEndpoint => RegionEndpoint.GetBySystemName(RegionName);
}
