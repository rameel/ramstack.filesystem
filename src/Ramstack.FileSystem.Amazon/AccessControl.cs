namespace Ramstack.FileSystem.Amazon;

/// <summary>
/// An enumeration of all possible CannedACLs that can be used
/// for S3 Buckets or S3 Objects. For more information about CannedACLs, refer to
/// <see href="http://docs.amazonwebservices.com/AmazonS3/latest/RESTAccessPolicy.html#RESTCannedAccessPolicies"/>.
/// </summary>
public enum AccessControl
{
    /// <summary>
    /// Owner gets FULL_CONTROL.
    /// No one else has access rights (default).
    /// </summary>
    NoAcl,

    /// <summary>
    /// Owner gets FULL_CONTROL.
    /// No one else has access rights (default).
    /// </summary>
    Private,

    /// <summary>
    /// Owner gets FULL_CONTROL and the anonymous principal is granted READ access.
    /// If this policy is used on an object, it can be read from a browser with no authentication.
    /// </summary>
    PublicRead,

    /// <summary>
    /// Owner gets FULL_CONTROL, the anonymous principal is granted READ and WRITE access.
    /// This can be a useful policy to apply to a bucket, but is generally not recommended.
    /// </summary>
    PublicReadWrite,

    /// <summary>
    /// Owner gets FULL_CONTROL, and any principal authenticated as a registered Amazon
    /// S3 user is granted READ access.
    /// </summary>
    AuthenticatedRead,

    /// <summary>
    /// Owner gets FULL_CONTROL. Amazon EC2 gets READ access to GET an
    /// Amazon Machine Image (AMI) bundle from Amazon S3.
    /// </summary>
    AwsExecRead,

    /// <summary>
    /// Object Owner gets FULL_CONTROL, Bucket Owner gets READ
    /// This ACL applies only to objects and is equivalent to private when used with PUT Bucket.
    /// You use this ACL to let someone other than the bucket owner write content (get full control)
    /// in the bucket but still grant the bucket owner read access to the objects.
    /// </summary>
    BucketOwnerRead,

    /// <summary>
    /// Object Owner gets FULL_CONTROL, Bucket Owner gets FULL_CONTROL.
    /// This ACL applies only to objects and is equivalent to private when used with PUT Bucket.
    /// You use this ACL to let someone other than the bucket owner write content (get full control)
    /// in the bucket but still grant the bucket owner full rights over the objects.
    /// </summary>
    BucketOwnerFullControl,

    /// <summary>
    /// The LogDelivery group gets WRITE and READ_ACP permissions on the bucket.
    /// </summary>
    LogDeliveryWrite
}
