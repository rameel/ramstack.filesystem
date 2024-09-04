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

## Related Projects
- [Ramstack.FileSystem.Abstractions](https://www.nuget.org/packages/Ramstack.FileSystem.Abstractions) - Provides a virtual file system abstraction.
- [Ramstack.FileSystem.Physical](https://www.nuget.org/packages/Ramstack.FileSystem.Physical) - Provides an implementation based on the local file system.
- [Ramstack.FileSystem.Azure](https://www.nuget.org/packages/Ramstack.FileSystem.Azure) - Provides an implementation using Azure Blob storage.
- [Ramstack.FileSystem.Zip](https://www.nuget.org/packages/Ramstack.FileSystem.Zip) - Provides an implementation based on ZIP archives.
- [Ramstack.FileSystem.Readonly](https://www.nuget.org/packages/Ramstack.FileSystem.Readonly) - Provides a read-only wrapper for the underlying file system.
- [Ramstack.FileSystem.Globbing](https://www.nuget.org/packages/Ramstack.FileSystem.Globbing) - Wraps the file system, filtering files and directories using glob patterns.
- [Ramstack.FileSystem.Prefixed](https://www.nuget.org/packages/Ramstack.FileSystem.Prefixed) - Adds a prefix to file paths within the underlying file system.
- [Ramstack.FileSystem.Sub](https://www.nuget.org/packages/Ramstack.FileSystem.Sub) - Wraps the underlying file system, restricting access to a specific subpath.
- [Ramstack.FileSystem.Adapters](https://www.nuget.org/packages/Ramstack.FileSystem.Adapters) - Provides integration with `Microsoft.Extensions.FileProviders`.
- [Ramstack.FileSystem.Composite](https://www.nuget.org/packages/Ramstack.FileSystem.Composite) - Provides an implementation that combines multiple file systems into a single composite file system.

## Supported versions

|      | Version |
|------|---------|
| .NET | 6, 7, 8 |

## Contributions

Bug reports and contributions are welcome.

## License

This package is released as open source under the **MIT License**.
See the [LICENSE](https://github.com/rameel/ramstack.virtualfiles/blob/main/LICENSE) file for more details.
