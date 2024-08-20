using System.Diagnostics;

using Ramstack.FileSystem.Internal;
using Ramstack.FileSystem.Null;

namespace Ramstack.FileSystem.Prefixed;

/// <summary>
/// Represents an implementation of the <see cref="IVirtualFileSystem"/> that adds a specified prefix
/// to the file paths within the underlying file system.
/// </summary>
[DebuggerDisplay("{_prefix,nq}")]
public sealed class PrefixedFileSystem : IVirtualFileSystem
{
    private readonly string _prefix;
    private readonly IVirtualFileSystem _fs;

    /// <inheritdoc />
    public bool IsReadOnly => _fs.IsReadOnly;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefixedFileSystem" /> class.
    /// </summary>
    /// <param name="prefix">The prefix to be applied to the file paths managed by this instance.</param>
    /// <param name="fileSystem">The underlying file system that manages the files to which the prefix will be applied.</param>
    public PrefixedFileSystem(string prefix, IVirtualFileSystem fileSystem) =>
        (_prefix, _fs) = (VirtualPath.GetFullPath(prefix), fileSystem);

    /// <inheritdoc />
    public VirtualFile GetFile(string path)
    {
        path = VirtualPath.GetFullPath(path);

        var underlying = TryGetPath(path, _prefix);
        if (underlying is not null)
            return new PrefixedFile(this, path, _fs.GetFile(underlying));

        return new NotFoundFile(this, path);
    }

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path)
    {
        path = VirtualPath.GetFullPath(path);

        var underlying = TryGetPath(path, _prefix);
        if (underlying is not null)
            return new PrefixedDirectory(this, path, _fs.GetDirectory(underlying));

        return new NotFoundDirectory(this, path);
    }

    /// <inheritdoc />
    public void Dispose() =>
        _fs.Dispose();

    /// <summary>
    /// Attempts to match a given path against the prefix. If successful, returns the remainder of the path relative to the prefix.
    /// </summary>
    /// <param name="path">The full path to match against the prefix.</param>
    /// <param name="prefix">The prefix to compare against the path.</param>
    /// <returns>
    /// The relative path if the prefix matches; otherwise, null.
    /// </returns>
    private static string? TryGetPath(string path, string prefix)
    {
        Debug.Assert(path == VirtualPath.GetFullPath(path));

        if (path == prefix)
            return "/";

        // TODO: Consider adding support for different file casing options.
        // FileSystemCasing? FilePathCasing?

        if (path.StartsWith(prefix, StringComparison.Ordinal) && path[prefix.Length] == '/')
            return path[prefix.Length..];

        return null;
    }
}
