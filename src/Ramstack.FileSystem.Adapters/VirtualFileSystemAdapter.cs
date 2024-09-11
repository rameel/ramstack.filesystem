using Microsoft.Extensions.FileProviders;

namespace Ramstack.FileSystem.Adapters;

/// <summary>
/// Represents an implementation of <see cref="IVirtualFileSystem"/> that enables working with virtual files
/// using the underlying <see cref="IFileProvider"/> instance.
/// </summary>
public sealed class VirtualFileSystemAdapter : IVirtualFileSystem
{
    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <summary>
    /// Gets the underlying <see cref="IFileProvider"/> instance.
    /// </summary>
    public IFileProvider FileProvider { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualFileSystemAdapter"/> class.
    /// </summary>
    /// <param name="provider">The <see cref="IFileProvider"/> instance to wrap.</param>
    public VirtualFileSystemAdapter(IFileProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        FileProvider = provider;
    }

    /// <inheritdoc />
    public VirtualDirectory GetDirectory(string path)
    {
        path = VirtualPath.Normalize(path);
        var directory = FileProvider.GetDirectoryContents(path);

        return new VirtualDirectoryAdapter(this, path, directory);
    }

    /// <inheritdoc />
    public VirtualFile GetFile(string path)
    {
        path = VirtualPath.Normalize(path);
        var file = FileProvider.GetFileInfo(path);

        return new VirtualFileAdapter(this, path, file);
    }

    /// <inheritdoc />
    public void Dispose() =>
        // ReSharper disable once SuspiciousTypeConversion.Global
        (FileProvider as IDisposable)?.Dispose();
}
