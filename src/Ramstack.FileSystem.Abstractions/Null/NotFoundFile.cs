using System.Diagnostics.CodeAnalysis;

namespace Ramstack.FileSystem.Null;

/// <summary>
/// Represents a non-existing file.
/// </summary>
public sealed class NotFoundFile : VirtualFile
{
    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundFile"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this file.</param>
    /// <param name="path">The path of the file.</param>
    public NotFoundFile(IVirtualFileSystem fileSystem, string path) : base(VirtualPath.Normalize(path)) =>
        FileSystem = fileSystem;

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenReadCoreAsync(CancellationToken cancellationToken)
    {
        Error_FileNotFound(FullName);
        return default;
    }

    /// <inheritdoc />
    protected override ValueTask<Stream> OpenWriteCoreAsync(CancellationToken cancellationToken)
    {
        Error_FileNotFound(FullName);
        return default;
    }

    /// <inheritdoc />
    protected override ValueTask WriteCoreAsync(Stream stream, bool overwrite, CancellationToken cancellationToken)
    {
        Error_FileNotFound(FullName);
        return default;
    }

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken) =>
        default;

    [DoesNotReturn]
    private static void Error_FileNotFound(string path) =>
        throw new FileNotFoundException($"Unable to find file '{path}'.", path);
}
