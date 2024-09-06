using System.Runtime.CompilerServices;

namespace Ramstack.FileSystem.Globbing;

/// <summary>
/// Represents a directory with globbing support for matching file patterns.
/// </summary>
internal sealed class GlobbingDirectory : VirtualDirectory
{
    private readonly GlobbingFileSystem _fs;
    private readonly VirtualDirectory _directory;
    private readonly bool _included;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobbingDirectory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="directory">The <see cref="VirtualDirectory"/> instance to wrap.</param>
    /// <param name="included">A boolean value indicating whether the directory matches the specified globbing patterns.</param>
    public GlobbingDirectory(GlobbingFileSystem fileSystem, VirtualDirectory directory, bool included) : base(directory.FullName) =>
        (_fs, _directory, _included) = (fileSystem, directory, included);

    /// <inheritdoc />
    protected override void RefreshCore() =>
        _directory.Refresh();

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        if (_included)
            return _directory.GetPropertiesAsync(cancellationToken)!;

        return default;
    }

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken)
    {
        if (_included)
            return _directory.ExistsAsync(cancellationToken);

        return default;
    }

    /// <inheritdoc />
    protected override ValueTask CreateCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var node in _directory.GetFileNodesAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!_fs.IsExcluded(node.FullName))
            {
                if (node is VirtualFile file)
                {
                    if (_fs.IsIncluded(node.FullName))
                        yield return new GlobbingFile(_fs, file, true);
                }
                else
                {
                    yield return new GlobbingDirectory(_fs, (VirtualDirectory)node, true);
                }
            }
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var file in _directory.GetFilesAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_fs.IsFileIncluded(file.FullName))
                yield return new GlobbingFile(_fs, file, true);
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var directory in _directory.GetDirectoriesAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_fs.IsDirectoryIncluded(directory.FullName))
                yield return new GlobbingDirectory(_fs, directory, true);
        }
    }
}
