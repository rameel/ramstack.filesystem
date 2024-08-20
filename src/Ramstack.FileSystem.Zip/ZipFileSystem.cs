using System.IO.Compression;

using Ramstack.FileSystem.Internal;
using Ramstack.FileSystem.Null;

namespace Ramstack.FileSystem.Zip;

/// <summary>
/// Represents a file system backed by a ZIP archive.
/// </summary>
public sealed class ZipFileSystem : IVirtualFileSystem
{
    private readonly ZipArchive _archive;
    private readonly Dictionary<string, VirtualNode> _directories;

    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZipFileSystem"/> class
    /// using a ZIP archive located at the specified file path.
    /// </summary>
    /// <param name="path">The path to the ZIP archive file.</param>
    public ZipFileSystem(string path)
        : this(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZipFileSystem"/> class
    /// using a stream containing a ZIP archive.
    /// </summary>
    /// <param name="stream">The stream containing the ZIP archive.</param>
    /// <param name="leaveOpen"><see langword="true" /> to leave the stream open
    /// after the <see cref="ZipFileSystem"/> object is disposed; otherwise, <see langword="false" />.</param>
    public ZipFileSystem(Stream stream, bool leaveOpen = false)
        : this(new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZipFileSystem"/> class
    /// using an existing <see cref="ZipArchive"/>.
    /// </summary>
    /// <param name="archive">The <see cref="ZipArchive"/> instance
    /// to use for providing access to ZIP archive content.</param>
    public ZipFileSystem(ZipArchive archive)
    {
        _archive = archive;
        _directories = new Dictionary<string, VirtualNode>
            {
                ["/"] = new ZipDirectory(this, "/")
            };

        Initialize(archive, _directories);
    }

    /// <inheritdoc />
    public VirtualFile GetFile(string path) =>
        FindNode(path) as VirtualFile ?? new NotFoundFile(this, path);

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path) =>
        FindNode(path) as VirtualDirectory ?? new NotFoundDirectory(this, path);

    /// <inheritdoc />
    public void Dispose() =>
        _archive.Dispose();

    /// <summary>
    /// Finds a <see cref="VirtualNode"/> by its path.
    /// </summary>
    /// <param name="path">The path of the node to find.</param>
    /// <returns>
    /// The <see cref="VirtualNode"/> if found; otherwise, <see langword="null"/>.
    /// </returns>
    private VirtualNode? FindNode(string path) =>
        _directories.GetValueOrDefault(VirtualPath.GetFullPath(path));

    /// <summary>
    /// Initializes the file system with entries from the specified ZIP archive.
    /// </summary>
    /// <param name="archive">The ZIP archive to read entries from.</param>
    /// <param name="cache">A dictionary to cache the directory and file nodes.</param>
    private void Initialize(ZipArchive archive, Dictionary<string, VirtualNode> cache)
    {
        foreach (var entry in archive.Entries)
        {
            // Skipping directories
            // --------------------
            // Directory entries are denoted by a trailing slash '/' in their names.
            //
            // Since we can't rely on all archivers to include directory entries in archives,
            // it's simpler to assume their absence and ignore any entries ending with a forward slash '/'.

            if (entry.FullName.EndsWith('/'))
                continue;

            var path = VirtualPath.Normalize(entry.FullName);
            var directory = GetOrCreateDirectory(VirtualPath.GetDirectoryName(path));
            var file = new ZipFile(this, path, entry);

            directory.RegisterNode(file);
            cache.Add(path, file);
        }

        ZipDirectory GetOrCreateDirectory(string path)
        {
            if (cache.TryGetValue(path, out var di))
                return (ZipDirectory)di;

            di = new ZipDirectory(this, path);
            var parent = GetOrCreateDirectory(VirtualPath.GetDirectoryName(path));
            parent.RegisterNode(di);
            cache.Add(path, di);

            return (ZipDirectory)di;
        }
    }
}
