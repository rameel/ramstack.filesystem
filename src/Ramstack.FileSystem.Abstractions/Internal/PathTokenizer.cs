using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ramstack.FileSystem.Internal;

/// <summary>
/// Tokenizes a file path into its constituent components.
/// </summary>
/// <param name="path">The file path to tokenize.</param>
internal readonly struct PathTokenizer(string path)
{
    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the collection.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() =>
        new(path);

    /// <summary>
    /// Tokenizes the specified path into a collection of the path components.
    /// </summary>
    /// <param name="path">The file path to tokenize.</param>
    /// <returns>
    /// The <see cref="PathTokenizer"/>.
    /// </returns>
    public static PathTokenizer Tokenize(string path) =>
        new(path);

    #region Inner type: Enumerator

    /// <summary>
    /// Represents the <see cref="PathTokenizer"/> enumerator.
    /// </summary>
    public struct Enumerator
    {
        private readonly string _path;
        private nint _start;
        private nint _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> structure.
        /// </summary>
        /// <param name="path">The file path to tokenize.</param>
        public Enumerator(string path) =>
            (_path, _count) = (path, -1);

        /// <summary>
        /// Gets the current path component.
        /// </summary>
        public ReadOnlySpan<char> Current
        {
            get
            {
                Debug.Assert(_path.AsSpan((int)_start, (int)_count).Length >= 0);

                return MemoryMarshal.CreateReadOnlySpan(
                    ref Unsafe.Add(ref Unsafe.AsRef(in _path.GetPinnableReference()), _start),
                    (int)_count);
            }
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

            if ((int)_start < _path.Length)
            {
                Debug.Assert(_path.AsSpan((int)_start, _path.Length - (int)_start).Length >= 0);

                var s = MemoryMarshal.CreateReadOnlySpan(
                    ref Unsafe.Add(ref Unsafe.AsRef(in _path.GetPinnableReference()), _start),
                    _path.Length - (int)_start);

                _count = s.IndexOfAny('/', '\\');
                if (_count < 0)
                    _count = (nint)(uint)s.Length;

                return true;
            }

            return false;
        }
    }

    #endregion
}
