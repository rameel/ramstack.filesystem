# Ramstack.FileSystem.Amazon

Provides an implementation of `Ramstack.FileSystem` using Amazon S3 storage.

## Getting Started

To install the `Ramstack.FileSystem.Amazon` [NuGet package](https://www.nuget.org/packages/Ramstack.FileSystem.Amazon)
in your project, run the following command:
```console
dotnet add package Ramstack.FileSystem.Amazon
```

## Usage

```csharp
using Ramstack.FileSystem.Amazon;

AmazonS3FileSystem fs = new AmazonS3FileSystem(
    accessKeyId: "...",
    secretAccessKey: "...",
    region: RegionEndpoint.USEast1,
    bucketName: "my-storage");

// Create S3 bucket if it doesn't exist
await fs.CreateBucketAsync(AccessControl.Private);

await foreach (VirtualFile file in fs.GetFilesAsync("/"))
{
    Console.WriteLine(node.Name);
}
```

You can also configure the file system to be read-only:
```csharp
AmazonS3FileSystem fs = new AmazonS3FileSystem(
    accessKeyId: "...",
    secretAccessKey: "...",
    region: RegionEndpoint.USEast1,
    bucketName: "my-storage")
{
    IsReadOnly = true
};
```

## Supported versions

|      | Version |
|------|---------|
| .NET | 6, 7, 8 |

## Contributions

Bug reports and contributions are welcome.

## License

This package is released as open source under the **MIT License**.
See the [LICENSE](https://github.com/rameel/ramstack.virtualfiles/blob/main/LICENSE) file for more details.
