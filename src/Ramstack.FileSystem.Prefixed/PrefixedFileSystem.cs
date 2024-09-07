using System.Diagnostics;

using Ramstack.FileSystem.Null;
using Ramstack.FileSystem.Utilities;

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
    private readonly VirtualDirectory[] _directories;

    /// <inheritdoc />
    public bool IsReadOnly => _fs.IsReadOnly;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefixedFileSystem" /> class.
    /// </summary>
    /// <param name="prefix">The prefix to be applied to the file paths managed by this instance.</param>
    /// <param name="fileSystem">The underlying file system that manages the files to which the prefix will be applied.</param>
    public PrefixedFileSystem(string prefix, IVirtualFileSystem fileSystem)
    {
        prefix = VirtualPath.GetFullPath(prefix);
        (_prefix, _fs) = (prefix, fileSystem);

        // Create artificial directory list
        _directories = CreateArtificialDirectories(this, prefix);

        static VirtualDirectory[] CreateArtificialDirectories(PrefixedFileSystem fs, string path)
        {
            var directories = new List<VirtualDirectory>();
            VirtualDirectory? directory = null;

            while (!string.IsNullOrEmpty(path))
            {
                directory = directory is null
                    ? new PrefixedDirectory(fs, path, fs._fs.GetDirectory("/"))
                    : new ArtificialDirectory(fs, path, directory);

                directories.Add(directory);
                path = VirtualPath.GetDirectoryName(path);
            }

            return directories.ToArray();
        }
    }

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

        foreach (var directory in _directories)
            if (directory.FullName == path)
                return directory;

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

    #region Inner type: ArtificialDirectory

    /// <summary>
    /// Represents an artificial directory within a file system.
    /// </summary>
    private sealed class ArtificialDirectory : VirtualDirectory
    {
        private readonly PrefixedFileSystem _fs;
        private readonly VirtualDirectory _directory;

        /// <inheritdoc />
        public override IVirtualFileSystem FileSystem => _fs;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArtificialDirectory"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system associated with this directory.</param>
        /// <param name="path">The path of the directory.</param>
        /// <param name="directory">The child directory.</param>
        public ArtificialDirectory(PrefixedFileSystem fileSystem, string path, VirtualDirectory directory) : base(path) =>
            (_fs, _directory) = (fileSystem, directory);

        /// <inheritdoc />
        protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
        {
            var properties = VirtualNodeProperties.CreateDirectoryProperties(default, default, default);
            return new ValueTask<VirtualNodeProperties?>(properties);
        }

        /// <inheritdoc />
        protected override ValueTask CreateCoreAsync(CancellationToken cancellationToken) =>
            default;

        /// <inheritdoc />
        protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken) =>
            _fs.GetDirectory(_fs._prefix).DeleteAsync(cancellationToken);

        /// <inheritdoc />
        protected override IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync(CancellationToken cancellationToken) =>
            new VirtualNode[] { _directory }.ToAsyncEnumerable();
    }

    #endregion
}
