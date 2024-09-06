using Microsoft.Extensions.FileProviders;

namespace Ramstack.FileSystem.Adapters;

/// <summary>
/// Represents an adapter for the <see cref="IFileInfo"/> instance.
/// </summary>
internal sealed class VirtualFileAdapter : VirtualFile
{
    private readonly VirtualFileSystemAdapter _fs;
    private IFileInfo _file;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualFileAdapter"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="file">The <see cref="IFileInfo"/> associated with this file.</param>
    public VirtualFileAdapter(VirtualFileSystemAdapter fileSystem, string path, IFileInfo file) : base(path) =>
        (_fs, _file) = (fileSystem, file);

    /// <inheritdoc />
    protected override void RefreshCore() =>
        _file = _fs.FileProvider.GetFileInfo(FullName);

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        var properties = _file.Exists
            ? VirtualNodeProperties.CreateFileProperties(default, default, lastWriteTime: _file.LastModified, _file.Length)
            : null;

        return new ValueTask<VirtualNodeProperties?>(properties);
    }

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken) =>
        new ValueTask<bool>(_file.Exists);

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken)
    {
        var stream = _file.CreateReadStream();
        return new ValueTask<Stream>(stream);
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
