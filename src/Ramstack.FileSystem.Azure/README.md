# Ramstack.FileSystem.Azure

Provides an implementation of `Ramstack.FileSystem` based on Azure Blob Storage.

## Getting Started

To install the `Ramstack.FileSystem.Azure` [NuGet package](https://www.nuget.org/packages/Ramstack.FileSystem.Azure)
in your project, run the following command:
```console
dotnet add package Ramstack.FileSystem.Azure
```
## Usage

```csharp
using Ramstack.FileSystem.Azure;

AzureFileSystemOptions options = new AzureFileSystemOptions()
{
    ConnectionString = "...",
    Public = true
};

AzureFileSystem fs = new AzureFileSystem(containerName: "data", options);

await foreach (VirtualFile file in fs.GetFilesAsync("/"))
{
    Console.WriteLine(node.Name);
}
```

You can also configure the file system to be read-only:
```csharp
using Ramstack.FileSystem.Physical;

AzureFileSystem fs = new AzureFileSystem(containerName: "data", options)
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
