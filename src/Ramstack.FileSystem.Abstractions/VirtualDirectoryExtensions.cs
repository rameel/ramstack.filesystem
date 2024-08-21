namespace Ramstack.FileSystem;

/// <summary>
/// Provides extension methods for the <see cref="VirtualDirectory"/> class.
/// </summary>
public static class VirtualDirectoryExtensions
{
    /// <summary>
    /// Asynchronously returns an async-enumerable collection of file nodes (both directories and files)
    /// within the specified directory that match the given glob pattern.
    /// </summary>
    /// <param name="directory">The <see cref="VirtualDirectory"/> representing the directory to search.</param>
    /// <param name="pattern">The glob pattern to match against the names of file nodes.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualNode"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualNode> GetFileNodesAsync(this VirtualDirectory directory, string pattern, CancellationToken cancellationToken = default) =>
        directory.GetFileNodesAsync(pattern, exclude: null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of file nodes (both directories and files)
    /// within the specified directory that match the given glob pattern.
    /// </summary>
    /// <param name="directory">The <see cref="VirtualDirectory"/> representing the directory to search.</param>
    /// <param name="pattern">The glob pattern to match against the names of file nodes.</param>
    /// <param name="exclude">An optional glob pattern to exclude certain file nodes.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualNode"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualNode> GetFileNodesAsync(this VirtualDirectory directory, string pattern, string? exclude, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        return directory.GetFileNodesAsync([pattern], exclude is not null ? [exclude] : null, cancellationToken);
    }

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of file nodes (both directories and files)
    /// within the specified directory that match any of the given glob patterns.
    /// </summary>
    /// <param name="directory">The <see cref="VirtualDirectory"/> representing the directory to search.</param>
    /// <param name="patterns">An array of glob patterns to match against the names of file nodes.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualNode"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualNode> GetFileNodesAsync(this VirtualDirectory directory, string[] patterns, CancellationToken cancellationToken = default) =>
        directory.GetFileNodesAsync(patterns, excludes: null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of files within the specified directory
    /// that match the given glob pattern.
    /// </summary>
    /// <param name="directory">The <see cref="VirtualDirectory"/> representing the directory to search.</param>
    /// <param name="pattern">The glob pattern to match against the names of files.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualFile"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualFile> GetFilesAsync(this VirtualDirectory directory, string pattern, CancellationToken cancellationToken = default) =>
        directory.GetFilesAsync(pattern, exclude: null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of files within the specified directory
    /// that match the given glob pattern.
    /// </summary>
    /// <param name="directory">The <see cref="VirtualDirectory"/> representing the directory to search.</param>
    /// <param name="pattern">The glob pattern to match against the names of files.</param>
    /// <param name="exclude">An optional glob pattern to exclude certain files.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualFile"/> instances.</returns>
    public static IAsyncEnumerable<VirtualFile> GetFilesAsync(this VirtualDirectory directory, string pattern, string? exclude, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        return directory.GetFilesAsync([pattern], exclude is not null ? [exclude] : null, cancellationToken);
    }

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of files within the specified directory
    /// that match any of the given glob patterns.
    /// </summary>
    /// <param name="directory">The <see cref="VirtualDirectory"/> representing the directory to search.</param>
    /// <param name="patterns">An array of glob patterns to match against the names of files.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualFile"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualFile> GetFilesAsync(this VirtualDirectory directory, string[] patterns, CancellationToken cancellationToken = default) =>
        directory.GetFilesAsync(patterns, excludes: null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of directories within the specified directory
    /// that match the given glob pattern.
    /// </summary>
    /// <param name="directory">The <see cref="VirtualDirectory"/> representing the directory to search.</param>
    /// <param name="pattern">The glob pattern to match against the names of directories.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualDirectory"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualDirectory> GetDirectoriesAsync(this VirtualDirectory directory, string pattern, CancellationToken cancellationToken = default) =>
        directory.GetDirectoriesAsync(pattern, exclude: null, cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of directories within the specified directory
    /// that match the given glob pattern.
    /// </summary>
    /// <param name="directory">The <see cref="VirtualDirectory"/> representing the directory to search.</param>
    /// <param name="pattern">The glob pattern to match against the names of directories.</param>
    /// <param name="exclude">An optional glob pattern to exclude certain directories.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualDirectory"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualDirectory> GetDirectoriesAsync(this VirtualDirectory directory, string pattern, string? exclude, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        return directory.GetDirectoriesAsync([pattern], exclude is not null ? [exclude] : null, cancellationToken);
    }

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of directories within the specified directory
    /// that match any of the given glob patterns.
    /// </summary>
    /// <param name="directory">The <see cref="VirtualDirectory"/> representing the directory to search.</param>
    /// <param name="patterns">An array of glob patterns to match against the names of directories.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualDirectory"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualDirectory> GetDirectoriesAsync(this VirtualDirectory directory, string[] patterns, CancellationToken cancellationToken = default) =>
        directory.GetDirectoriesAsync(patterns, excludes: null, cancellationToken);
}
