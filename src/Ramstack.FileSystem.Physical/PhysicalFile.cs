namespace Ramstack.FileSystem.Physical;

/// <summary>
/// Represents an implementation of <see cref="VirtualFile"/> that maps to a physical file.
/// </summary>
internal sealed class PhysicalFile : VirtualFile
{
    /// <summary>
    /// The default size of the buffer used for file operations.
    /// </summary>
    private const int DefaultBufferSize = 4096;

    private readonly PhysicalFileSystem _fileSystem;
    private readonly string _physicalPath;

    /// <inheritdoc/>
    public override IVirtualFileSystem FileSystem => _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path of the file.</param>
    internal PhysicalFile(PhysicalFileSystem fileSystem, string path) : base(path) =>
        (_fileSystem, _physicalPath) = (fileSystem, fileSystem.GetPhysicalPath(path));

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        var info = new FileInfo(_physicalPath);
        var properties = info.Exists
            ? VirtualNodeProperties.CreateFileProperties(
                creationTime: info.CreationTimeUtc,
                lastAccessTime: info.LastAccessTime,
                lastWriteTime: info.LastWriteTimeUtc,
                length: info.Length)
            : null;

        return new ValueTask<VirtualNodeProperties?>(properties);
    }

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken)
    {
        const FileOptions options = FileOptions.Asynchronous | FileOptions.SequentialScan;
        var stream = new FileStream(_physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, options);

        return new ValueTask<Stream>(stream);
    }

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenWriteCoreAsync(CancellationToken cancellationToken)
    {
        EnsureDirectoryExists(Path.GetDirectoryName(_physicalPath)!);

        const FileOptions options = FileOptions.Asynchronous | FileOptions.SequentialScan;

        var stream = new FileStream(_physicalPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, DefaultBufferSize, options);

        // Since, FileMode.OpenOrCreate doesn't truncate the file, we manually
        // set the file length to zero to remove any leftover data.
        stream.SetLength(0);

        return new ValueTask<Stream>(stream);
    }

    /// <inheritdoc />
    protected override async ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken)
    {
        EnsureDirectoryExists(Path.GetDirectoryName(_physicalPath)!);

        const FileOptions options = FileOptions.Asynchronous | FileOptions.SequentialScan;

        // To overwrite the file, we use FileMode.OpenOrCreate instead of FileMode.Create.
        // This avoids a System.UnauthorizedAccessException: Access to the path is denied,
        // which can occur if the file has the FileAttributes.Hidden attribute.
        var fileMode = overwrite ? FileMode.OpenOrCreate : FileMode.CreateNew;

        await using var fs = new FileStream(_physicalPath, fileMode, FileAccess.Write, FileShare.None, DefaultBufferSize, options);

        // Since, FileMode.OpenOrCreate doesn't truncate the file, we manually
        // set the file length to zero to remove any leftover data.
        fs.SetLength(0);

        await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            File.Delete(_physicalPath);
        }
        catch (DirectoryNotFoundException)
        {
            // The path to the deleting file may not exist.
            // This is a safe scenario and does not require further handling.
        }

        return default;
    }

    /// <summary>
    /// Ensures that the specified directory exists on disk. If the directory does not exist, it will be created.
    /// </summary>
    /// <remarks>
    /// This method is used to prevent errors when attempting to write to a non-existing directory.
    /// </remarks>
    /// <param name="directoryPath">The path of the directory to check and create if necessary.</param>
    private static void EnsureDirectoryExists(string directoryPath)
    {
        if (!System.IO.Directory.Exists(directoryPath))
            System.IO.Directory.CreateDirectory(directoryPath);
    }
}
