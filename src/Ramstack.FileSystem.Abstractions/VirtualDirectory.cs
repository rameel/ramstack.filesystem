using System.Diagnostics;
using System.Runtime.CompilerServices;

using Ramstack.FileSystem.Internal;
using Ramstack.Globbing.Traversal;

namespace Ramstack.FileSystem;

/// <summary>
/// Represents a virtual directory within a specified file system.
/// </summary>
[DebuggerDisplay("{FullName,nq}")]
public abstract class VirtualDirectory : VirtualNode
{
    /// <summary>
    /// Gets a value indicating whether the current directory is the root directory.
    /// </summary>
    public bool IsRoot => FullName == "/";

    /// <summary>
    /// Gets a <see cref="VirtualDirectory"/> instance representing the parent directory.
    /// </summary>
    public VirtualDirectory? Parent
    {
        get
        {
            if (IsRoot)
                return null;

            var directoryName = VirtualPath.GetDirectoryName(FullName);
            return FileSystem.GetDirectory(directoryName);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualDirectory"/> class with the specified path.
    /// </summary>
    /// <param name="path">The full path of the directory.</param>
    protected VirtualDirectory(string path) : base(path)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualDirectory"/> class with the specified path and properties.
    /// </summary>
    /// <param name="path">The full path of the directory.</param>
    /// <param name="properties">The properties of the directory.</param>
    protected VirtualDirectory(string path, VirtualNodeProperties? properties) : base(path, properties)
    {
    }

    /// <summary>
    /// Asynchronously creates the current directory.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public ValueTask CreateAsync(CancellationToken cancellationToken = default)
    {
        EnsureWritable();
        Refresh();

        return CreateCoreAsync(cancellationToken);
    }

    /// <summary>
    /// Asynchronously deletes the current directory, including all its subdirectories and files.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public ValueTask DeleteAsync(CancellationToken cancellationToken = default)
    {
        EnsureWritable();
        Refresh();

        return DeleteCoreAsync(cancellationToken);
    }

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of file nodes (both directories and files) within the current directory.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualNode"/> instances.
    /// </returns>
    public IAsyncEnumerable<VirtualNode> GetFileNodesAsync(CancellationToken cancellationToken = default) =>
        GetFileNodesCoreAsync(cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of files within the current directory.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualFile"/> instances.
    /// </returns>
    public IAsyncEnumerable<VirtualFile> GetFilesAsync(CancellationToken cancellationToken = default) =>
        GetFilesCoreAsync(cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of directories within the current directory.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualDirectory"/> instances.
    /// </returns>
    public IAsyncEnumerable<VirtualDirectory> GetDirectoriesAsync(CancellationToken cancellationToken = default) =>
        GetDirectoriesCoreAsync(cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of file nodes (both directories and files)
    /// within the current directory that match any of the specified glob patterns.
    /// </summary>
    /// <param name="patterns">An array of glob patterns to match against the names of file nodes.</param>
    /// <param name="excludes">An optional array of glob patterns to exclude file nodes.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualNode"/> instances.
    /// </returns>
    public IAsyncEnumerable<VirtualNode> GetFileNodesAsync(string[] patterns, string[]? excludes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patterns);

        return new FileTreeAsyncEnumerable<VirtualNode, VirtualNode>(this, cancellationToken)
        {
            Patterns = patterns,
            Excludes = excludes ?? [],
            FileNameSelector = node => node.FullName,
            ShouldRecursePredicate = node => node is VirtualDirectory,
            ChildrenSelector = (node, token) => ((VirtualDirectory)node).GetFileNodesCoreAsync(token),
            ResultSelector = node => node
        };
    }

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of files within the current directory
    /// that match any of the specified glob patterns.
    /// </summary>
    /// <param name="patterns">An array of glob patterns to match against the names of files.</param>
    /// <param name="excludes">An optional array of glob patterns to exclude files.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualFile"/> instances.
    /// </returns>
    public IAsyncEnumerable<VirtualFile> GetFilesAsync(string[] patterns, string[]? excludes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patterns);

        return new FileTreeAsyncEnumerable<VirtualNode, VirtualFile>(this, cancellationToken)
        {
            Patterns = patterns,
            Excludes = excludes ?? [],
            FileNameSelector = node => node.FullName,
            ShouldIncludePredicate = node => node is VirtualFile,
            ShouldRecursePredicate = node => node is VirtualDirectory,
            ChildrenSelector = (node, token) => ((VirtualDirectory)node).GetFileNodesCoreAsync(token),
            ResultSelector = node => (VirtualFile)node
        };
    }

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of directories within the current directory
    /// that match any of the specified glob patterns.
    /// </summary>
    /// <param name="patterns">An array of glob patterns to match against the names of directories.</param>
    /// <param name="excludes">An optional array of glob patterns to exclude directories.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualDirectory"/> instances.
    /// </returns>
    public IAsyncEnumerable<VirtualDirectory> GetDirectoriesAsync(string[] patterns, string[]? excludes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patterns);

        return new FileTreeAsyncEnumerable<VirtualNode, VirtualDirectory>(this, cancellationToken)
        {
            Patterns = patterns,
            Excludes = excludes ?? [],
            FileNameSelector = node => node.FullName,
            ShouldIncludePredicate = node => node is VirtualDirectory,
            ShouldRecursePredicate = node => node is VirtualDirectory,
            ChildrenSelector = (node, token) => ((VirtualDirectory)node).GetFileNodesCoreAsync(token),
            ResultSelector = node => (VirtualDirectory)node
        };
    }

    /// <summary>
    /// Core implementation for asynchronously creating the current directory.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    protected abstract ValueTask CreateCoreAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Core implementation for asynchronously deleting the current directory, including all its subdirectories and files.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    protected abstract ValueTask DeleteCoreAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Core implementation for asynchronously returning an async-enumerable collection of file nodes (both directories and files) within the current directory.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualNode"/> instances.
    /// </returns>
    protected abstract IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Core implementation for asynchronously returning an async-enumerable collection of files within the current directory.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualFile"/> instances.
    /// </returns>
    protected virtual async IAsyncEnumerable<VirtualFile> GetFilesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var node in GetFileNodesCoreAsync(cancellationToken).ConfigureAwait(false))
        {
            if (node is VirtualFile file)
                yield return file;
        }
    }

    /// <summary>
    /// Core implementation for asynchronously returning an async-enumerable collection of directories within the current directory.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualDirectory"/> instances.
    /// </returns>
    protected virtual async IAsyncEnumerable<VirtualDirectory> GetDirectoriesCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var node in GetFileNodesCoreAsync(cancellationToken).ConfigureAwait(false))
        {
            if (node is VirtualDirectory directory)
                yield return directory;
        }
    }
}
