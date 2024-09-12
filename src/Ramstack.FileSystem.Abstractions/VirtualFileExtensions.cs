using System.Buffers;
using System.Text;

namespace Ramstack.FileSystem;

/// <summary>
/// Provides extension methods for the <see cref="VirtualFile"/> class.
/// </summary>
public static class VirtualFileExtensions
{
    private static Encoding? _utf8NoBom;

    private static Encoding Utf8NoBom => _utf8NoBom
        ??= new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

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
    /// Asynchronously reads all the text in the current file.
    /// </summary>
    /// <param name="file">The file from which to read the entire text content.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// containing the full text from the current file.
    /// </returns>
    public static ValueTask<string> ReadAllTextAsync(this VirtualFile file, CancellationToken cancellationToken = default) =>
        ReadAllTextAsync(file, Encoding.UTF8, cancellationToken);

    /// <summary>
    /// Asynchronously reads all the text in the current file with the specified encoding.
    /// </summary>
    /// <param name="file">The file from which to read the entire text content.</param>
    /// <param name="encoding">The encoding applied to the contents.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// containing the full text from the current file.
    /// </returns>
    public static async ValueTask<string> ReadAllTextAsync(this VirtualFile file, Encoding encoding, CancellationToken cancellationToken = default)
    {
        const int BufferSize = 4096;

        var reader = await file.OpenTextAsync(encoding, cancellationToken).ConfigureAwait(false);
        var buffer = (char[]?)null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            buffer = ArrayPool<char>.Shared.Rent(encoding.GetMaxCharCount(BufferSize));
            var sb = new StringBuilder();

            while (true)
            {
                var count = await reader
                    .ReadAsync(new Memory<char>(buffer), cancellationToken)
                    .ConfigureAwait(false);

                if (count == 0)
                    return sb.ToString();

                sb.Append(buffer.AsSpan(0, count));
            }
        }
        finally
        {
            reader.Dispose();

            if (buffer is not null)
                ArrayPool<char>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Asynchronously reads all lines of the current file.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// containing an array of all lines in the current file.
    /// </returns>
    public static ValueTask<string[]> ReadAllLinesAsync(this VirtualFile file, CancellationToken cancellationToken = default) =>
        ReadAllLinesAsync(file, Encoding.UTF8, cancellationToken);

    /// <summary>
    /// Asynchronously reads all lines of the current file with the specified encoding.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="encoding">The encoding applied to the contents.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// containing an array of all lines in the current file.
    /// </returns>
    public static async ValueTask<string[]> ReadAllLinesAsync(this VirtualFile file, Encoding encoding, CancellationToken cancellationToken = default)
    {
        using var reader = await file.OpenTextAsync(encoding, cancellationToken).ConfigureAwait(false);

        var list = new List<string>();
        while (await reader.ReadLineAsync().ConfigureAwait(false) is {} line)
            list.Add(line);

        return list.ToArray();
    }

    /// <summary>
    /// Asynchronously reads the entire contents of the current file into a byte array.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// containing an array of the file's bytes.
    /// </returns>
    public static async ValueTask<byte[]> ReadAllBytesAsync(this VirtualFile file, CancellationToken cancellationToken = default)
    {
        // ReSharper disable once UseAwaitUsing
        using var stream = await file.OpenReadAsync(cancellationToken).ConfigureAwait(false);

        var length = GetStreamLength(stream);
        if (length > Array.MaxLength)
            throw new IOException("The file is too large.");

        var task = length <= 0
            ? ReadAllBytesUnknownLengthImplAsync(stream, cancellationToken)
            : ReadAllBytesImplAsync(stream, cancellationToken);

        return await task.ConfigureAwait(false);

        static async ValueTask<byte[]> ReadAllBytesImplAsync(Stream stream, CancellationToken cancellationToken)
        {
            var bytes = new byte[stream.Length];
            var index = 0;
            do
            {
                var count = await stream.ReadAsync(bytes.AsMemory(index), cancellationToken).ConfigureAwait(false);
                if (count == 0)
                    Error();

                index += count;
            }
            while (index < bytes.Length);

            return bytes;
        }

        static async ValueTask<byte[]> ReadAllBytesUnknownLengthImplAsync(Stream stream, CancellationToken cancellationToken)
        {
            var bytes = ArrayPool<byte>.Shared.Rent(512);
            var total = 0;

            try
            {
                while (true)
                {
                    if (total == bytes.Length)
                        bytes = ResizeBuffer(bytes);

                    var count = await stream
                        .ReadAsync(bytes.AsMemory(total), cancellationToken)
                        .ConfigureAwait(false);

                    if (count == 0)
                        return bytes.AsSpan(0, total).ToArray();

                    total += count;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }

            static byte[] ResizeBuffer(byte[] bytes)
            {
                var length = (uint)bytes.Length * 2;
                if (length > (uint)Array.MaxLength)
                    length = (uint)Math.Max(Array.MaxLength, bytes.Length + 1);

                var tmp = ArrayPool<byte>.Shared.Rent((int)length);
                Buffer.BlockCopy(bytes, 0, tmp, 0, bytes.Length);

                var rented = bytes;
                bytes = tmp;

                ArrayPool<byte>.Shared.Return(rented);
                return bytes;
            }
        }

        static long GetStreamLength(Stream stream)
        {
            try
            {
                if (stream.CanSeek)
                    return stream.Length;
            }
            catch
            {
                // skip
            }

            return 0;
        }

        static void Error() =>
            throw new EndOfStreamException();
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
    /// Asynchronously writes the specified string to the current file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllTextAsync(this VirtualFile file, string contents, CancellationToken cancellationToken = default) =>
        WriteAllTextAsync(file, contents.AsMemory(), Utf8NoBom, cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified string to the current file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="encoding">The encoding to apply to the string.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllTextAsync(this VirtualFile file, string contents, Encoding encoding, CancellationToken cancellationToken = default) =>
        WriteAllTextAsync(file, contents.AsMemory(), encoding, cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified string to current the file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllTextAsync(this VirtualFile file, ReadOnlyMemory<char> contents, CancellationToken cancellationToken = default) =>
        WriteAllTextAsync(file, contents, Utf8NoBom, cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified string to the current file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="encoding">The encoding to apply to the string.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static async ValueTask WriteAllTextAsync(this VirtualFile file, ReadOnlyMemory<char> contents, Encoding encoding, CancellationToken cancellationToken = default)
    {
        const int ChunkSize = 8192;

        if (contents.IsEmpty)
            return;

        var stream = await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);

        var preamble = encoding.GetPreamble();
        if (preamble.Length != 0)
            stream.Write(preamble);

        var bytes = ArrayPool<byte>.Shared.Rent(
            encoding.GetMaxCharCount(Math.Min(ChunkSize, contents.Length)));

        try
        {
            var encoder = encoding.GetEncoder();
            while (contents.Length != 0)
            {
                var data = contents[..Math.Min(ChunkSize, contents.Length)];
                contents = contents[data.Length..];

                var encoded = encoder.GetBytes(data.Span, bytes.AsSpan(), flush: contents.IsEmpty);
                await stream.WriteAsync(bytes.AsMemory(0, encoded), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    /// <summary>
    /// Asynchronously writes the specified lines to the current file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllLinesAsync(this VirtualFile file, IEnumerable<string> contents, CancellationToken cancellationToken = default) =>
        WriteAllLinesAsync(file, contents, Utf8NoBom, cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified lines to the current file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="encoding">The encoding to apply to the string.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static async ValueTask WriteAllLinesAsync(this VirtualFile file, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default)
    {
        var stream = await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
        await using var writer = new StreamWriter(stream, encoding);

        foreach (var line in contents)
            await writer.WriteLineAsync(line).ConfigureAwait(false);

        await writer.FlushAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes the specified byte array to the current file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="bytes">The bytes to write to the file.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask WriteAllBytesAsync(this VirtualFile file, byte[] bytes, CancellationToken cancellationToken = default) =>
        WriteAllBytesAsync(file, bytes.AsMemory(), cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified byte array to the current file. If the file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="bytes">The bytes to write to the file.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static async ValueTask WriteAllBytesAsync(this VirtualFile file, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        await using var stream = await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously copies the file to the specified destination path.
    /// </summary>
    /// <param name="file">The source <see cref="VirtualFile"/> to copy from.</param>
    /// <param name="destinationPath">The path of the destination file. This cannot be a directory.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public static ValueTask CopyToAsync(this VirtualFile file, string destinationPath, CancellationToken cancellationToken = default) =>
        file.CopyToAsync(destinationPath, overwrite: false, cancellationToken);

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
    public static async ValueTask<long> GetLengthAsync(this VirtualFile file, CancellationToken cancellationToken = default)
    {
        var properties = await file.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
        return properties.Length;
    }
}
