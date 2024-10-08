using System.Text;

namespace Ramstack.FileSystem;

/// <summary>
/// Provides extension methods for the <see cref="IVirtualFileSystem"/> interface.
/// </summary>
public static partial class VirtualFileSystemExtensions
{
    /// <summary>
    /// Asynchronously opens the file at the specified path for reading.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the file to open.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation, containing a stream for reading the file content.
    /// </returns>
    public static ValueTask<Stream> OpenReadAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).OpenReadAsync(cancellationToken);

    /// <summary>
    /// Asynchronously returns a <see cref="StreamReader"/> with <see cref="Encoding.UTF8"/> encoding for a file at the specified path.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the file to open.</param>
    /// <param name="cancellationToken">The optional cancellation token used for canceling the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation and returns a <see cref="StreamReader"/> that reads from the text file.
    /// </returns>
    public static Task<StreamReader> OpenTextAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.OpenTextAsync(path, Encoding.UTF8, cancellationToken);

    /// <summary>
    /// Asynchronously returns a <see cref="StreamReader"/> with the specified character encoding for a file at the specified path.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the file to open.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <param name="cancellationToken">The optional cancellation token used for canceling the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation and returns a <see cref="StreamReader"/> that reads from the text file.
    /// </returns>
    public static async Task<StreamReader> OpenTextAsync(this IVirtualFileSystem fs, string path, Encoding encoding, CancellationToken cancellationToken = default)
    {
        var stream = await fs.OpenReadAsync(path, cancellationToken).ConfigureAwait(false);
        return new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: -1, leaveOpen: false);
    }

    /// <summary>
    /// Asynchronously opens the file at the specified path for writing.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the file to open or create.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation, containing a stream for writing to the file content.
    /// </returns>
    public static ValueTask<Stream> OpenWriteAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).OpenWriteAsync(cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified content to a file at the specified path. If the file exists, an exception will be thrown.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="stream">A <see cref="Stream"/> containing the content to write to the file.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAsync(this IVirtualFileSystem fs, string path, Stream stream, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).WriteAsync(stream, overwrite: false, cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified content to a file at the specified path, creating a new file or overwriting an existing one.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="stream">A <see cref="Stream"/> containing the content to write to the file.</param>
    /// <param name="overwrite"><see langword="true"/> to overwrite an existing file;
    /// <see langword="false"/> to throw an exception if the file already exists.</param>
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
    public static ValueTask WriteAsync(this IVirtualFileSystem fs, string path, Stream stream, bool overwrite, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).WriteAsync(stream, overwrite, cancellationToken);

    /// <summary>
    /// Asynchronously reads all the text in the file with the specified encoding.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file from which to read the entire text content.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// containing the full text from the current file.
    /// </returns>
    public static ValueTask<string> ReadAllTextAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).ReadAllTextAsync(cancellationToken);

    /// <summary>
    /// Asynchronously reads all the text in the file with the specified encoding.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file from which to read the entire text content.</param>
    /// <param name="encoding">The encoding applied to the contents.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// containing the full text from the current file.
    /// </returns>
    public static ValueTask<string> ReadAllTextAsync(this IVirtualFileSystem fs, string path, Encoding? encoding, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).ReadAllTextAsync(encoding, cancellationToken);

    /// <summary>
    /// Asynchronously reads all lines of the file with the specified encoding.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file to read from.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// containing an array of all lines in the current file.
    /// </returns>
    public static ValueTask<string[]> ReadAllLinesAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).ReadAllLinesAsync(cancellationToken);

    /// <summary>
    /// Asynchronously reads all lines of the file with the specified encoding.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file to read from.</param>
    /// <param name="encoding">The encoding applied to the contents.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// containing an array of all lines in the current file.
    /// </returns>
    public static ValueTask<string[]> ReadAllLinesAsync(this IVirtualFileSystem fs, string path, Encoding? encoding, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).ReadAllLinesAsync(encoding, cancellationToken);

    /// <summary>
    /// Asynchronously reads the entire contents of the specified file into a byte array.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file to read from.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// containing an array of the file's bytes.
    /// </returns>
    public static ValueTask<byte[]> ReadAllBytesAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).ReadAllBytesAsync(cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified string to the specified file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllTextAsync(this IVirtualFileSystem fs, string path, string contents, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).WriteAllTextAsync(contents, cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified string to the specified file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="encoding">The encoding to apply to the string.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllTextAsync(this IVirtualFileSystem fs, string path, string contents, Encoding? encoding, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).WriteAllTextAsync(contents, encoding, cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified string to specified the file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllTextAsync(this IVirtualFileSystem fs, string path, ReadOnlyMemory<char> contents, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).WriteAllTextAsync(contents, cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified string to the specified file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="encoding">The encoding to apply to the string.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllTextAsync(this IVirtualFileSystem fs, string path, ReadOnlyMemory<char> contents, Encoding? encoding, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).WriteAllTextAsync(contents, encoding, cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified lines to the specified file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllLinesAsync(this IVirtualFileSystem fs, string path, IEnumerable<string> contents, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).WriteAllLinesAsync(contents, cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified lines to the specified file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="encoding">The encoding to apply to the string.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllLinesAsync(this IVirtualFileSystem fs, string path, IEnumerable<string> contents, Encoding? encoding, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).WriteAllLinesAsync(contents, encoding, cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified byte array to the specified file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="bytes">The bytes to write to the file.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllBytesAsync(this IVirtualFileSystem fs, string path, byte[] bytes, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).WriteAllBytesAsync(bytes, cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified byte array to the specified file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="bytes">The bytes to write to the file.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllBytesAsync(this IVirtualFileSystem fs, string path, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).WriteAllBytesAsync(bytes, cancellationToken);

    /// <summary>
    /// Asynchronously determines whether the file exists.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.
    /// The task result is <see langword="true"/> if the file exists; otherwise, <see langword="false"/>.
    /// </returns>
    public static ValueTask<bool> FileExistsAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).ExistsAsync(cancellationToken);

    /// <summary>
    /// Asynchronously deletes the file at the specified path. No exception is thrown if the file does not exist.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the file to delete.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask DeleteFileAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).DeleteAsync(cancellationToken);

    /// <summary>
    /// Asynchronously copies a file within the file system to the specified destination path.
    /// </summary>
    /// <param name="fs">The <see cref="IVirtualFileSystem"/> instance.</param>
    /// <param name="path">The path of the file to copy.</param>
    /// <param name="destinationPath">The path where the file will be copied to.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation. Defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> that represents the asynchronous copy operation.
    /// </returns>
    public static ValueTask CopyFileAsync(this IVirtualFileSystem fs, string path, string destinationPath, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).CopyToAsync(destinationPath, overwrite: false, cancellationToken);

    /// <summary>
    /// Asynchronously copies a file within the file system to the specified destination path.
    /// </summary>
    /// <param name="fs">The <see cref="IVirtualFileSystem"/> instance.</param>
    /// <param name="path">The path of the file to copy.</param>
    /// <param name="destinationPath">The path where the file will be copied to.</param>
    /// <param name="overwrite"><see langword="true"/> to overwrite an existing file; <see langword="false"/> to throw an exception if the file already exists.</param>
    /// <param name="cancellationToken">A token to cancel the operation. Defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> that represents the asynchronous copy operation.
    /// </returns>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>If the file does not exist, it will be created.</description></item>
    ///   <item><description>If it exists and <paramref name="overwrite"/> is <see langword="true"/>, the existing file will be overwritten.</description></item>
    ///   <item><description>If <paramref name="overwrite"/> is <see langword="false"/> and the file exists, an exception will be thrown.</description></item>
    /// </list>
    /// </remarks>
    public static ValueTask CopyFileAsync(this IVirtualFileSystem fs, string path, string destinationPath, bool overwrite, CancellationToken cancellationToken = default) =>
        fs.GetFile(path).CopyToAsync(destinationPath, overwrite, cancellationToken);

    /// <summary>
    /// Asynchronously determines whether the directory exists.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the directory.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.
    /// The task result is <see langword="true"/> if the directory exists; otherwise, <see langword="false"/>.
    /// </returns>
    public static ValueTask<bool> DirectoryExistsAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).ExistsAsync(cancellationToken);

    /// <summary>
    /// Asynchronously creates a directory at the specified path.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the directory to create.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask CreateDirectoryAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).CreateAsync(cancellationToken);

    /// <summary>
    /// Asynchronously deletes a directory at the specified path, including its subdirectories and all files.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the directory to delete.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask DeleteDirectoryAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).DeleteAsync(cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of file nodes (both directories and files)
    /// within the directory at the specified path.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the directory.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualNode"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualNode> GetFileNodesAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetFileNodesAsync(cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of files within the directory at the specified path.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the directory.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualFile"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualFile> GetFilesAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetFilesAsync(cancellationToken);

    /// <summary>
    /// Asynchronously returns an async-enumerable collection of directories within the directory at the specified path.
    /// </summary>
    /// <param name="fs">The file system to use.</param>
    /// <param name="path">The path of the directory.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// An async-enumerable collection of <see cref="VirtualDirectory"/> instances.
    /// </returns>
    public static IAsyncEnumerable<VirtualDirectory> GetDirectoriesAsync(this IVirtualFileSystem fs, string path, CancellationToken cancellationToken = default) =>
        fs.GetDirectory(path).GetDirectoriesAsync(cancellationToken);
}
