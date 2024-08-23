namespace Ramstack.FileSystem.Composite;

/// <summary>
/// Represents a <see cref="VirtualFile"/> implementation for the <see cref="CompositeFileSystem"/> class.
/// </summary>
internal sealed class CompositeFile : VirtualFile
{
    private readonly CompositeFileSystem _fs;
    private readonly VirtualFile _file;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="file">The <see cref="VirtualFile"/> to wrap.</param>
    public CompositeFile(CompositeFileSystem fileSystem, VirtualFile file) : base(file.FullName) =>
        (_fs, _file) = (fileSystem, file);

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken) =>
        _file.GetPropertiesAsync(cancellationToken)!;

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
