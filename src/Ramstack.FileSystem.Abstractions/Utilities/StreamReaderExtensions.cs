#if NET6_0
using System.Buffers;
using System.Text;

// ReSharper disable once CheckNamespace
namespace System.IO;
/// <summary>
/// Provides extension methods for the <see cref="StreamReader"/> to offer API compatibility with newer .NET versions.
/// </summary>
internal static class StreamReaderExtensions
{
    /// <summary>
    /// Reads a line of characters asynchronously from the current stream and returns the data as a string.
    /// </summary>
    /// <param name="reader">The <see cref="StreamReader"/> to read from.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous read operation.
    /// The value of the task contains the next line from the stream, or is <see langword="null"/>
    /// if the end of the stream is reached.
    /// </returns>
    public static ValueTask<string?> ReadLineAsync(this StreamReader reader, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<string?>(cancellationToken);

        return new ValueTask<string?>(reader.ReadLineAsync());
    }

    /// <summary>
    /// Reads all characters from the current position to the end of the stream asynchronously and returns them as one string.
    /// </summary>
    /// <param name="reader">The <see cref="StreamReader"/> to read from.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous read operation.
    /// The value of the task contains a string with the characters from the current position to the end of the stream.
    /// </returns>
    public static async Task<string> ReadToEndAsync(this StreamReader reader, CancellationToken cancellationToken = default)
    {
        const int BufferSize = 4096;

        var chars = ArrayPool<char>.Shared.Rent(BufferSize);
        var sb = new StringBuilder();

        while (true)
        {
            var count = await reader
                .ReadAsync(new Memory<char>(chars), cancellationToken)
                .ConfigureAwait(false);

            if (count == 0)
                break;

            sb.Append(chars.AsSpan(0, count));
        }

        ArrayPool<char>.Shared.Return(chars);
        return sb.ToString();
    }
}
#endif
