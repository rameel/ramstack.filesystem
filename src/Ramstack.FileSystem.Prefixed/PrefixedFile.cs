namespace Ramstack.FileSystem.Prefixed;

/// <summary>
/// Represents a file in a <see cref="PrefixedFileSystem"/>,
/// wrapping an existing <see cref="VirtualFile"/> instance with a prefixed path.
/// </summary>
internal sealed class PrefixedFile : VirtualFile
{
    private readonly PrefixedFileSystem _fileSystem;
    private readonly VirtualFile _file;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefixedFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The prefixed path of the file.</param>
    /// <param name="file">The underlying <see cref="VirtualFile"/> that this instance wraps.</param>
    public PrefixedFile(PrefixedFileSystem fileSystem, string path, VirtualFile file) : base(path) =>
        (_fileSystem, _file) = (fileSystem, file);

    /// <inheritdoc />
    protected override void RefreshCore() =>
        _file.Refresh();

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken) =>
        _file.GetPropertiesAsync(refresh: true, cancellationToken)!;

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken) =>
        _file.ExistsAsync(cancellationToken);

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken) =>
        _file.OpenReadAsync(cancellationToken);

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenWriteCoreAsync(CancellationToken cancellationToken) =>
        _file.OpenWriteAsync(cancellationToken);

    /// <inheritdoc />
    protected override ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken) =>
        _file.WriteAsync(stream, overwrite, cancellationToken);

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken) =>
        _file.DeleteAsync(cancellationToken);
}
