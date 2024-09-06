namespace Ramstack.FileSystem.Readonly;

/// <summary>
/// Represents a read-only wrapper around an existing <see cref="VirtualFile"/> instance.
/// </summary>
internal sealed class ReadonlyFile : VirtualFile
{
    private readonly ReadonlyFileSystem _fileSystem;
    private readonly VirtualFile _file;

    /// <inheritdoc/>
    public override IVirtualFileSystem FileSystem => _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadonlyFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="file">The <see cref="VirtualFile"/> instance to wrap.</param>
    public ReadonlyFile(ReadonlyFileSystem fileSystem, VirtualFile file) : base(file.FullName) =>
        (_fileSystem, _file) = (fileSystem, file);

    /// <inheritdoc />
    protected override void RefreshCore() =>
        _file.Refresh();

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken) =>
        _file.GetPropertiesAsync(true, cancellationToken)!;

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken) =>
        _file.ExistsAsync(cancellationToken);

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken) =>
        _file.OpenReadAsync(cancellationToken);

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenWriteCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken) =>
        default;
}
