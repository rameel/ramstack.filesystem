using System.Diagnostics;
using System.IO.Enumeration;

using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem.Physical;

/// <summary>
/// Represents an implementation of <see cref="VirtualDirectory"/> that maps to a physical directory.
/// </summary>
internal sealed class PhysicalDirectory : VirtualDirectory
{
    /// <summary>
    /// Default options for file enumeration, configured to enumerate all files,
    /// including system and hidden files, not recurse through subdirectories,
    /// and ignore inaccessible files.
    /// </summary>
    private static readonly EnumerationOptions DefaultOptions = new()
    {
        AttributesToSkip = 0,
        RecurseSubdirectories = false,
        IgnoreInaccessible = true
    };

    private readonly PhysicalFileSystem _fs;
    private readonly string _physicalPath;

    /// <inheritdoc/>
    public override IVirtualFileSystem FileSystem => _fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalDirectory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The path of the directory.</param>
    public PhysicalDirectory(PhysicalFileSystem fileSystem, string path) : base(path) =>
        (_fs, _physicalPath) = (fileSystem, fileSystem.GetPhysicalPath(path));

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        var info = new DirectoryInfo(_physicalPath);
        var properties = info.Exists
            ? VirtualNodeProperties.CreateDirectoryProperties(
                creationTime: info.CreationTimeUtc,
                lastAccessTime: info.LastAccessTime,
                lastWriteTime: info.LastWriteTimeUtc)
            : null;

        return new ValueTask<VirtualNodeProperties?>(properties);
    }

    /// <inheritdoc />
    protected override ValueTask CreateCoreAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_physicalPath);
        return default;
    }

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            Directory.Delete(_physicalPath, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
            // The path to the deleting directory may not exist.
            // This is a safe scenario and does not require further handling.
        }

        return default;
    }

    /// <inheritdoc />
    protected override IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync(CancellationToken cancellationToken)
    {
        // TOCTOU (Time-of-check to time-of-use)
        // -------------------------------------
        // We accept that a "DirectoryNotFoundException" may be thrown if
        // the directory becomes unavailable, such as being deleted after
        // its existence was checked.

        var nodes = Directory.Exists(_physicalPath)
            ? new FileSystemEnumerable<VirtualNode>(_physicalPath, FindTransform, DefaultOptions)
            : Enumerable.Empty<VirtualNode>();

        return nodes.ToAsyncEnumerable();

        VirtualNode FindTransform(ref FileSystemEntry entry)
        {
            var fullName = VirtualPath.Join(FullName, entry.FileName);
            return entry.IsDirectory
                ? new PhysicalDirectory(_fs, fullName)
                : new PhysicalFile(_fs, fullName);
        }
    }

    /// <inheritdoc />
    protected override IAsyncEnumerable<VirtualFile> GetFilesCoreAsync(CancellationToken cancellationToken)
    {
        var nodes = Enumerable.Empty<VirtualFile>();

        if (Directory.Exists(_physicalPath))
        {
            nodes = new FileSystemEnumerable<VirtualFile>(_physicalPath, FindTransform, DefaultOptions)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory
            };
        }

        return nodes.ToAsyncEnumerable();

        VirtualFile FindTransform(ref FileSystemEntry entry)
        {
            Debug.Assert(entry.IsDirectory == false);

            var fullName = VirtualPath.Join(FullName, entry.FileName);
            return new PhysicalFile(_fs, fullName);
        }
    }

    /// <inheritdoc />
    protected override IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync(CancellationToken cancellationToken)
    {
        var nodes = Enumerable.Empty<VirtualDirectory>();

        if (Directory.Exists(_physicalPath))
        {
            nodes = new FileSystemEnumerable<VirtualDirectory>(_physicalPath, FindTransform, DefaultOptions)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => entry.IsDirectory
            };
        }

        return nodes.ToAsyncEnumerable();

        VirtualDirectory FindTransform(ref FileSystemEntry entry)
        {
            Debug.Assert(entry.IsDirectory);

            var fullName = VirtualPath.Join(FullName, entry.FileName);
            return new PhysicalDirectory(_fs, fullName);
        }
    }
}
