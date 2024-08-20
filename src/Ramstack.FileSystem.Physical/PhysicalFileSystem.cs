using System.Diagnostics;

using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Physical;

/// <summary>
/// Represents an implementation of <see cref="IVirtualFileSystem"/> for physical files.
/// </summary>
[DebuggerDisplay("Root = {_path,nq}")]
public sealed class PhysicalFileSystem : IVirtualFileSystem
{
    /// <summary>
    /// The physical path that is considered the root path of this file system.
    /// </summary>
    private readonly string _path;

    /// <summary>
    /// Gets or sets a value indicating whether the file system is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalFileSystem"/> class with the specified physical root path.
    /// </summary>
    /// <param name="path">The physical path that is considered the root path of the creating file system.</param>
    public PhysicalFileSystem(string path)
    {
        if (!Path.IsPathRooted(path))
            Error(path);

        _path = Path.GetFullPath(path);

        static void Error(string path) =>
            throw new ArgumentException($"The path '{path}' must be absolute.");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalFileSystem"/> class with the specified physical root path.
    /// </summary>
    /// <param name="path">The physical path that is considered the root path of the creating file system.</param>
    /// <param name="isReadOnly">A value indicating whether the file system is read-only.</param>
    public PhysicalFileSystem(string path, bool isReadOnly) : this(path) =>
        IsReadOnly = isReadOnly;

    /// <inheritdoc />
    public VirtualFile GetFile(string path)
    {
        path = VirtualPath.GetFullPath(path);
        return new PhysicalFile(this, path);
    }

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path)
    {
        path = VirtualPath.GetFullPath(path);
        return new PhysicalDirectory(this, path);
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
    internal string GetPhysicalPath(string path)
    {
        Debug.Assert(path == VirtualPath.GetFullPath(path));
        return Path.Join(_path, path);
    }
}
