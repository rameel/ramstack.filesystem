using System.IO.Compression;
using System.Runtime.CompilerServices;

using Ramstack.FileSystem.Null;

namespace Ramstack.FileSystem.Zip;

/// <summary>
/// Represents a file system backed by a ZIP archive.
/// </summary>
/// <remarks>
/// <b>WARNING:</b>
/// <para>
///   The <see cref="ZipFileSystem"/> is not thread-safe and allows reading only one file at a time, as it relies on
///   <see cref="ZipArchive"/>, which does not support parallel read operations or simultaneous opening of multiple streams.
/// </para>
/// <para>
///   You may use this class only if you can guarantee that:
///   <list type="bullet">
///     <item><description>Only one file is open for reading at a time.</description></item>
///     <item><description>No file is accessed concurrently.</description></item>
///   </list>
/// </para>
/// </remarks>
[Obsolete("Deprecated due to thread safety limitations and parallel file access capabilities.")]
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
        _directories.GetValueOrDefault(VirtualPath.Normalize(path));

    /// <summary>
    /// Initializes the file system with entries from the specified ZIP archive.
    /// </summary>
    /// <param name="archive">The ZIP archive to read entries from.</param>
    /// <param name="cache">A dictionary to cache the directory and file nodes.</param>
    private void Initialize(ZipArchive archive, Dictionary<string, VirtualNode> cache)
    {
        foreach (var entry in archive.Entries)
        {
            //
            // Strip common path prefixes from zip entries to handle archives
            // saved with absolute paths.
            //
            var path = VirtualPath.Normalize(
                entry.FullName[GetPrefixLength(entry.FullName)..]);

            if (VirtualPath.HasTrailingSlash(entry.FullName))
            {
                GetOrCreateDirectory(path);
                continue;
            }

            var directory = GetOrCreateDirectory(VirtualPath.GetDirectoryName(path));
            var file = new ZipFile(this, path, entry);

            //
            // Archives legitimately may contain entries with identical names,
            // so skip if a file with this name has already been added,
            // avoiding duplicates in the directory file list.
            //
            if (cache.TryAdd(path, file))
                directory.RegisterNode(file);
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetPrefixLength(string path)
    {
        //
        // Check only well-known prefixes.
        // Note: Since entry names can be arbitrary,
        // we specifically target only common absolute path patterns.
        //

        if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(@"\\.\UNC\", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("//?/UNC/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("//./UNC/", StringComparison.OrdinalIgnoreCase))
            return 8;

        if (path.StartsWith(@"\\?\", StringComparison.Ordinal)
            || path.StartsWith(@"\\.\", StringComparison.Ordinal)
            || path.StartsWith("//?/", StringComparison.Ordinal)
            || path.StartsWith("//./", StringComparison.Ordinal))
            return path.Length >= 6 && IsAsciiLetter(path[4]) && path[5] == ':' ? 6 : 4;

        if (path.Length >= 2
            && IsAsciiLetter(path[0]) && path[1] == ':')
            return 2;

        return 0;

        static bool IsAsciiLetter(char ch) =>
            (uint)((ch | 0x20) - 'a') <= 'z' - 'a';
    }

}
