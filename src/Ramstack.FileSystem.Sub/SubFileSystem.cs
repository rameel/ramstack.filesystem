using System.Diagnostics;

namespace Ramstack.FileSystem.Sub;

/// <summary>
/// Represents a file system that manages a subset of files
/// located under a specified path in an underlying <see cref="IVirtualFileSystem"/>.
/// </summary>
/// <remarks>
/// This class provides functionality to handle files and directories that are located under
/// a specific path within the root directory of the underlying file system.
/// </remarks>
[DebuggerDisplay("{_root,nq}")]
public sealed class SubFileSystem : IVirtualFileSystem
{
    private readonly IVirtualFileSystem _fs;
    private readonly string _root;

    /// <inheritdoc />
    public bool IsReadOnly => _fs.IsReadOnly;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubFileSystem"/> class.
    /// </summary>
    /// <param name="path">The path under the root directory of the <paramref name="fileSystem"/>.</param>
    /// <param name="fileSystem">The underlying file system.</param>
    public SubFileSystem(string path, IVirtualFileSystem fileSystem) =>
        (_root, _fs) = (VirtualPath.Normalize(path), fileSystem);

    /// <inheritdoc />
    public VirtualFile GetFile(string path)
    {
        path = VirtualPath.Normalize(path);

        var underlyingPath = ConvertToUnderlyingPath(path);
        var file = _fs.GetFile(underlyingPath);

        return new SubFile(this, path, file);
    }

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path)
    {
        path = VirtualPath.Normalize(path);

        var underlyingPath = ConvertToUnderlyingPath(path);
        var directory = _fs.GetDirectory(underlyingPath);

        return new SubDirectory(this, path, directory);
    }

    /// <inheritdoc />
    public void Dispose() =>
        _fs.Dispose();

    /// <summary>
    /// Converts a path from the underlying file system to a path relative to this file system's root.
    /// </summary>
    /// <param name="underlyingPath">The absolute path within the parent file system.
    /// Must be normalized and start with this file system's root path.</param>
    /// <returns>
    /// The corresponding path within this file system.
    /// </returns>
    /// <remarks>
    /// For example, converts "/app/assets/images/logo.png" to "/images/logo.png",
    /// where "/app/assets" is this file system's root.
    /// </remarks>
    internal string ConvertToSubPath(string underlyingPath)
    {
        Debug.Assert(VirtualPath.IsNormalized(underlyingPath));
        Debug.Assert(underlyingPath.StartsWith(_root, StringComparison.Ordinal));

        if (underlyingPath == _root)
            return "/";

        return new string(underlyingPath.AsSpan(_root.Length));
    }

    /// <summary>
    /// Converts a path from this file system to the corresponding absolute path in the underlying file system.
    /// </summary>
    /// <param name="path">The path within this file system.
    /// Must be normalized (e.g., "/" or "/images/logo.png").</param>
    /// <returns>
    /// The absolute path in the underlying file system that corresponds to this file system path.
    /// </returns>
    /// <remarks>
    /// For example, converts "/images/logo.png" to "/app/assets/images/logo.png",
    /// where "/app/assets" is this file system's root.
    /// </remarks>
    private string ConvertToUnderlyingPath(string path)
    {
        Debug.Assert(VirtualPath.IsNormalized(path));

        if (path == "/")
            return _root;

        return VirtualPath.Join(_root, path);
    }
}
