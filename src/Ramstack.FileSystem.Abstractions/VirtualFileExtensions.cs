using System.Text;

namespace Ramstack.FileSystem;

/// <summary>
/// Provides extension methods for the <see cref="VirtualFile"/> class.
/// </summary>
public static class VirtualFileExtensions
{
    /// <summary>
    /// Asynchronously returns a <see cref="StreamReader"/> with <see cref="Encoding.UTF8"/>
    /// character encoding that reads from the specified text file.
    /// </summary>
    /// <param name="file">The file to get the <see cref="StreamReader"/> for.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.
    /// The result is a <see cref="StreamReader"/> that reads from the text file.
    /// </returns>
    public static ValueTask<StreamReader> OpenTextAsync(this VirtualFile file, CancellationToken cancellationToken = default) =>
        file.OpenTextAsync(Encoding.UTF8, cancellationToken);

    /// <summary>
    /// Asynchronously returns a <see cref="StreamReader"/> with the specified character encoding
    /// that reads from the specified text file.
    /// </summary>
    /// <param name="file">The file to get the <see cref="StreamReader"/> for.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.
    /// The result is a <see cref="StreamReader"/> that reads from the text file.
    /// </returns>
    public static async ValueTask<StreamReader> OpenTextAsync(this VirtualFile file, Encoding encoding, CancellationToken cancellationToken = default)
    {
        var stream = await file.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        return new StreamReader(stream, encoding);
    }

    /// <summary>
    /// Asynchronously writes the specified content to a file. If the file exists, an exception will be thrown.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="stream">A <see cref="Stream"/> containing the content to write to the file.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAsync(this VirtualFile file, Stream stream, CancellationToken cancellationToken = default) =>
        file.WriteAsync(stream, overwrite: false, cancellationToken);

    /// <summary>
    /// Asynchronously copies the contents of the current <see cref="VirtualFile"/> to the specified destination <see cref="VirtualFile"/>.
    /// </summary>
    /// <param name="file">The source <see cref="VirtualFile"/> to copy from.</param>
    /// <param name="destination">The destination <see cref="VirtualFile"/> where the contents will be copied to.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation. Defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> that represents the asynchronous copy operation.
    /// </returns>
    public static ValueTask CopyToAsync(this VirtualFile file, VirtualFile destination, CancellationToken cancellationToken = default) =>
        file.CopyToAsync(destination, overwrite: false, cancellationToken);

    /// <summary>
    /// Asynchronously returns the size of the specified file in bytes.
    /// </summary>
    /// <param name="file">The file to get the size of.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.
    /// The result is the size of the specified file in bytes, or <c>-1</c> if the file does not exist.
    /// </returns>
    public static async ValueTask<long> GetLengthAsync(this VirtualFile file, CancellationToken cancellationToken = default) =>
        (await file.GetPropertiesAsync(cancellationToken).ConfigureAwait(false)).Length;
}
