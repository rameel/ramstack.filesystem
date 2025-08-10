using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

using Ramstack.Globbing;

namespace Ramstack.FileSystem.Globbing.Internal;

/// <summary>
/// Provides helper methods for path manipulations.
/// </summary>
internal static class PathHelper
{
    /// <summary>
    /// Determines whether the specified path matches any of the specified patterns.
    /// </summary>
    /// <param name="path">The path to match for a match.</param>
    /// <param name="patterns">An array of patterns to match against the path.</param>
    /// <returns>
    /// <see langword="true" /> if the path matches any of the patterns;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsMatch(scoped ReadOnlySpan<char> path, string[] patterns)
    {
        foreach (var pattern in patterns)
            if (Matcher.IsMatch(path, pattern, MatchFlags.Unix))
                return true;

        return false;
    }

    /// <summary>
    /// Determines whether the specified path partially matches any of the specified patterns.
    /// </summary>
    /// <param name="path">The path to be partially matched.</param>
    /// <param name="patterns">An array of patterns to match against the path.</param>
    /// <returns>
    /// <see langword="true" /> if the path partially matches any of the patterns;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsPartialMatch(scoped ReadOnlySpan<char> path, string[] patterns)
    {
        Debug.Assert(path is not "/");

        var count = CountPathSegments(path);

        foreach (var pattern in patterns)
            if (Matcher.IsMatch(path, GetPartialPattern(pattern, count), MatchFlags.Unix))
                return true;

        return false;
    }

    /// <summary>
    /// Counts the number of segments in the specified path.
    /// </summary>
    /// <param name="path">The path to count segments for.</param>
    /// <returns>
    /// The number of segments in the path.
    /// </returns>
    public static int CountPathSegments(scoped ReadOnlySpan<char> path)
    {
        var count = 0;
        var iterator = new PathSegmentIterator();
        ref var s = ref Unsafe.AsRef(in MemoryMarshal.GetReference(path));
        var length = path.Length;

        while (true)
        {
            var r = iterator.GetNext(ref s, length);

            if (r.start != r.final)
                count++;

            if (r.final == length)
                break;
        }

        if (count == 0)
            count = 1;

        return count;
    }

