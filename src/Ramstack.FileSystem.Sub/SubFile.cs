namespace Ramstack.FileSystem.Sub;

/// <summary>
/// Represents a wrapper around an existing <see cref="VirtualFile"/> instance.
/// </summary>
internal sealed class SubFile : VirtualFile
{
    private readonly SubFileSystem _fileSystem;
    private readonly VirtualFile _file;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="file">The underlying <see cref="VirtualFile"/> instance to wrap.</param>
    public SubFile(SubFileSystem fileSystem, string path, VirtualFile file) : base(path) =>
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
