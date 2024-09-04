# Ramstack.FileSystem.Adapters

Provides an implementation of `Ramstack.FileSystem` for integrating with `Microsoft.Extensions.FileProviders`.

## Getting Started

To install the `Ramstack.FileSystem.Adapters` [NuGet package](https://www.nuget.org/packages/Ramstack.FileSystem.Adapters)
in your project, run the following command:
```console
dotnet add package Ramstack.FileSystem.Adapters
```
## Usage

```csharp
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Ramstack.FileSystem.Adapters;

// Create a physical file provider that points to the specified directory
IFileProvider provider = new PhysicalFileProvider(@"C:\path");

// Create a virtual file system adapter that wraps the physical file provider.
//
// An added benefit is that now information about the entire file hierarchy is available
// for files and directories provided by the underlying provider.
IVirtualFileSystem fs = new VirtualFileSystemAdapter(provider);

await foreach (VirtualFile file in fs.GetFilesAsync("/"))
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
- [Ramstack.FileSystem.Prefixed](https://www.nuget.org/packages/Ramstack.FileSystem.Prefixed) - Adds a prefix to file paths within the underlying file system.
- [Ramstack.FileSystem.Sub](https://www.nuget.org/packages/Ramstack.FileSystem.Sub) - Wraps the underlying file system, restricting access to a specific subpath.
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
