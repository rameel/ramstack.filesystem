using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Ramstack.FileSystem.Sub;

/// <summary>
/// Represents a file system that manages a subset of files
/// located under a specified path in an underlying <see cref="IVirtualFileSystem"/>.
/// </summary>
/// <remarks>
/// This class provides functionality to handle files and directories that are located under
/// a specific path within the root directory of the underlying file system.
/// </remarks>
[DebuggerDisplay("{_path,nq}")]
public sealed class SubFileSystem : IVirtualFileSystem
{
    private readonly IVirtualFileSystem _fs;
    private readonly string _path;

    /// <inheritdoc />
    public bool IsReadOnly => _fs.IsReadOnly;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubFileSystem"/> class.
    /// </summary>
    /// <param name="path">The path under the root directory of the <paramref name="fileSystem"/>.</param>
    /// <param name="fileSystem">The underlying file system.</param>
    public SubFileSystem(string path, IVirtualFileSystem fileSystem) =>
        (_path, _fs) = (VirtualPath.GetFullPath(path), fileSystem);

    /// <inheritdoc />
    public VirtualFile GetFile(string path)
    {
        path = VirtualPath.GetFullPath(path);
        var file = _fs.GetFile(GetFullPath(path));

        return new SubFile(this, path, file);
    }

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path)
    {
        path = VirtualPath.GetFullPath(path);
        var directory = _fs.GetDirectory(GetFullPath(path));

        return new SubDirectory(this, path, directory);
    }

    /// <inheritdoc />
    public void Dispose() =>
        _fs.Dispose();

    /// <summary>
    /// Returns the full path to the underlying file system.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns>
    /// The full path to underlying file system.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="path"/> navigates above the root directory.
    /// </exception>
    private string GetFullPath(string path)
    {
        if (path.Length == 0 || path == "/")
            return _path;

        if (VirtualPath.IsNavigatesAboveRoot(path))
            Error_InvalidPath();

        return VirtualPath.Join(_path, path);
    }

    [DoesNotReturn]
    private static void Error_InvalidPath() =>
        throw new ArgumentException("Invalid path");
}