    /// <summary>
    /// Returns a partial pattern from the specified pattern string based on the specified depth.
    /// </summary>
    /// <param name="pattern">The pattern string to extract from.</param>
    /// <param name="depth">The depth level to extract the partial pattern up to.</param>
    /// <returns>
    /// A <see cref="ReadOnlySpan{T}"/> representing the partial pattern.
    /// </returns>
    public static ReadOnlySpan<char> GetPartialPattern(string pattern, int depth)
    {
        Debug.Assert(depth >= 1);

        var iterator = new PathSegmentIterator();
        ref var s = ref Unsafe.AsRef(in pattern.GetPinnableReference());
        var length = pattern.Length;

        while (true)
        {
            var r = iterator.GetNext(ref s, length);
            if (r.start != r.final)
                depth--;

            if (depth < 1
                || r.final == length
                || IsGlobStar(ref s, r.start, r.final))
                return MemoryMarshal.CreateReadOnlySpan(ref s, r.final);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsGlobStar(ref char s, int index, int final) =>
            index + 2 == final && Unsafe.ReadUnaligned<int>(
                ref Unsafe.As<char, byte>(
                    ref Unsafe.Add(ref s, (nint)(uint)index))) == ('*' << 16 | '*');
    }

    #region Vector helper methods

    /// <summary>
    /// Loads a 256-bit vector from the specified source.
    /// </summary>
    /// <param name="source">The source from which the vector will be loaded.</param>
    /// <param name="offset">The offset from the <paramref name="source"/> from which the vector will be loaded.</param>
    /// <returns>
    /// The loaded 256-bit vector.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<ushort> LoadVector256(ref char source, nint offset) =>
        Unsafe.ReadUnaligned<Vector256<ushort>>(
            ref Unsafe.As<char, byte>(ref Unsafe.Add(ref source, offset)));

    /// <summary>
    /// Loads a 128-bit vector from the specified source.
    /// </summary>
    /// <param name="source">The source from which the vector will be loaded.</param>
    /// <param name="offset">The offset from <paramref name="source"/> from which the vector will be loaded.</param>
    /// <returns>
    /// The loaded 128-bit vector.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<ushort> LoadVector128(ref char source, nint offset) =>
        Unsafe.ReadUnaligned<Vector128<ushort>>(
            ref Unsafe.As<char, byte>(
                ref Unsafe.Add(ref source, offset)));

    #endregion

    #region Inner type: PathSegmentIterator

    /// <summary>
    /// Provides functionality to iterate over segments of a path.
    /// </summary>
    private struct PathSegmentIterator
    {
        private int _last;
        private nint _position;
        private uint _mask;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathSegmentIterator"/> structure.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PathSegmentIterator() =>
            _last = -1;

        /// <summary>
        /// Retrieves the next segment of the path.
        /// </summary>
        /// <param name="source">A reference to the starting character of the path.</param>
        /// <param name="length">The total number of characters in the input path starting from <paramref name="source"/>.</param>
        /// <returns>
        /// A tuple containing the start and end indices of the next path segment.
        /// <c>start</c> indicates the beginning of the segment, and <c>final</c> satisfies
        /// the condition that <c>final - start</c> equals the length of the segment.
        /// The end of the iteration is indicated by <c>final</c> being equal to the length of the path.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int start, int final) GetNext(ref char source, int length)
        {
            var start = _last + 1;

            while ((int)_position < length)
            {
                if ((Avx2.IsSupported || Sse2.IsSupported || AdvSimd.Arm64.IsSupported) && _mask != 0)
                {
                    var offset = BitOperations.TrailingZeroCount(_mask);
                    if (AdvSimd.IsSupported)
                    {
                        //
                        // On ARM, ExtractMostSignificantBits returns a mask where each bit
                        // represents one vector element (1 bit per ushort), so offset
                        // directly corresponds to the element index
                        //
                        _last = (int)(_position + (nint)(uint)offset);

                        //
                        // Clear the bits for the current separator
                        //
                        _mask &= ~(1u << offset);
                    }
                    else
                    {
                        //
                        // On x86, MoveMask (and ExtractMostSignificantBits on byte-based vectors)
                        // returns a mask where each bit represents one byte (2 bits per ushort),
                        // so we need to divide offset by 2 to get the actual element index
                        //
                        _last = (int)(_position + (nint)((uint)offset >> 1));

                        //
                        // Clear the bits for the current separator
                        //
                        _mask &= ~(0b_11u << offset);
                    }

                    //
                    // Advance position to the next chunk when no separators remain in the mask
                    //
                    if (_mask == 0)
                    {
                        //
                        // https://github.com/dotnet/runtime/issues/117416
                        //
                        // Precompute the stride size instead of calculating it inline
                        // to avoid stack spilling. For some unknown reason, the JIT
                        // fails to optimize properly when this is written inline, like so:
                        // _position += Avx2.IsSupported
                        //     ? Vector256<ushort>.Count
                        //     : Vector128<ushort>.Count;
                        //

                        var stride = Avx2.IsSupported
                            ? Vector256<ushort>.Count
                            : Vector128<ushort>.Count;

                        _position += stride;
                    }

                    return (start, _last);
                }

                if (Avx2.IsSupported && (int)_position + Vector256<ushort>.Count <= length)
                {
                    var chunk = LoadVector256(ref source, _position);
                    var slash = Vector256.Create('/');
                    var comparison = Avx2.CompareEqual(chunk, slash);

                    //
                    // Store the comparison bitmask and reuse it across iterations
                    // as long as it contains non-zero bits.
                    // This avoids reloading SIMD registers and repeating comparisons
                    // on the same chunk of data.
                    //
                    _mask = (uint)Avx2.MoveMask(comparison.AsByte());

                    //
                    // Advance position to the next chunk when no separators found
                    //
                    if (_mask == 0)
                        _position += Vector256<ushort>.Count;
                }
                else if (Sse2.IsSupported && !Avx2.IsSupported && (int)_position + Vector128<ushort>.Count <= length)
                {
                    var chunk = LoadVector128(ref source, _position);
                    var slash = Vector128.Create('/');
                    var comparison = Sse2.CompareEqual(chunk, slash);

                    //
                    // Store the comparison bitmask and reuse it across iterations
                    // as long as it contains non-zero bits.
                    // This avoids reloading SIMD registers and repeating comparisons
                    // on the same chunk of data.
                    //
                    _mask = (uint)Sse2.MoveMask(comparison.AsByte());

                    //
                    // Advance position to the next chunk when no separators found
                    //
                    if (_mask == 0)
                        _position += Vector128<ushort>.Count;
                }
                else if (AdvSimd.Arm64.IsSupported && (int)_position + Vector128<ushort>.Count <= length)
                {
                    var chunk = LoadVector128(ref source, _position);
                    var slash = Vector128.Create('/');
                    var comparison = AdvSimd.CompareEqual(chunk, slash);

                    //
                    // Store the comparison bitmask and reuse it across iterations
                    // as long as it contains non-zero bits.
                    // This avoids reloading SIMD registers and repeating comparisons
                    // on the same chunk of data.
                    //
                    _mask = ExtractMostSignificantBits(comparison);

                    //
                    // Advance position to the next chunk when no separators found
                    //
                    if (_mask == 0)
                        _position += Vector128<ushort>.Count;

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    static uint ExtractMostSignificantBits(Vector128<ushort> v)
                    {
                        var sum = AdvSimd.Arm64.AddAcross(
                            AdvSimd.ShiftLogical(
                                AdvSimd.And(v, Vector128.Create((ushort)0x8000)),
                                Vector128.Create(-15, -14, -13, -12, -11, -10, -9, -8)));
                        return sum.ToScalar();
                    }
                }
                else
                {
                    for (; (int)_position < length; _position++)
                    {
                        var ch = Unsafe.Add(ref source, _position);
                        if (ch == '/')
                        {
                            _last = (int)_position;
                            _position++;

                            return (start, _last);
                        }
                    }
                }
            }

            return (start, length);
        }
    }

    #endregion
}
