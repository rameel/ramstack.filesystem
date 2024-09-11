using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Ramstack.FileSystem.Utilities;

namespace Ramstack.FileSystem;

/// <summary>
/// Provides utility methods for working with virtual paths.
/// </summary>
/// <remarks>
/// <para>
///   For compatibility across different implementations of <see cref="IVirtualFileSystem"/>
///   and operating systems, directory separators are unified to use both
///   backslashes and forward slashes ("/" and "\").
///   <strong>This approach will be reviewed once a better solution is found.</strong>
/// </para>
/// <para>
///   When normalizing paths by using the method <see cref="Normalize" />,
///   backslashes ("\") will be replaced with forward slashes ("/") forcibly.
/// </para>
/// </remarks>
public static class VirtualPath
{
    /// <summary>
    /// The threshold size in characters for using stack allocation.
    /// </summary>
    private const int StackallocThreshold = 256;

    /// <summary>
    /// Returns an extension (including the period ".") of the specified path string.
    /// </summary>
    /// <param name="path">The path string from which to get the extension.</param>
    /// <returns>
    /// The extension of the specified path (including the period "."),
    /// or an empty string if no extension is present.
    /// </returns>
    /// <remarks>
    /// <see cref="Path.GetExtension(string)"/> returns an empty string ("")
    /// if the extension consists solely of a period (e.g., "file."), which differs from
    /// <see cref="FileSystemInfo.Extension"/>, which returns "." in this case.
    /// This method follows the behavior of <see cref="Path.GetExtension(string)"/>.
    /// </remarks>
    public static string GetExtension(string path)
    {
        _ = path.Length;
        return GetExtension(path.AsSpan()).ToString();
    }

