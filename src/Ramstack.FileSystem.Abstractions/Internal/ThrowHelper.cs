using System.Diagnostics.CodeAnalysis;

namespace Ramstack.FileSystem.Internal;

/// <summary>
/// Provides helper methods for throwing exceptions.
/// </summary>
internal static class ThrowHelper
{
    /// <summary>
    /// Throws a <see cref="NotSupportedException"/> with a message indicating that write operations are not supported on a read-only instance.
    /// </summary>
    [DoesNotReturn]
    public static void ChangesNotSupported() =>
        throw new NotSupportedException("Write operations are not supported on a read-only instance.");

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> with a message indicating that the virtual path could not be mapped to a physical path.
    /// This may occur if the parent directory does not exist or is not accessible.
    /// </summary>
    [DoesNotReturn]
    public static void PathMappingFailed()
    {
        const string message = "The virtual path could not be mapped to a physical path. The parent directory may not exist or be accessible.";
        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> with a message indicating an attempt to resolve a path outside the root.
    /// </summary>
    [DoesNotReturn]
    public static void PathOutsideRoot() =>
        throw new ArgumentException("Invalid path: Attempted to resolve a path outside the root.");

    /// <summary>
    /// Throws a <see cref="FileNotFoundException"/> with an optional inner exception,
    /// indicating that the specified file could not be found.
    /// </summary>
    /// <param name="path">The path of the file that could not be found.</param>
    /// <param name="innerException">An optional inner exception that provides more details about the error.</param>
    [DoesNotReturn]
    public static void FileNotFound(string path, Exception? innerException = null) =>
        throw new FileNotFoundException($"Unable to find file '{path}'", innerException);
}
