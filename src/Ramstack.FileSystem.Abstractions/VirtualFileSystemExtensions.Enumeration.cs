namespace Ramstack.FileSystem;

partial class VirtualFileSystemExtensions
{
    /// <summary>
    /// Asynchronously returns an async-enumerable collection of file nodes (both directories and files)
    /// in the specified path that match the given glob pattern.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="pattern">The glob pattern to match against the names of file nodes.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualNode"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualNode> GetFileNodesAsync(this IVirtualFileSystem fs, string path, string pattern, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetFileNodesAsync(pattern, exclude: null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of file nodes (both directories and files)
    /// in the specified path that match the given glob pattern.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="pattern">The glob pattern to match against the names of file nodes.</param>
    /// <param name="exclude">An optional glob pattern to exclude certain file nodes.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualNode"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualNode> GetFileNodesAsync(this IVirtualFileSystem fs, string path, string pattern, string? exclude, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetFileNodesAsync([pattern], exclude is not null ? [exclude] : null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of file nodes (both directories and files)
    /// in the specified path that match any of the given glob patterns.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="patterns">An array of glob patterns to match against the names of file nodes.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualNode"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualNode> GetFileNodesAsync(this IVirtualFileSystem fs, string path, string[] patterns, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetFileNodesAsync(patterns, excludes: null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of file nodes (both directories and files)
    /// in the specified path that match any of the given glob patterns.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="patterns">An array of glob patterns to match against the names of file nodes.</param>
    /// <param name="excludes">An optional array of glob patterns to exclude file nodes.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualNode"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualNode> GetFileNodesAsync(this IVirtualFileSystem fs, string path, string[] patterns, string[]? excludes, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetFileNodesAsync(patterns, excludes, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of files in the specified path that match the given glob pattern.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="pattern">The glob pattern to match against the names of files.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualFile"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualFile> GetFilesAsync(this IVirtualFileSystem fs, string path, string pattern, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetFilesAsync(pattern, exclude: null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of files in the specified path that match the given glob pattern.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="pattern">The glob pattern to match against the names of files.</param>
    /// <param name="exclude">An optional glob pattern to exclude certain files.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualFile"/> instances.</returns>
    public static IAsyncEnumerable<VirtualFile> GetFilesAsync(this IVirtualFileSystem fs, string path, string pattern, string? exclude, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetFilesAsync([pattern], exclude is not null ? [exclude] : null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of files in the specified path that match any of the given glob patterns.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="patterns">An array of glob patterns to match against the names of files.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualFile"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualFile> GetFilesAsync(this IVirtualFileSystem fs, string path, string[] patterns, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetFilesAsync(patterns, excludes: null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of files in the specified path that match any of the given glob patterns.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="patterns">An array of glob patterns to match against the names of files.</param>
    /// <param name="excludes">An optional array of glob patterns to exclude files.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualFile"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualFile> GetFilesAsync(this IVirtualFileSystem fs, string path, string[] patterns, string[]? excludes, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetFilesAsync(patterns, excludes, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of directories in the specified path that match the given glob pattern.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="pattern">The glob pattern to match against the names of directories.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualDirectory"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualDirectory> GetDirectoriesAsync(this IVirtualFileSystem fs, string path, string pattern, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetDirectoriesAsync(pattern, exclude: null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of directories in the specified path that match the given glob pattern.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="pattern">The glob pattern to match against the names of directories.</param>
    /// <param name="exclude">An optional glob pattern to exclude certain directories.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualDirectory"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualDirectory> GetDirectoriesAsync(this IVirtualFileSystem fs, string path, string pattern, string? exclude, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetDirectoriesAsync([pattern], exclude is not null ? [exclude] : null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of directories in the specified path that match any of the given glob patterns.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="patterns">An array of glob patterns to match against the names of directories.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualDirectory"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualDirectory> GetDirectoriesAsync(this IVirtualFileSystem fs, string path, string[] patterns, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetDirectoriesAsync(patterns, excludes: null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of directories in the specified path that match any of the given glob patterns.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="patterns">An array of glob patterns to match against the names of directories.</param>
    /// <param name="excludes">An optional array of glob patterns to exclude directories.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualDirectory"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualDirectory> GetDirectoriesAsync(this IVirtualFileSystem fs, string path, string[] patterns, string[]? excludes, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetDirectoriesAsync(patterns, excludes, cancellationToken);
}
