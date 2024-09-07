using Ramstack.FileSystem.Utilities;

namespace Ramstack.FileSystem.Null;

/// <summary>
/// Represents a non-existing directory.
/// </summary>
public class NotFoundDirectory : VirtualDirectory
{
    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundDirectory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The path of the directory.</param>
    public NotFoundDirectory(IVirtualFileSystem fileSystem, string path) : base(VirtualPath.GetFullPath(path)) =>
        FileSystem = fileSystem;

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override ValueTask CreateCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync(CancellationToken cancellationToken) =>
        Array.Empty<VirtualNode>().ToAsyncEnumerable();
}
