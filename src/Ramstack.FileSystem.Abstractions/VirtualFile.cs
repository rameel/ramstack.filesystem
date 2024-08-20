using System.Diagnostics;

using Ramstack.FileSystem.Internal;

namespace Ramstack.FileSystem;

/// <summary>
/// Represents a virtual file within a specified file system.
/// </summary>
[DebuggerDisplay("{FullName,nq}")]
public abstract class VirtualFile : VirtualNode
{
    /// <summary>
    /// Gets the full path of the directory containing this file.
    /// </summary>
    public string DirectoryName => VirtualPath.GetDirectoryName(FullName);

    /// <summary>
    /// Gets a <see cref="VirtualDirectory"/> instance representing the parent directory of this file.
    /// </summary>
    public VirtualDirectory Directory => FileSystem.GetDirectory(DirectoryName);

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualFile"/> class with the specified path.
    /// </summary>
    /// <param name="path">The full path of the file.</param>
    protected VirtualFile(string path) : base(path)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualFile"/> class with the specified path and properties.
    /// </summary>
    /// <param name="path">The full path of the file.</param>
    /// <param name="properties">The properties of the file.</param>
    protected VirtualFile(string path, VirtualNodeProperties? properties) : base(path, properties)
    {
    }

    /// <summary>
    /// Asynchronously opens the file for reading.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// with a result of type <see cref="Stream"/> for reading the file content.
    /// </returns>
    public ValueTask<Stream> OpenReadAsync(CancellationToken cancellationToken = default) =>
        OpenReadCoreAsync(cancellationToken);

    /// <summary>
    /// Asynchronously opens the file for writing.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// with a result of type <see cref="Stream"/> for writing to the file.
    /// </returns>
    public ValueTask<Stream> OpenWriteAsync(CancellationToken cancellationToken = default)
    {
        EnsureWritable();
        Refresh();

        return OpenWriteCoreAsync(cancellationToken);
    }

    /// <summary>
    /// Asynchronously writes the specified content to the file, creating a new file or overwriting an existing one.
    /// </summary>
    /// <param name="stream">A <see cref="Stream"/> containing the content to write to the file.</param>
    /// <param name="overwrite"><see langword="true"/> to overwrite an existing file; <see langword="false"/> to throw an exception if the file already exists.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>If the file does not exist, it will be created.</description></item>
    ///   <item><description>If it exists and <paramref name="overwrite"/> is <see langword="true"/>, the existing file will be overwritten.</description></item>
    ///   <item><description>If <paramref name="overwrite"/> is <see langword="false"/> and the file exists, an exception will be thrown.</description></item>
    /// </list>
    /// </remarks>
    public ValueTask WriteAsync(Stream stream, bool overwrite, CancellationToken cancellationToken = default)
    {
        EnsureWritable();
        Refresh();

        return WriteCoreAsync(stream, overwrite, cancellationToken);
    }

    /// <summary>
    /// Asynchronously deletes the current file.
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
    /// Core implementation for asynchronously opening the file for reading.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// with a result of type <see cref="Stream"/> for reading the file content.
    /// </returns>
    protected abstract ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Core implementation for asynchronously opening the file for writing.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// with a result of type <see cref="Stream"/> for writing to the file.
    /// </returns>
    protected abstract ValueTask<Stream> OpenWriteCoreAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Core implementation for asynchronously writing the specified content to the file,
    /// creating a new file or overwriting an existing one.
    /// </summary>
    /// <param name="stream">A <see cref="Stream"/> containing the content to write to the file.</param>
    /// <param name="overwrite"><see langword="true"/> to overwrite an existing file; <see langword="false"/> to throw an exception if the file already exists.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>If the file does not exist, it should be created.</description></item>
    ///   <item><description>If the file exists and <paramref name="overwrite"/> is <see langword="true"/>, the existing file should be overwritten.</description></item>
    ///   <item><description>If <paramref name="overwrite"/> is <see langword="false"/> and the file exists, an exception should be thrown.</description></item>
    /// </list>
    /// </remarks>
    protected abstract ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken);

    /// <summary>
    /// Core implementation for asynchronously deleting the current file.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    protected abstract ValueTask DeleteCoreAsync(CancellationToken cancellationToken);
}
