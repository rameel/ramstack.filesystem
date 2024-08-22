using Microsoft.Extensions.FileProviders;

using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Adapters;

/// <summary>
/// Represents an adapter for the <see cref="IFileInfo"/> instance.
/// </summary>
internal sealed class VirtualDirectoryAdapter : VirtualDirectory
{
    private readonly VirtualFileSystemAdapter _fs;
    private IFileInfo? _file;
    private IDirectoryContents? _directory;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualDirectoryAdapter"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The path of the directory.</param>
    /// <param name="file">The <see cref="IFileInfo"/> associated with this directory.</param>
    public VirtualDirectoryAdapter(VirtualFileSystemAdapter fileSystem, string path, IFileInfo file) : base(path) =>
        // ReSharper disable once SuspiciousTypeConversion.Global
        (_fs, _file, _directory) = (fileSystem, file, file as IDirectoryContents);

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualDirectoryAdapter"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The path of the directory.</param>
    /// <param name="directory">The <see cref="IDirectoryContents"/> associated with this directory.</param>
    public VirtualDirectoryAdapter(VirtualFileSystemAdapter fileSystem, string path, IDirectoryContents directory) : base(path) =>
        (_fs, _directory) = (fileSystem, directory);

    /// <inheritdoc />
    protected override void RefreshCore()
    {
        _file = null;
        _directory = _fs.FileProvider.GetDirectoryContents(FullName);
    }

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        var properties = _file?.Exists ?? _directory!.Exists
            ? VirtualNodeProperties.CreateDirectoryProperties(default, default, default)
            : null;

        return new ValueTask<VirtualNodeProperties?>(properties);
    }

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken)
    {
        var exists = _file?.Exists ?? _directory!.Exists;
        return new ValueTask<bool>(exists);
    }

    /// <inheritdoc />
    protected override ValueTask CreateCoreAsync(CancellationToken cancellationToken)
    {
        ThrowHelper.ChangesNotSupported();
        return default;
    }

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken)
    {
        ThrowHelper.ChangesNotSupported();
        return default;
    }

    /// <inheritdoc />
    protected override IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync(CancellationToken cancellationToken)
    {
        return GetFileNodes().ToAsyncEnumerable();

        IEnumerable<VirtualNode> GetFileNodes()
        {
            foreach (var fi in _directory ??= _fs.FileProvider.GetDirectoryContents(FullName))
            {
                var path = VirtualPath.Join(FullName, fi.Name);
                yield return fi.IsDirectory
                    ? new VirtualDirectoryAdapter(_fs, path, fi)
                    : new VirtualFileAdapter(_fs, path, fi);
            }
        }
    }

    /// <inheritdoc />
    protected override IAsyncEnumerable<VirtualFile> GetFilesCoreAsync(CancellationToken cancellationToken)
    {
        return GetFiles().ToAsyncEnumerable();

        IEnumerable<VirtualFile> GetFiles()
        {
            foreach (var fi in _directory ??= _fs.FileProvider.GetDirectoryContents(FullName))
            {
                var path = VirtualPath.Join(FullName, fi.Name);
                if (!fi.IsDirectory)
                    yield return new VirtualFileAdapter(_fs, path, fi);
            }
        }
    }

    /// <inheritdoc />
    protected override IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync(CancellationToken cancellationToken)
    {
        return GetDirectories().ToAsyncEnumerable();

        IEnumerable<VirtualDirectory> GetDirectories()
        {
            foreach (var fi in _directory ??= _fs.FileProvider.GetDirectoryContents(FullName))
            {
                var path = VirtualPath.Join(FullName, fi.Name);
                if (fi.IsDirectory)
                    yield return new VirtualDirectoryAdapter(_fs, path, fi);
            }
        }
    }
}
