using System.Diagnostics;
using System.IO.Enumeration;

using Ramstack.FileSystem.Internal;
using Ramstack.FileSystem.Null;

namespace Ramstack.FileSystem.Physical;

/// <summary>
/// Represents an implementation of <see cref="IVirtualFileSystem"/> for physical files.
/// </summary>
[DebuggerDisplay("Root = {_root,nq}")]
public sealed class PhysicalFileSystem : IVirtualFileSystem
{
    private readonly string _root;
    private readonly ExclusionFilters _exclusionFilters;

    /// <summary>
    /// Gets or sets a value indicating whether the file system is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalFileSystem"/> class.
    /// </summary>
    /// <param name="path">The physical path of the directory.</param>
    /// <param name="exclusionFilters">The filter to exclude specific types of files and directories.
    /// Defaults to <see cref="ExclusionFilters.Sensitive"/>.</param>
    public PhysicalFileSystem(string path, ExclusionFilters exclusionFilters = ExclusionFilters.Sensitive)
    {
        _exclusionFilters = exclusionFilters;
        if (!Path.IsPathRooted(path))
            Error(path);

        _root = Path.GetFullPath(path);

        static void Error(string path) =>
            throw new ArgumentException($"The path '{path}' must be absolute.");
    }

    /// <inheritdoc />
    public VirtualFile GetFile(string path)
    {
        path = VirtualPath.GetFullPath(path);
        var physicalPath = GetPhysicalPath(path);

        if (!IsFullyExcluded(physicalPath))
            return new PhysicalFile(this, path, physicalPath);

        return new NotFoundFile(this, path);
    }

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path)
    {
        path = VirtualPath.GetFullPath(path);
        var physicalPath = GetPhysicalPath(path);

        if (!IsFullyExcluded(physicalPath))
            return new PhysicalDirectory(this, path, physicalPath);

        return new NotFoundDirectory(this, path);
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
    }

    /// <summary>
    /// Determines whether the specified <see cref="FileSystemEntry"/> is excluded based on the configured <see cref="ExclusionFilters"/>.
    /// </summary>
    /// <param name="entry">The <see cref="FileSystemEntry"/> to check for exclusion.</param>
    /// <returns>
    /// <see langword="true" /> if the specified <see cref="FileSystemEntry"/> is excluded; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This method checks the entry against the configured exclusion filters, such as dot-prefixed, hidden, and system attributes.
    /// </remarks>
    internal bool IsExcluded(ref FileSystemEntry entry)
    {
        if ((_exclusionFilters & ExclusionFilters.DotPrefixed) != 0)
            if (!entry.FileName.StartsWith("."))
                return true;

        const int Mask = (int)(ExclusionFilters.Hidden | ExclusionFilters.System);
        return ((int)_exclusionFilters & (int)entry.Attributes & Mask) != 0;
    }

    /// <summary>
    /// Determines whether the specified physical path is fully excluded based on the configured exclusion filters.
    /// </summary>
    /// <param name="physicalPath">The physical path to check for exclusion.</param>
    /// <returns>
    /// <see langword="true" /> if the specified physical path is fully excluded; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This method traverses up the directory hierarchy from the specified path to the root path,
    /// checking each directory against the exclusion filters.
    /// </remarks>
    private bool IsFullyExcluded(string physicalPath)
    {
        if (_exclusionFilters != ExclusionFilters.None)
        {
            while (physicalPath != _root)
            {
                if (IsExcluded(physicalPath))
                    return true;

                physicalPath = Path.GetDirectoryName(physicalPath)!;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified path is excluded based on the configured exclusion filters.
    /// </summary>
    /// <param name="physicalPath">The physical path to check for exclusion.</param>
    /// <returns>
    /// <see langword="true" /> if the specified path is excluded; otherwise, <see langword="false" />.
    /// </returns>
    private bool IsExcluded(string physicalPath)
    {
        try
        {
            const int Mask = (int)(ExclusionFilters.Hidden | ExclusionFilters.System);

            var filters = (int)_exclusionFilters;
            if ((filters & (int)ExclusionFilters.DotPrefixed) != 0)
                if (Path.GetFileName(physicalPath.AsSpan()).StartsWith("."))
                    return true;

            filters &= Mask;

            if (filters != 0)
            {
                var attributes = File.GetAttributes(physicalPath);
                if (((int)attributes & filters) != 0)
                    return true;
            }
        }
        catch (IOException)
        {
        }

        return false;
    }

    /// <summary>
    /// Converts the specified virtual path within this file system
    /// to its corresponding physical path.
    /// </summary>
    /// <param name="path">The virtual path within this file system.</param>
    /// <returns>
    /// The corresponding physical path.
    /// </returns>
    private string GetPhysicalPath(string path)
    {
        Debug.Assert(path == VirtualPath.GetFullPath(path));
        return Path.Join(_root, path);
    }
}
