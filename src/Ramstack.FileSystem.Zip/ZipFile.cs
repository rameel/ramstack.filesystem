using System.IO.Compression;

using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Zip;

/// <summary>
/// Represents a file within a ZIP archive.
/// </summary>
internal sealed class ZipFile : VirtualFile
{
    private readonly ZipFileSystem _fileSystem;
    private readonly ZipArchiveEntry _entry;

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZipFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="entry">The ZIP archive entry representing the file.</param>
    public ZipFile(ZipFileSystem fileSystem, string path, ZipArchiveEntry entry) : base(path) =>
        (_fileSystem, _entry) = (fileSystem, entry);

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        var properties = VirtualNodeProperties.File(default, default, _entry.LastWriteTime, _entry.Length);
        return new ValueTask<VirtualNodeProperties?>(properties);
    }

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken) =>
        new ValueTask<bool>(true);

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken) =>
        new ValueTask<Stream>(_entry.Open());

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenWriteCoreAsync(CancellationToken cancellationToken)
    {
        ThrowHelper.ChangesNotSupported();
        return default;
    }

    /// <inheritdoc />
    protected override ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken)
    {
        ThrowHelper.ChangesNotSupported();
        return default;
    }

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken)
    {
        ThrowHelper.ChangesNotSupported();
        return default;
    }
}
