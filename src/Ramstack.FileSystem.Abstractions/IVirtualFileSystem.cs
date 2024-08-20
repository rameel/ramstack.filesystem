namespace Ramstack.FileSystem;

/// <summary>
/// Provides an abstraction for accessing files and directories within a virtual file system.
/// </summary>
public interface IVirtualFileSystem : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the file system is read-only.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Retrieves a file located at the specified path.
    /// </summary>
    /// <param name="path">The path of the file to locate.</param>
    /// <returns>
    /// A <see cref="VirtualFile"/> instance representing the specified file.
    /// </returns>
    VirtualFile GetFile(string path);

    /// <summary>
    /// Retrieves a directory located at the specified path.
    /// </summary>
    /// <param name="path">The path of the directory to locate.</param>
    /// <returns>
    /// A <see cref="VirtualDirectory"/> instance representing the specified directory.
    /// </returns>
    VirtualDirectory GetDirectory(string path);
}
