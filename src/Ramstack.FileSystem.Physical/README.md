# Ramstack.FileSystem.Physical

Provides an implementation of `Ramstack.FileSystem` based on local file system.

## Getting Started

To install the `Ramstack.FileSystem.Physical` [NuGet package](https://www.nuget.org/packages/Ramstack.FileSystem.Physical)
in your project, run the following command:
```console
dotnet add package Ramstack.FileSystem.Physical
```
## Usage

```csharp
using Ramstack.FileSystem.Physical;

PhysicalFileSystem fs = new PhysicalFileSystem(@"C:\path\to\directory");

await foreach (VirtualFile file in fs.GetFilesAsync("/"))
{
    Console.WriteLine(node.Name);
}
```

You can also configure the file system to be read-only:
```csharp
using Ramstack.FileSystem.Physical;

PhysicalFileSystem fs = new PhysicalFileSystem(@"C:\path\to\directory")
{
    IsReadOnly = true
};
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
