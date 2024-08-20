namespace Ramstack.FileSystem.Internal;

/// <summary>
/// Provides extension methods for the <see cref="IEnumerable{T}"/>.
/// </summary>
internal static class EnumerableExtensions
{
    /// <summary>
    /// Converts an enumerable sequence to an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">Th enumerable sequence to convert to an async-enumerable sequence.</param>
    /// <returns>
    /// The async-enumerable sequence whose elements are pulled from the given enumerable sequence.
    /// </returns>
    public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source) =>
        new AsyncEnumerableAdapter<T>(source);

    #region Inner type: AsyncEnumerableAdapter

    /// <summary>
    /// Wraps the <see cref="IEnumerable{T}"/> to the <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <param name="source">The <see cref="IEnumerable{T}"/> to wrap.</param>
    private sealed class AsyncEnumerableAdapter<T>(IEnumerable<T> source) : IAsyncEnumerable<T>
    {
        /// <inheritdoc cref="IAsyncEnumerable{T}.GetAsyncEnumerator(CancellationToken)" />
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken) =>
            new AsyncEnumeratorAdapter<T>(source.GetEnumerator(), cancellationToken);
    }

    #endregion

    #region Inner type: AsyncEnumeratorAdapter

    /// <summary>
    /// Wraps the <see cref="IEnumerator{T}"/> to the <see cref="IAsyncEnumerator{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <param name="enumerator">The <see cref="IEnumerator{T}"/> to wrap.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    private sealed class AsyncEnumeratorAdapter<T>(IEnumerator<T> enumerator, CancellationToken cancellationToken) : IAsyncEnumerator<T>
    {
        /// <inheritdoc />
        public T Current => enumerator.Current;

        /// <inheritdoc />
        public ValueTask<bool> MoveNextAsync()
        {
            var result = !cancellationToken.IsCancellationRequested && enumerator.MoveNext();
            return new ValueTask<bool>(result);
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            enumerator.Dispose();
            return default;
        }
    }

    #endregion
}
