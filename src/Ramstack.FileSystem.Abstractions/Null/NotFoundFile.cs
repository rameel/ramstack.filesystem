using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Null;

/// <summary>
/// Represents a non-existing file.
/// </summary>
public sealed class NotFoundFile : VirtualFile
{
    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path of the file.</param>
    public NotFoundFile(IVirtualFileSystem fileSystem, string path) : base(VirtualPath.GetFullPath(path)) =>
        FileSystem = fileSystem;

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken)
    {
        ThrowHelper.FileNotFound(FullName);
        return default;
    }

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenWriteCoreAsync(CancellationToken cancellationToken)
    {
        if (IsReadOnly)
            ThrowHelper.ChangesNotSupported();

        ThrowHelper.FileNotFound(FullName);
        return default;
    }

    /// <inheritdoc />
    protected override ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken)
    {
        if (IsReadOnly)
            ThrowHelper.ChangesNotSupported();

        ThrowHelper.FileNotFound(FullName);
        return default;
    }

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken)
    {
        if (IsReadOnly)
            ThrowHelper.ChangesNotSupported();

        return default;
    }
}
