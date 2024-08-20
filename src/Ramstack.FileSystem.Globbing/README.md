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

## Supported versions

|      | Version |
|------|---------|
| .NET | 6, 7, 8 |

## Contributions

Bug reports and contributions are welcome.

## License

This package is released as open source under the **MIT License**.
See the [LICENSE](https://github.com/rameel/ramstack.virtualfiles/blob/main/LICENSE) file for more details.
