using System.Diagnostics;

namespace Ramstack.FileSystem;

/// <summary>
/// Represents the properties of a file node, which can be either a file or a directory.
/// </summary>
[DebuggerDisplay("{ToStringDebugger(),nq}")]
public sealed class VirtualNodeProperties
{
    /// <summary>
    /// Gets an instance of <see cref="VirtualNodeProperties"/> that represents a node with no data or an unavailable state.
    /// </summary>
    public static VirtualNodeProperties Unavailable { get; } =
        new VirtualNodeProperties(default, default, default, -1);

    /// <summary>
    /// Gets the time when the current file or directory was created.
    /// </summary>
    public DateTimeOffset CreationTime { get; }

    /// <summary>
    /// Gets the time when the current file or directory was last accessed.
    /// </summary>
    public DateTimeOffset LastAccessTime { get; }

    /// <summary>
    /// Gets the time when the current file or directory was last modified.
    /// </summary>
    public DateTimeOffset LastWriteTime { get; }

    /// <summary>
    /// Gets the size of the file in bytes.
    /// Returns <c>0</c> for directories and <c>-1</c> for non-existent nodes.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Gets a value indicating whether the current file or directory exists.
    /// Returns <see langword="true"/> if the node exists; otherwise, <see langword="false"/>.
    /// </summary>
    public bool Exists => Length >= 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualNodeProperties"/> class.
    /// </summary>
    /// <param name="creationTime">The time when the node was created.</param>
    /// <param name="lastAccessTime">The time when the node was last accessed.</param>
    /// <param name="lastWriteTime">The time when the node was last modified.</param>
    /// <param name="length">The size of the file in bytes, or <c>0</c> for directories, or <c>-1</c> for non-existent nodes.</param>
    private VirtualNodeProperties(DateTimeOffset creationTime, DateTimeOffset lastAccessTime, DateTimeOffset lastWriteTime, long length) =>
        (CreationTime, LastAccessTime, LastWriteTime, Length) = (creationTime, lastAccessTime, lastWriteTime, length);

    /// <summary>
    /// Creates a new instance of <see cref="VirtualNodeProperties"/> for a file.
    /// </summary>
    /// <param name="creationTime">The time when the file was created.</param>
    /// <param name="lastAccessTime">The time when the file was last accessed.</param>
    /// <param name="lastWriteTime">The time when the file was last modified.</param>
    /// <param name="length">The size of the file in bytes.</param>
    /// <returns>
    /// A new <see cref="VirtualNodeProperties"/> instance representing a file.
    /// </returns>
    public static VirtualNodeProperties CreateFileProperties(DateTimeOffset creationTime, DateTimeOffset lastAccessTime, DateTimeOffset lastWriteTime, long length) =>
        new VirtualNodeProperties(creationTime, lastAccessTime, lastWriteTime, length);

    /// <summary>
    /// Creates a new instance of <see cref="VirtualNodeProperties"/> for a directory.
    /// </summary>
    /// <param name="creationTime">The time when the directory was created.</param>
    /// <param name="lastAccessTime">The time when the directory was last accessed.</param>
    /// <param name="lastWriteTime">The time when the directory was last modified.</param>
    /// <returns>
    /// A new <see cref="VirtualNodeProperties"/> instance representing a directory.
    /// </returns>
    public static VirtualNodeProperties CreateDirectoryProperties(DateTimeOffset creationTime, DateTimeOffset lastAccessTime, DateTimeOffset lastWriteTime) =>
        new VirtualNodeProperties(creationTime, lastAccessTime, lastWriteTime, 0);

    /// <summary>
    /// Returns a string representation of the current instance for debugging purposes.
    /// </summary>
    /// <returns>
    /// A string that provides information about the length of the current instance.
    /// If the length is -1, it returns "Unavailable".
    /// </returns>
    private string ToStringDebugger() =>
        Length >= 0 ? $"Length = {Length}" : "Unavailable";
}
