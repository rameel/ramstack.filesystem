using Ramstack.Globbing;

namespace Ramstack.FileSystem.Globbing;

/// <summary>
/// Provides a file system implementation that filters files and directories of the underlying file system using glob patterns.
/// </summary>
/// <remarks>
/// The <see cref="GlobbingFileSystem"/> class wraps around another <see cref="IVirtualFileSystem"/> and applies glob-based
/// filtering rules to determine which files and directories to include or exclude. This allows for flexible and powerful
/// file and directory selection using standard glob patterns.
/// </remarks>
/// <example>
/// <code>
/// var underlying = new PhysicalFileSystem(@"C:\MyDirectory");
/// var fs = new GlobbingFileSystem(underlying, patterns: ["**/*.txt", "docs/*.md"], excludes: ["**/README.md"]);
/// await foreach (var file in fs.GetFilesAsync("/"))
///     Console.WriteLine(file.FullName);
/// </code>
/// </example>
public sealed class GlobbingFileSystem : IVirtualFileSystem
{
    private readonly IVirtualFileSystem _fs;
    private readonly string[] _patterns;
    private readonly string[] _excludes;

    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobbingFileSystem"/> class with a single include pattern and an optional exclude pattern.
    /// </summary>
    /// <param name="fileSystem">The underlying file system.</param>
    /// <param name="pattern">The pattern to include in the enumeration.</param>
    /// <param name="exclude">The optional pattern to exclude from the enumeration.</param>
    public GlobbingFileSystem(IVirtualFileSystem fileSystem, string pattern, string? exclude = null)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(pattern);

        _fs = fileSystem;
        _patterns = [pattern];
        _excludes = exclude is not null ? [exclude] : [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobbingFileSystem"/> class with multiple include patterns and optional exclude patterns.
    /// </summary>
    /// <param name="fileSystem">The underlying file system.</param>
    /// <param name="patterns">The patterns to include in the enumeration.</param>
    /// <param name="excludes">The optional patterns to exclude from the enumeration.</param>
    public GlobbingFileSystem(IVirtualFileSystem fileSystem, string[] patterns, string[]? excludes = null)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(patterns);

        _fs = fileSystem;
        _patterns = patterns.ToArray();
        _excludes = excludes?.ToArray() ?? [];
    }

    /// <inheritdoc />
    public VirtualFile GetFile(string path)
    {
        path = VirtualPath.GetFullPath(path);

        var file = _fs.GetFile(path);
        var fileIncluded = IsFileIncluded(path);
        return new GlobbingFile(this, file, fileIncluded);
    }

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path)
    {
        path = VirtualPath.GetFullPath(path);

        var directory = _fs.GetDirectory(path);
        var directoryIncluded = IsDirectoryIncluded(path);
        return new GlobbingDirectory(this, directory, directoryIncluded);
    }

    /// <inheritdoc />
    public void Dispose() =>
        _fs.Dispose();

    /// <summary>
    /// Determines if a file is included based on the specified patterns and exclusions.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>
    /// <see langword="true" /> if the file is included;
    /// otherwise, <see langword="false" />.
    /// </returns>
    internal bool IsFileIncluded(string path) =>
        !IsExcluded(path) && IsIncluded(path);

    /// <summary>
    /// Determines if a directory is included based on the specified exclusions.
    /// </summary>
    /// <param name="path">The path of the directory.</param>
    /// <returns>
    /// <see langword="true" /> if the directory is included;
    /// otherwise, <see langword="false" />.
    /// </returns>
    internal bool IsDirectoryIncluded(string path) =>
        !IsExcluded(path);

    /// <summary>
    /// Checks if a path matches any of the include patterns.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>
    /// <see langword="true" /> if the path matches an include pattern;
    /// otherwise, <see langword="false" />.
    /// </returns>
    internal bool IsIncluded(string path)
    {
        foreach (var pattern in _patterns)
            if (Matcher.IsMatch(path, pattern, MatchFlags.Unix))
                return true;

        return false;
    }

    /// <summary>
    /// Checks if a path matches any of the exclude patterns.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>
    /// <see langword="true" /> if the path matches an exclude pattern;
    /// otherwise, <see langword="false" />.
    /// </returns>
    internal bool IsExcluded(string path)
    {
        foreach (var pattern in _excludes)
            if (Matcher.IsMatch(path, pattern, MatchFlags.Unix))
                return true;

        return false;
    }
}