    /// <summary>
    /// Returns an extension (including the period ".") of the specified path string.
    /// </summary>
    /// <param name="path">The path string from which to get the extension.</param>
    /// <returns>
    /// The extension of the specified path (including the period "."),
    /// or an empty string if no extension is present.
    /// </returns>
    /// <remarks>
    /// <see cref="Path.GetExtension(ReadOnlySpan{char})"/> returns an empty string ("")
    /// if the extension consists solely of a period (e.g., "file."), which differs from
    /// <see cref="FileSystemInfo.Extension"/>, which returns "." in this case.
    /// This method follows the behavior of <see cref="Path.GetExtension(ReadOnlySpan{char})"/>.
    /// </remarks>
    public static ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path)
    {
        for (var i = path.Length - 1; i >= 0; i--)
        {
            if (path[i] == '.')
            {
                if (i == path.Length - 1)
                    break;

                return path.Slice(i);
            }

            if (path[i] == '/' || path[i] == '\\')
                break;
        }

        return default;
    }

    /// <summary>
    /// Returns the file name and extension for the specified path.
    /// </summary>
    /// <param name="path">The path from which to obtain the file name and extension.</param>
    /// <returns>
    /// The file name and extension for the <paramref name="path"/>.
    /// </returns>
    public static string GetFileName(string path)
    {
        var length = path.Length;

        var fileName = GetFileName(path.AsSpan());
        if (fileName.Length != length)
            return fileName.ToString();

        return path;
    }

    /// <summary>
    /// Returns the file name and extension for the specified path.
    /// </summary>
    /// <param name="path">The path from which to obtain the file name and extension.</param>
    /// <returns>
    /// The file name and extension for the <paramref name="path"/>.
    /// </returns>
    public static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path)
    {
        var index = path.LastIndexOfAny('/', '\\');

        return index >= 0
            ? path.Slice(index + 1)
            : path;
    }

    /// <summary>
    /// Returns the directory portion for the specified path.
    /// </summary>
    /// <param name="path">The path to retrieve the directory portion from.</param>
    /// <returns>
    /// Directory portion for <paramref name="path"/>, or an empty string if path denotes a root directory.
    /// </returns>
    public static string GetDirectoryName(string path)
    {
        var offset = GetDirectoryNameOffset(path);

        if (offset < 0)
            return "";

        if (offset == 0)
            return "/";

        return path[..offset];
    }

    /// <summary>
    /// Returns the directory portion for the specified path.
    /// </summary>
    /// <param name="path">The path to retrieve the directory portion from.</param>
    /// <returns>
    /// Directory portion for <paramref name="path"/>, or an empty string if path denotes a root directory.
    /// </returns>
    public static ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path)
    {
        var offset = GetDirectoryNameOffset(path);

        if (offset < 0)
            return "";

        if (offset == 0)
            return "/";

        return path.Slice(0, offset);
    }

    /// <summary>
    /// Determines if the specified path in a normalized form.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns>
    /// <see langword="true" /> if the path in a normalized form;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsNormalized(ReadOnlySpan<char> path)
    {
        if (path is ['/', ..])
        {
            var prior = path[0];

            for (var j = 1; j < path.Length; j++)
            {
                var ch = path[j];
                if (ch == '\\' || ch == '/' && prior == '/')
                    return false;

                if (ch == '.' && prior == '/')
                {
                    if ((uint)j + 1 >= path.Length)
                        return false;

                    var nch = path[j + 1];
                    if (nch == '/' || nch == '\\')
                        return false;

                    if (nch == '.')
                    {
                        if ((uint)j + 2 >= path.Length)
                            return false;

                        var sch = path[j + 2];
                        if (sch == '/' || sch == '\\')
                            return false;
                    }
                }

                prior = ch;
            }

            if (prior != '/' || path.Length == 1)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the absolute path for the specified path string.
    /// </summary>
    /// <param name="path">The file or directory for which to obtain absolute path information.</param>
    /// <returns>
    /// The fully qualified location of <paramref name="path"/>.
    /// </returns>
    public static string Normalize(string path)
    {
        if (IsNormalized(path))
            return path;

        char[]? rented = null;

        var buffer = path.Length + 1 <= StackallocThreshold
            ? stackalloc char[StackallocThreshold]
            : rented = ArrayPool<char>.Shared.Rent(path.Length + 1);

        var index = 0;

        foreach (var s in PathTokenizer.Tokenize(path))
        {
            if (s.Length == 0 || s is ['.'])
                continue;

            if (s is ['.', '.'])
            {
                // Unwind back to the last separator
                index = buffer[..index].LastIndexOf('/');

                // Path.GetFullPath in this case does not throw an exception,
                // it simply clears out the buffer.
                if (index < 0)
                    Error_InvalidPath();
            }
            else
            {
                buffer[index] = '/';
                s.CopyTo(buffer.Slice(index + 1));
                index += s.Length + 1;
            }
        }

        var result = index != 0
            ? buffer[..index].ToString()
            : "/";

        if (rented is not null)
            ArrayPool<char>.Shared.Return(rented);

        return result;
    }

    /// <summary>
    /// Concatenates two paths into a single path.
    /// </summary>
    /// <param name="path1">The path to join.</param>
    /// <param name="path2">The path to join.</param>
    /// <returns>
    /// The concatenated path.
    /// </returns>
    public static string Join(string path1, string path2)
    {
        if (path1.Length == 0)
            return path2;

        if (path2.Length == 0)
            return path1;

        if (HasTrailingSlash(path1) || HasLeadingSlash(path2))
            return string.Concat(path1, path2);

        return string.Concat(path1, "/", path2);
    }

    /// <summary>
    /// Concatenates two paths into a single path.
    /// </summary>
    /// <param name="path1">The path to join.</param>
    /// <param name="path2">The path to join.</param>
    /// <returns>
    /// The concatenated path.
    /// </returns>
    public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
    {
        if (path1.Length == 0)
            return path2.ToString();

        if (path2.Length == 0)
            return path1.ToString();

        if (HasTrailingSlash(path1) || HasLeadingSlash(path2))
            return string.Concat(path1, path2);

        return string.Concat(path1, "/", path2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasLeadingSlash(string path) =>
        path.StartsWith('/') || path.StartsWith('\\');

    /// <summary>
    /// Determines whether the specified path string ends in a directory separator.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns>
    /// <see langword="true" /> if the path has a trailing directory separator;
    /// otherwise, <see langword="false" />.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasTrailingSlash(string path) =>
        path.EndsWith('/') || path.EndsWith('\\');

    /// <summary>
    /// Determines whether the specified path string starts with a directory separator.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns>
    /// <see langword="true" /> if the path has a leading directory separator;
    /// otherwise, <see langword="false" />.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasLeadingSlash(ReadOnlySpan<char> path) =>
        path.Length != 0 && (path[0] == '/' || path[0] == '\\');

    /// <summary>
    /// Determines whether the specified path string ends in a directory separator.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns>
    /// <see langword="true" /> if the path has a trailing directory separator;
    /// otherwise, <see langword="false" />.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasTrailingSlash(ReadOnlySpan<char> path)
    {
        if (path.Length != 0)
        {
            var ch = Unsafe.Add(ref MemoryMarshal.GetReference(path), (nint)(uint)path.Length - 1);
            return ch == '/' || ch == '\\';
        }

        return false;
    }

    private static int GetDirectoryNameOffset(ReadOnlySpan<char> path)
    {
        var lastIndex = path.LastIndexOfAny('/', '\\');
        var index = lastIndex;

        // Process consecutive separators
        while ((uint)index - 1 < (uint)path.Length && (path[index - 1] == '/' || path[index - 1] == '\\'))
            index--;

        // Case where the path consists of separators only
        if (index == 0 && lastIndex + 1 == path.Length)
            index = -1;

        return index;
    }

    [DoesNotReturn]
    private static void Error_InvalidPath() =>
        throw new ArgumentException("Invalid path");
}
