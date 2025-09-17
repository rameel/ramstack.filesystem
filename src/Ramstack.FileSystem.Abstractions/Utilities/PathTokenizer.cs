namespace Ramstack.FileSystem.Utilities;

/// <summary>
/// Tokenizes a file path into its constituent components.
/// </summary>
/// <param name="path">The path of the file to tokenize.</param>
internal readonly struct PathTokenizer(string path)
{
    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// An enumerator to iterate through the collection.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() =>
        new Enumerator(path);

    /// <summary>
    /// Tokenizes the specified path into a collection of the path components.
    /// </summary>
    /// <param name="path">The file path to tokenize.</param>
    /// <returns>
    /// The <see cref="PathTokenizer"/>.
    /// </returns>
    public static PathTokenizer Tokenize(string path) =>
        new PathTokenizer(path);

    #region Inner type: Enumerator

    /// <summary>
    /// Represents the <see cref="PathTokenizer"/> enumerator.
    /// </summary>
    public struct Enumerator
    {
        private readonly string _path;
        private int _start;
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> structure.
        /// </summary>
        /// <param name="path">The file path to tokenize.</param>
        internal Enumerator(string path) =>
            (_path, _count) = (path, -1);

        /// <summary>
        /// Gets the current path component.
        /// </summary>
        public ReadOnlySpan<char> Current
        {
            //
            // Using AsSpan(_start) followed by slicing is more efficient
            // than AsSpan(_start, _count) because:
            // 1) MoveNext already validated _start bounds
            // 2) We only need to check _count <= length (simpler than checking start+count)
            //
            // The alternative AsSpan(start, count) does a combined bounds check
            // which the JIT can't optimize away:
            // (ulong)(uint)_start + (ulong)(uint)_count <= (ulong)(uint)Length
            //
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _path.AsSpan(_start)[.._count];
        }

        /// <summary>
        /// Advances the enumerator to the next path component.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the enumerator was successfully advanced
        /// to the next path component; otherwise, <see langword="false" />.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _start = _start + _count + 1;

            if ((uint)_start < (uint)_path.Length)
            {
                var s = _path.AsSpan(_start);

                _count = s.IndexOfAny('/', '\\');
                if (_count < 0)
                    _count = s.Length;

                return true;
            }

            return false;
        }
    }

    #endregion
}
