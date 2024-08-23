using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Composite;

/// <summary>
/// Represents an implementation of <see cref="IVirtualFileSystem"/> that combines multiple file systems
/// into a single composite file system.
/// </summary>
public sealed class CompositeFileSystem : IVirtualFileSystem
{
    /// <summary>
    /// An array of the underlying file systems.
    /// </summary>
    internal readonly IVirtualFileSystem[] InternalFileSystems;

    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <summary>
    /// Gets a read-only list of the underlying file systems.
    /// </summary>
    public IReadOnlyList<IVirtualFileSystem> FileSystems => InternalFileSystems;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeFileSystem"/> class.
    /// </summary>
    /// <param name="fileSystems">An array of <see cref="IVirtualFileSystem"/> instances.</param>
    public CompositeFileSystem(params IVirtualFileSystem[] fileSystems)
    {
        ArgumentNullException.ThrowIfNull(fileSystems);
        InternalFileSystems = fileSystems;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeFileSystem"/> class.
    /// </summary>
    /// <param name="fileSystems">An array of <see cref="IVirtualFileSystem"/> instances.</param>
    public CompositeFileSystem(IEnumerable<IVirtualFileSystem> fileSystems) =>
        InternalFileSystems = fileSystems.ToArray();

    /// <inheritdoc />
    public VirtualFile GetFile(string path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path)
    {
        path = VirtualPath.GetFullPath(path);
        return new CompositeDirectory(this, path);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var fs in InternalFileSystems)
            fs.Dispose();
    }
}
