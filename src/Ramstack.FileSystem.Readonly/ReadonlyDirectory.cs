using System.Runtime.CompilerServices;

using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Readonly;

/// <summary>
/// Represents a read-only wrapper around an existing <see cref="VirtualDirectory"/> instance.
/// </summary>
internal sealed class ReadonlyDirectory : VirtualDirectory
{
    private readonly ReadonlyFileSystem _fs;
    private readonly VirtualDirectory _directory;

    /// <inheritdoc/>
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadonlyDirectory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="directory">The <see cref="VirtualDirectory"/> instance to wrap.</param>
    public ReadonlyDirectory(ReadonlyFileSystem fileSystem, VirtualDirectory directory) : base(directory.FullName) =>
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
    protected override async IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var node in _directory.GetFileNodesAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return node switch
            {
                VirtualDirectory directory => new ReadonlyDirectory(_fs, directory),
                _ => new ReadonlyFile(_fs, (VirtualFile)node)
            };
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var node in _directory.GetFilesAsync(cancellationToken).ConfigureAwait(false))
            yield return new ReadonlyFile(_fs, node);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var node in _directory.GetDirectoriesAsync(cancellationToken).ConfigureAwait(false))
            yield return new ReadonlyDirectory(_fs, node);
    }
}
