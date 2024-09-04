# Ramstack.FileSystem.Prefixed

Provides an implementation of `Ramstack.FileSystem` that adds a specified prefix to the file paths within the underlying file system.

## Getting Started

To install the `Ramstack.FileSystem.Prefixed` [NuGet package](https://www.nuget.org/packages/Ramstack.FileSystem.Prefixed)
in your project, run the following command:
```console
dotnet add package Ramstack.FileSystem.Prefixed
```
## Usage

```csharp
using Ramstack.FileSystem.Prefixed;

IVirtualFileSystem fileSystem = new PhysicalFileSystem(@"C:\path");

// Make all original files and directories accessible with the "/public/assets" prefix,
// as if they were originally located at that path.
//
// As an example, a file originally at "/hello.txt" will be accessed as "/public/assets/hello.txt".

PrefixedFileSystem fs = new PrefixedFileSystem(prefix: "/public/assets", fileSystem);

await foreach (VirtualFile file in fs.GetFilesAsync("/public/assets"))
{
    Console.WriteLine(file.FullName);
}
```

## Related Projects
- [Ramstack.FileSystem.Abstractions](https://www.nuget.org/packages/Ramstack.FileSystem.Abstractions) - Provides a virtual file system abstraction.
- [Ramstack.FileSystem.Physical](https://www.nuget.org/packages/Ramstack.FileSystem.Physical) - Provides an implementation based on the local file system.
- [Ramstack.FileSystem.Azure](https://www.nuget.org/packages/Ramstack.FileSystem.Azure) - Provides an implementation using Azure Blob storage.
- [Ramstack.FileSystem.Amazon](https://www.nuget.org/packages/Ramstack.FileSystem.Amazon) - Provides an implementation using Amazon S3 storage.
- [Ramstack.FileSystem.Zip](https://www.nuget.org/packages/Ramstack.FileSystem.Zip) - Provides an implementation based on ZIP archives.
- [Ramstack.FileSystem.Readonly](https://www.nuget.org/packages/Ramstack.FileSystem.Readonly) - Provides a read-only wrapper for the underlying file system.
- [Ramstack.FileSystem.Globbing](https://www.nuget.org/packages/Ramstack.FileSystem.Globbing) - Wraps the file system, filtering files and directories using glob patterns.
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
