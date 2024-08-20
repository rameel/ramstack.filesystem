# Ramstack.FileSystem.Sub

Provides an implementation of `Ramstack.FileSystem` that wraps an underlying file system for managing files under a specific subpath.

## Getting Started

To install the `Ramstack.FileSystem.Sub` [NuGet package](https://www.nuget.org/packages/Ramstack.FileSystem.Sub)
in your project, run the following command:
```console
dotnet add package Ramstack.FileSystem.Sub
```
## Usage

```csharp
using Ramstack.FileSystem.Sub;

IVirtualFileSystem fileSystem = new PhysicalFileSystem(@"C:\project");

// Restrict the file system to only the contents within the "/app/assets" directory
// of the parent file system. Files and directories above the specified path will be inaccessible.
SubFileSystem fs = new SubFileSystem(path: "/app/assets", fileSystem);

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
