using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Globbing;

/// <summary>
/// Represents a file with globbing support for matching file patterns.
/// </summary>
internal sealed class GlobbingFile : VirtualFile
{
    private readonly GlobbingFileSystem _fs;
    private readonly VirtualFile _file;
    private readonly bool _included;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobbingFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="file">The <see cref="VirtualFile"/> instance to wrap.</param>
    /// <param name="included">A boolean value indicating whether the file matches the specified globbing patterns.</param>
    public GlobbingFile(GlobbingFileSystem fileSystem, VirtualFile file, bool included) : base(file.FullName) =>
        (_fs, _file, _included) = (fileSystem, file, included);

    /// <inheritdoc />
    protected override void RefreshCore() =>
        _file.Refresh();

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        if (_included)
            return _file.GetPropertiesAsync(cancellationToken)!;

        return default;
    }

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken)
    {
        if (_included)
            return _file.ExistsAsync(cancellationToken);

        return default;
    }

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken)
    {
        if (!_included)
            ThrowHelper.FileNotFound(FullName);

        return _file.OpenReadAsync(cancellationToken);
    }

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
