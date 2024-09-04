# Ramstack.FileSystem.Composite

Provides an implementation of `Ramstack.FileSystem` that combines multiple file systems into a single composite file system.

## Getting Started

To install the `Ramstack.FileSystem.Composite` [NuGet package](https://www.nuget.org/packages/Ramstack.FileSystem.Composite)
in your project, run the following command:
```console
dotnet add package Ramstack.FileSystem.Composite
```
## Usage

```csharp
using Ramstack.FileSystem.Composite;

CompositeFileSystem fs = new CompositeFileSystem(
    new PhysicalFileSystem(@"C:\dir-1"),
    new PhysicalFileSystem(@"C:\dir-2"),
    new PhysicalFileSystem(@"C:\dir-3"));

await foreach (VirtualFile file in fs.GetFilesAsync("/"))
{
    Console.WriteLine(node.Name);
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
- [Ramstack.FileSystem.Adapters](https://www.nuget.org/packages/Ramstack.FileSystem.Adapters) - Provides integration with `Microsoft.Extensions.FileProviders`.

## Supported versions

|      | Version |
|------|---------|
| .NET | 6, 7, 8 |

## Contributions

Bug reports and contributions are welcome.

## License

This package is released as open source under the **MIT License**.
See the [LICENSE](https://github.com/rameel/ramstack.virtualfiles/blob/main/LICENSE) file for more details.
