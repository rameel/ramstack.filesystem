# Ramstack.FileSystem

A .NET library providing a virtual file system abstraction.

**For current implementations, see [Related Projects](#related-projects)**

## Overview

The primary interface is `IVirtualFileSystem`, which exposes methods to:
- Access files (`VirtualFile`).
- Access directories (`VirtualDirectory`).

### VirtualFile

The `VirtualFile` class provides properties and methods for creating, deleting, copying and opening files within the virtual file system.

```csharp
using Ramstack.FileSystem;

public class Program
{
    public static async Task Sample(IVirtualFileSystem fs)
    {
        var file = fs.GetFile("/sample/hello.txt");

        if (await file.ExistsAsync())
        {
            Console.WriteLine($"Deleting file '{file.FullName}'");
            await file.DeleteAsync();
        }

        Console.WriteLine($"Writing 'Hello, World!' to '{file.FullName}'");

        await using (Stream stream = await file.OpenWriteAsync())
        await using (StreamWriter writer = new StreamWriter(stream))
        {
            await writer.WriteLineAsync("Hello, World!");
        }

        Console.WriteLine($"Reading contents of '{file.FullName}'");
        using (StreamReader reader = await file.OpenTextAsync())
        {
            Console.WriteLine(await reader.ReadToEndAsync());
        }

        // Replace content of the file with data from an existing file
        await using FileStream input = File.OpenRead(@"D:\sample\hello-world.txt");
        await file.WriteAsync(input, overwrite: true);

        Console.WriteLine($"The file '{file.Name}' has a length of {await file.GetLengthAsync()} bytes");
    }
}
```

The `VirtualFile` class provides the following properties to retrieve information about a file:
- `FileSystem`: Gets the `IVirtualFileSystem` associated with the file.
- `Directory`: Gets the `VirtualDirectory` representing the parent directory.
- `DirectoryName`: Gets the full path of the parent directory.
- `IsReadOnly`: Indicates whether the file is read-only.
- `Name`: Gets the name of the file.
- `Extension`: Gets the extension of the file.

The `VirtualFile` class also exposes a `GetPropertiesAsync` method to retrieve file properties:
- `Length`: Gets the size of the file in bytes.
- `Exists`: Indicates whether the file exists.
- `CreationTime`: Gets the creation time of the file.
- `LastAccessTime`: Gets the last access time of the file.
- `LastWriteTime`: Gets the last modification time of the file.

For convenience, many methods specific to `VirtualFile` are also available as extension methods on `IVirtualFileSystem`.

For example, if we know the path of the file we want to read, we don't need to obtain a `VirtualFile` object explicitly:
```csharp
using StreamReader reader = await fs.OpenTextAsync("/docs/README", Encoding.UTF8);
Console.WriteLine(await reader.ReadToEndAsync());
```

### VirtualDirectory

The `VirtualDirectory` class provides properties and methods for creating, deleting, and enumerating directories and subdirectories.

```csharp
public static async Task PrintFilesAsync(VirtualDirectory directory, string padding = "", CancellationToken cancellationToken = default)
{
    await foreach (VirtualNode node in directory.GetFileNodesAsync(cancellationToken))
    {
        Console.WriteLine($"{padding}{node.Name}");

        if (node is VirtualDirectory dir)
        {
            await PrintFilesAsync(dir, padding + "   ", cancellationToken);
        }
    }
}

// Print the file tree
await PrintFilesAsync(fs.GetDirectory("/sample"));
```

The `VirtualDirectory` class provides the following properties to retrieve information about a directory:
- `FileSystem`: Gets the `IVirtualFileSystem` associated with the directory.
- `IsRoot`: Indicates whether the directory is the root directory.
- `Parent`: Gets the `VirtualDirectory` representing the parent directory.
- `DirectoryName`: Gets the full path of the parent directory.
- `IsReadOnly`: Indicates whether the directory is read-only.
- `Name`: Gets the name of the directory.

The `VirtualDirectory` class also exposes a `GetPropertiesAsync` method to retrieve directory properties, if available:
- `Exists`: Indicates whether the directory exists.
- `CreationTime`: Gets the creation time of the directory.
- `LastAccessTime`: Gets the last access time of the directory.
- `LastWriteTime`: Gets the last modification time of the directory.
- `Length`: Always returns `0` for directories.

For convenience, many methods specific to `VirtualDirectory` are also available as extension methods on `IVirtualFileSystem`.

For example, if we know the path of the desired directory, we don't need to obtain a `VirtualDirectory` object explicitly:
```csharp
await foreach (VirtualFile file in fs.GetFilesAsync("/sample/directory"))
{
    Console.WriteLine($"Processing: {file.FullName}");
}

// Delete the directory recursively
await fs.DeleteDirectoryAsync("/sample/directory");
```

## Remark

The file system in use may be read-only, and as a result, any modifying operations on files and directories will throw an exception.
To check if the file system is read-only, the `IVirtualFileSystem`, `VirtualFile` and `VirtualDirectory` classes provide the `IsReadOnly` property.

```csharp
if (!fs.IsReadOnly)
{
    await fs.DeleteFileAsync("/hello.txt");
}
```

## Related Projects
- [Ramstack.FileSystem.Abstractions](https://www.nuget.org/packages/Ramstack.FileSystem.Abstractions) - Provides a virtual file system abstraction.
- [Ramstack.FileSystem.Physical](https://www.nuget.org/packages/Ramstack.FileSystem.Physical) - Provides an implementation based on the local file system.
- [Ramstack.FileSystem.Azure](https://www.nuget.org/packages/Ramstack.FileSystem.Azure) - Provides an implementation based on Azure Blob Storage.
- [Ramstack.FileSystem.Zip](https://www.nuget.org/packages/Ramstack.FileSystem.Zip) - Provides an implementation based on ZIP archives.
- [Ramstack.FileSystem.Readonly](https://www.nuget.org/packages/Ramstack.FileSystem.Readonly) - Provides a read-only wrapper for the underlying file system.
- [Ramstack.FileSystem.Globbing](https://www.nuget.org/packages/Ramstack.FileSystem.Globbing) - Wraps the file system, filtering files and directories using glob patterns.
- [Ramstack.FileSystem.Prefixed](https://www.nuget.org/packages/Ramstack.FileSystem.Prefixed) - Adds a prefix to file paths within the underlying file system.
- [Ramstack.FileSystem.Sub](https://www.nuget.org/packages/Ramstack.FileSystem.Sub) - Wraps the underlying file system, restricting access to a specific subpath.
- [Ramstack.FileSystem.Adapters](https://www.nuget.org/packages/Ramstack.FileSystem.Adapters) - Provides integration with `Microsoft.Extensions.FileProviders`.

## Supported Versions

|      | Version |
|------|---------|
| .NET | 6, 7, 8 |

## Contributions

Bug reports and contributions are welcome.

## License

This package is released as open source under the **MIT License**.
See the [LICENSE](https://github.com/rameel/ramstack.virtualfiles/blob/main/LICENSE) file for more details.

