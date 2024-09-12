using Ramstack.FileSystem.Null;

namespace Ramstack.FileSystem.Composite;

/// <summary>
/// Represents a <see cref="VirtualFile"/> implementation for the <see cref="CompositeFileSystem"/> class.
/// </summary>
internal sealed class CompositeFile : VirtualFile
{
    private readonly CompositeFileSystem _fs;
    private VirtualFile? _file;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="file">The <see cref="VirtualFile"/> to wrap.</param>
    public CompositeFile(CompositeFileSystem fileSystem, string path, VirtualFile? file = null) : base(path) =>
        (_fs, _file) = (fileSystem, file);

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        return (_file?.GetPropertiesAsync(cancellationToken) ?? GetPropertiesImplAsync(cancellationToken))!;

        async ValueTask<VirtualNodeProperties> GetPropertiesImplAsync(CancellationToken token)
        {
            var file = await FindFileAsync(token).ConfigureAwait(false);
            return await file.GetPropertiesAsync(token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken)
    {
        return _file?.OpenReadAsync(cancellationToken) ?? OpenStreamImplAsync(cancellationToken);

        async ValueTask<Stream> OpenStreamImplAsync(CancellationToken token)
        {
            var file = await FindFileAsync(token).ConfigureAwait(false);
            return await file.OpenReadAsync(token).ConfigureAwait(false);
        }
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

    /// <inheritdoc />
    protected override void RefreshCore() =>
        _file = null;

    private async ValueTask<VirtualFile> FindFileAsync(CancellationToken cancellationToken)
    {
        if (_file is null)
        {
            foreach (var fs in _fs.InternalFileSystems)
            {
                var file = fs.GetFile(FullName);
                if (file is NotFoundFile)
                    continue;

                if (await file.ExistsAsync(cancellationToken).ConfigureAwait(false))
                    return _file = file;
            }
        }

        return _file ??= new NotFoundFile(FileSystem, FullName);
    }
}
