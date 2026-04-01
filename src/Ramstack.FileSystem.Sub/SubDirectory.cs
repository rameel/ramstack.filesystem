using System.Runtime.CompilerServices;

namespace Ramstack.FileSystem.Sub;

/// <summary>
/// Represents a wrapper around an existing <see cref="VirtualDirectory"/> instance.
/// </summary>
internal sealed class SubDirectory : VirtualDirectory
{
    private readonly SubFileSystem _fs;
    private readonly VirtualDirectory _directory;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubDirectory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The path of the directory.</param>
    /// <param name="directory">The underlying <see cref="VirtualDirectory"/> instance to wrap.</param>
    public SubDirectory(SubFileSystem fileSystem, string path, VirtualDirectory directory) : base(path) =>
        (_fs, _directory) = (fileSystem, directory);

    /// <inheritdoc />
    protected override void RefreshCore() =>
        _directory.Refresh();

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken) =>
        _directory.GetPropertiesAsync(cancellationToken)!;

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken) =>
        _directory.ExistsAsync(cancellationToken);

    /// <inheritdoc />
    protected override ValueTask CreateCoreAsync(CancellationToken cancellationToken) =>
        _directory.CreateAsync(cancellationToken);

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken) =>
        _directory.DeleteAsync(cancellationToken);

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var node in _directory.GetFileNodesAsync(cancellationToken).ConfigureAwait(false))
        {
            var path = VirtualPath.Join(FullName, node.Name);

            yield return node switch
            {
                VirtualDirectory directory => new SubDirectory(_fs, path, directory),
                _ => new SubFile(_fs, path, (VirtualFile)node)
            };
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var file in _directory.GetFilesAsync(cancellationToken).ConfigureAwait(false))
        {
            var path = VirtualPath.Join(FullName, file.Name);
            yield return new SubFile(_fs, path, file);
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var directory in _directory.GetDirectoriesAsync(cancellationToken).ConfigureAwait(false))
        {
            var path = VirtualPath.Join(FullName, directory.Name);
            yield return new SubDirectory(_fs, path, directory);
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync(string[] patterns, string[]? excludes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var node in _directory.GetFileNodesAsync(patterns, excludes, cancellationToken).ConfigureAwait(false))
        {
            var path = _fs.ConvertToSubPath(node.FullName);

            yield return node switch
            {
                VirtualDirectory directory => new SubDirectory(_fs, path, directory),
                _ => new SubFile(_fs, path, (VirtualFile)node)
            };
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync(string[] patterns, string[]? excludes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var file in _directory.GetFilesAsync(patterns, excludes, cancellationToken).ConfigureAwait(false))
        {
            var path = _fs.ConvertToSubPath(file.FullName);
            yield return new SubFile(_fs, path, file);
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync(string[] patterns, string[]? excludes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var directory in _directory.GetDirectoriesAsync(patterns, excludes, cancellationToken).ConfigureAwait(false))
        {
            var path = _fs.ConvertToSubPath(directory.FullName);
            yield return new SubDirectory(_fs, path, directory);
        }
    }
}
