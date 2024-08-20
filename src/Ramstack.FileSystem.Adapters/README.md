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

## Supported versions

|      | Version |
|------|---------|
| .NET | 6, 7, 8 |

## Contributions

Bug reports and contributions are welcome.

## License

This package is released as open source under the **MIT License**.
See the [LICENSE](https://github.com/rameel/ramstack.virtualfiles/blob/main/LICENSE) file for more details.
