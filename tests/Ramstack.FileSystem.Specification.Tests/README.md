# Ramstack.FileSystem.Specification.Tests

Provides a suite of `NUnit` tests to validate specifications for `Ramstack.FileSystem`.

## Getting Started

To install the `Ramstack.FileSystem.Specification.Tests` [NuGet package](https://www.nuget.org/packages/Ramstack.FileSystem.Specification.Tests)
in your project, run the following command:
```console
dotnet add package Ramstack.FileSystem.Specification.Tests
```

## Usage

```csharp
using Ramstack.FileSystem.Specification.Tests;
using Ramstack.FileSystem.Specification.Tests.Utilities;

namespace Ramstack.FileSystem.Physical;

[TestFixture]
public class PhysicalFileSystemSpecificationTests : VirtualFileSystemSpecificationTests
{
    // Test content for the file system structure
    private readonly TempFileStorage _storage = new();

    [OneTimeTearDown]
    public void Cleanup()
    {
        // Remove temporary files
        _storage.Dispose();
    }

    // Create a file system
    protected override IVirtualFileSystem GetFileSystem() =>
        new PhysicalFileSystem(_storage.Root);

    // Return DirectoryInfo to check file system structure
    protected override DirectoryInfo GetDirectoryInfo() =>
        new DirectoryInfo(_storage.Root);
}
```

## Supported Versions

|      | Version |
|------|---------|
| .NET | 6, 7, 8 |

## Contributions

Bug reports and contributions are welcome.

## License

This package is released as open source under the **MIT License**.
See the [LICENSE](https://github.com/rameel/ramstack.virtualfiles/blob/main/LICENSE) file for more details.
