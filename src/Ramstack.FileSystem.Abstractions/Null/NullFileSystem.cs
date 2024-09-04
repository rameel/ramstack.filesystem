namespace Ramstack.FileSystem.Null;

/// <summary>
/// Represents a virtual file system that does not provide any actual file or directory functionality.
/// All operations on this file system result in a <see cref="NotFoundFile"/> or <see cref="NotFoundDirectory"/>.
/// </summary>
public sealed class NullFileSystem : IVirtualFileSystem
{
    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <inheritdoc />
    public VirtualFile GetFile(string path) =>
        new NotFoundFile(this, path);

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path) =>
        new NotFoundDirectory(this, path);

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
    }
}
