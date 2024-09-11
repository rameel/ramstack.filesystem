using System.Diagnostics;

namespace Ramstack.FileSystem.Physical;

/// <summary>
/// Represents an implementation of <see cref="IVirtualFileSystem"/> for physical files.
/// </summary>
[DebuggerDisplay("Root = {_root,nq}")]
public sealed class PhysicalFileSystem : IVirtualFileSystem
{
    private readonly string _root;

    /// <summary>
    /// Gets or sets a value indicating whether the file system is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalFileSystem"/> class.
    /// </summary>
    /// <param name="path">The physical path of the directory.</param>
    public PhysicalFileSystem(string path)
    {
        if (!Path.IsPathRooted(path))
            Error(path);

        _root = Path.GetFullPath(path);

        static void Error(string path) =>
            throw new ArgumentException($"The path '{path}' must be absolute.");
    }

    /// <inheritdoc />
    public VirtualFile GetFile(string path)
    {
        path = VirtualPath.Normalize(path);
        var physicalPath = GetPhysicalPath(path);

        return new PhysicalFile(this, path, physicalPath);
    }

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path)
    {
        path = VirtualPath.Normalize(path);
        var physicalPath = GetPhysicalPath(path);

        return new PhysicalDirectory(this, path, physicalPath);
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
    }

    /// <summary>
    /// Converts the specified virtual path within this file system
    /// to its corresponding physical path.
    /// </summary>
    /// <param name="path">The virtual path within this file system.</param>
    /// <returns>
    /// The corresponding physical path.
    /// </returns>
    private string GetPhysicalPath(string path)
    {
        Debug.Assert(path == VirtualPath.Normalize(path));
        return Path.Join(_root, path);
    }
}
