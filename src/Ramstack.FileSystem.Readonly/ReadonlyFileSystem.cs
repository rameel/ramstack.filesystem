namespace Ramstack.FileSystem.Readonly;

/// <summary>
/// Represents a read-only wrapper around an existing <see cref="IVirtualFileSystem"/> instance.
/// This wrapper prevents modifications to the underlying file system.
/// </summary>
public sealed class ReadonlyFileSystem : IVirtualFileSystem
{
    private readonly IVirtualFileSystem _fs;

    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadonlyFileSystem"/> class.
    /// </summary>
    /// <param name="fileSystem">The underlying file system to wrap as read-only.</param>
    public ReadonlyFileSystem(IVirtualFileSystem fileSystem) =>
        _fs = fileSystem;

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path) =>
        new ReadonlyDirectory(this, _fs.GetDirectory(path));

    /// <inheritdoc />
    public VirtualFile GetFile(string path) =>
        new ReadonlyFile(this, _fs.GetFile(path));

    /// <inheritdoc />
    public void Dispose() =>
        _fs.Dispose();
}
