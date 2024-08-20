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
            ? VirtualNodeProperties.File(
                info.CreationTimeUtc,
                info.LastAccessTime,
                info.LastWriteTimeUtc,
                info.Length)
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

        return new ValueTask<Stream>(stream);
    }

    /// <inheritdoc />
    protected override async ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken)
    {
        EnsureDirectoryExists(Path.GetDirectoryName(_physicalPath)!);

        const FileOptions options = FileOptions.Asynchronous | FileOptions.SequentialScan;

        // If the file needs to be overwritten, we use FileMode.OpenOrCreate instead of FileMode.Create.
        // This is because using FileMode.Create on a file with the FileAttributes.Hidden attribute will
        // throw a System.UnauthorizedAccessException: Access to the path is denied.
        //
        // However, FileMode.OpenOrCreate does not truncate the file, so we manually set the file length
        // after writing to remove any leftover data.
        var fileMode = overwrite ? FileMode.OpenOrCreate : FileMode.CreateNew;

        await using var fs = new FileStream(_physicalPath, fileMode, FileAccess.Write, FileShare.None, DefaultBufferSize, options);
        await stream.CopyToAsync(fs, cancellationToken);

        if (overwrite)
            fs.SetLength(fs.Position);
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
