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

## Supported versions

|      | Version |
|------|---------|
| .NET | 6, 7, 8 |

## Contributions

Bug reports and contributions are welcome.

## License

This package is released as open source under the **MIT License**.
See the [LICENSE](https://github.com/rameel/ramstack.virtualfiles/blob/main/LICENSE) file for more details.
