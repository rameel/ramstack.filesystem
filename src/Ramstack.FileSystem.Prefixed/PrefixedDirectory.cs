using System.Runtime.CompilerServices;

using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Prefixed;

/// <summary>
/// Represents a directory in a <see cref="PrefixedFileSystem"/>,
/// wrapping an existing <see cref="VirtualDirectory"/> instance with a prefixed path.
/// </summary>
internal class PrefixedDirectory : VirtualDirectory
{
    private readonly PrefixedFileSystem _fs;
    private readonly VirtualDirectory _directory;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefixedDirectory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The prefixed path of the directory.</param>
    /// <param name="directory">The underlying <see cref="VirtualDirectory"/> that this instance wraps.</param>
    public PrefixedDirectory(PrefixedFileSystem fileSystem, string path, VirtualDirectory directory) : base(path) =>
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
        await foreach (var node in _directory.GetFileNodesAsync(cancellationToken))
        {
            var path = VirtualPath.Join(FullName, node.Name);
            yield return node switch
            {
                VirtualDirectory directory => new PrefixedDirectory(_fs, path, directory),
                _ => new PrefixedFile(_fs, path, (VirtualFile)node)
            };
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var node in _directory.GetFilesAsync(cancellationToken))
        {
            var path = VirtualPath.Join(FullName, node.Name);
            yield return new PrefixedFile(_fs, path, node);
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var node in _directory.GetDirectoriesAsync(cancellationToken))
        {
            var path = VirtualPath.Join(FullName, node.Name);
            yield return new PrefixedDirectory(_fs, path, node);
        }
    }
}
