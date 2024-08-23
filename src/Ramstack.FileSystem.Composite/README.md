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

## Supported versions

|      | Version |
|------|---------|
| .NET | 6, 7, 8 |

## Contributions

Bug reports and contributions are welcome.

## License

This package is released as open source under the **MIT License**.
See the [LICENSE](https://github.com/rameel/ramstack.virtualfiles/blob/main/LICENSE) file for more details.
