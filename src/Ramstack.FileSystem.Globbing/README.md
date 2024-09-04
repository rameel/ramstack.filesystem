# Ramstack.FileSystem.Globbing

Provides an implementation of `Ramstack.FileSystem` that applies glob-based filtering rules to determine
which files and directories of the underlying file system to include or exclude.

It relies on the [Ramstack.Globbing](https://www.nuget.org/packages/Ramstack.Globbing) package for its globbing capabilities.

## Getting Started

To install the `Ramstack.FileSystem.Globbing` [NuGet package](https://www.nuget.org/packages/Ramstack.FileSystem.Globbing)
in your project, run the following command:
```console
dotnet add package Ramstack.FileSystem.Globbing
```
## Usage

```csharp
using Ramstack.FileSystem.Globbing;

PhysicalFileSystem fileSystem = new PhysicalFileSystem(@"C:\path");

// Include only "*.txt" files and "*.md" files in the "/docs" directory, excluding any "README.md" file
GlobbingFileSystem fs = new GlobbingFileSystem(fileSystem,
    patterns: ["**/*.txt", "docs/*.md"],
    excludes: ["**/README.md"]);

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
