# Ramstack.FileSystem.Readonly

Provides an implementation of `Ramstack.FileSystem` that wraps the underlying file system, preventing any destructive operations.

## Getting Started

To install the `Ramstack.FileSystem.Readonly` [NuGet package](https://www.nuget.org/packages/Ramstack.FileSystem.Readonly)
in your project, run the following command:
```console
dotnet add package Ramstack.FileSystem.Readonly
```
## Usage

```csharp
using Ramstack.FileSystem.Readonly;

IVirtualFileSystem fileSystem = new PhysicalFileSystem(@"C:\path");
ReadonlyFileSystem fs = new ReadonlyFileSystem(fileSystem);

await foreach (VirtualFile file in fs.GetFilesAsync("/"))
{
    Console.WriteLine(node.Name);
}

// Throws an exception because the ReadonlyFileSystem does not support modifying operations
await fs.DeleteFileAsync("/hello.txt");
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
