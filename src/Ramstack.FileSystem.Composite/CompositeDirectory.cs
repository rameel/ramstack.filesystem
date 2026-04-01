using System.Runtime.CompilerServices;

using Ramstack.FileSystem.Null;

namespace Ramstack.FileSystem.Composite;

/// <summary>
/// Represents a <see cref="VirtualDirectory"/> implementation for the <see cref="CompositeFileSystem"/> class.
/// </summary>
internal sealed class CompositeDirectory : VirtualDirectory
{
    private readonly CompositeFileSystem _fs;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeDirectory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The path of the directory.</param>
    public CompositeDirectory(CompositeFileSystem fileSystem, string path) : base(path) =>
        _fs = fileSystem;

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken) =>
        new ValueTask<VirtualNodeProperties?>(VirtualNodeProperties.None);

    /// <inheritdoc />
    protected override ValueTask CreateCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var set = new HashSet<string>();

        foreach (var fs in _fs.InternalFileSystems)
        {
            await foreach (var node in fs.GetFileNodesAsync(FullName, cancellationToken).ConfigureAwait(false))
            {
                if (node is NotFoundFile or NotFoundDirectory)
                    continue;

                if (set.Add(node.FullName))
                    yield return node is VirtualFile file
                        ? new CompositeFile(_fs, node.FullName, file)
                        : new CompositeDirectory(_fs, node.FullName);
            }
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var set = new HashSet<string>();

        foreach (var fs in _fs.InternalFileSystems)
        {
            await foreach (var file in fs.GetFilesAsync(FullName, cancellationToken).ConfigureAwait(false))
            {
                if (file is NotFoundFile)
                    continue;

                if (set.Add(file.FullName))
                    yield return new CompositeFile(_fs, file.FullName, file);
            }
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var set = new HashSet<string>();

        foreach (var fs in _fs.InternalFileSystems)
        {
            await foreach (var directory in fs.GetDirectoriesAsync(FullName, cancellationToken).ConfigureAwait(false))
            {
                if (directory is NotFoundDirectory)
                    continue;

                if (set.Add(directory.FullName))
                    yield return new CompositeDirectory(_fs, directory.FullName);
            }
        }
    }
}
