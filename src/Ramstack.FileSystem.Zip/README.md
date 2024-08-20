# Ramstack.FileSystem.Zip

Provides an implementation of `Ramstack.FileSystem` based on ZIP archives.

## Getting Started

To install the `Ramstack.FileSystem.Zip` [NuGet package](https://www.nuget.org/packages/Ramstack.FileSystem.Zip)
in your project, run the following command:
```console
dotnet add package Ramstack.FileSystem.Zip
```
## Usage

```csharp
using Ramstack.FileSystem.Zip;

using ZipFileSystem fs = new ZipFileSystem(@"C:\MyArchive.zip");

await foreach (VirtualFile file in fs.GetFilesAsync("/"))
{
    Console.WriteLine(node.Name);
}
```

## Remark
`ZipFileSystem` is a read-only implementation and does not support for modifying operations.

## Supported versions

|      | Version |
|------|---------|
| .NET | 6, 7, 8 |

## Contributions

Bug reports and contributions are welcome.

## License

This package is released as open source under the **MIT License**.
See the [LICENSE](https://github.com/rameel/ramstack.virtualfiles/blob/main/LICENSE) file for more details.
