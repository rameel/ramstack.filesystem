using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Ramstack.FileSystem;

/// <summary>
/// Represents a node in a virtual file system, which could be either a file or a directory.
/// </summary>
public abstract class VirtualNode
{
    private VirtualNodeProperties? _properties;

    /// <summary>
    /// Gets the <see cref="IVirtualFileSystem"/> instance associated with this node.
    /// </summary>
    public abstract IVirtualFileSystem FileSystem { get; }

    /// <summary>
    /// Gets a value indicating whether the file or directory is read-only.
    /// </summary>
    public bool IsReadOnly => FileSystem.IsReadOnly;

    /// <summary>
    /// Gets the full path of the file or directory.
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// Gets the name of the file or directory, excluding the path.
    /// </summary>
    public string Name => VirtualPath.GetFileName(FullName);

    /// <summary>
    /// Gets the file extension, including the leading dot <c>.</c>, or an empty string if there is no extension.
    /// </summary>
    public string Extension => VirtualPath.GetExtension(FullName);

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualNode"/> class.
    /// </summary>
    /// <param name="path">The full path of the file or directory.</param>
    protected VirtualNode(string path)
    {
        Debug.Assert(path == VirtualPath.GetFullPath(path));
        FullName = path;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualNode"/> class with specified properties.
    /// </summary>
    /// <param name="path">The full path of the file or directory.</param>
    /// <param name="properties">The properties of the file or directory.</param>
    protected VirtualNode(string path, VirtualNodeProperties? properties) : this(path) =>
        _properties = properties;

    /// <summary>
    /// Refreshes the state of the file or directory node, clearing any cached or stored values.
    /// </summary>
    public void Refresh()
    {
        _properties = null;
        RefreshCore();
    }

    /// <summary>
    /// Asynchronously determines whether the file or directory exists.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.
    /// The task result is <see langword="true"/> if the file or directory exists; otherwise, <see langword="false"/>.
    /// </returns>
    public ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        if (TryGetProperties(out var properties))
            return new ValueTask<bool>(properties.Exists);

        return ExistsCoreAsync(cancellationToken);
    }

    /// <summary>
    /// Asynchronously retrieves the properties of the file or directory.
    /// </summary>
    /// <param name="refresh">A boolean indicating whether to refresh the state before retrieving the properties.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// with a result of type <see cref="VirtualNodeProperties"/> containing the file or directory properties.
    /// </returns>
    public ValueTask<VirtualNodeProperties> GetPropertiesAsync(bool refresh, CancellationToken cancellationToken = default)
    {
        var properties = _properties;
        return refresh || properties is null
            ? GetPropertiesImpl(cancellationToken)
            : new ValueTask<VirtualNodeProperties>(properties);

        async ValueTask<VirtualNodeProperties> GetPropertiesImpl(CancellationToken token) =>
            _properties = await GetPropertiesCoreAsync(token).ConfigureAwait(false) ?? VirtualNodeProperties.Unavailable;
    }

    /// <summary>
    /// Core implementation for refreshing the state of the file or directory node,
    /// clearing any cached or stored values.
    /// </summary>
    protected virtual void RefreshCore()
    {
    }

    /// <summary>
    /// Core implementation for asynchronously retrieving the current properties of the file or directory.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// with a result of type <see cref="VirtualNodeProperties"/> containing the updated properties,
    /// or <see langword="null"/> if the properties cannot be retrieved.
    /// </returns>
    protected abstract ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Core implementation for asynchronously determining whether the file or directory exists.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.
    /// The task result is <see langword="true"/> if the file or directory exists; otherwise, <see langword="false"/>.
    /// </returns>
    protected virtual async ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken) =>
        (await GetPropertiesAsync(refresh: true, cancellationToken).ConfigureAwait(false)).Exists;

    /// <summary>
    /// Attempts to retrieve the current properties of the file or directory.
    /// </summary>
    /// <param name="properties">When this method returns, contains the current <see cref="VirtualNodeProperties"/> if available;
    /// otherwise, <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the properties were successfully retrieved;
    /// otherwise, <see langword="false"/> if the properties have not been set or have been invalidated.
    /// </returns>
    protected bool TryGetProperties([NotNullWhen(true)] out VirtualNodeProperties? properties)
    {
        properties = _properties;
        return properties is not null;
    }

    /// <summary>
    /// Ensures that the current file or directory is writable.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when the current file or directory is read-only
    /// and modifications are not supported.
    /// </exception>
    protected void EnsureWritable()
    {
        if (FileSystem.IsReadOnly)
            throw new NotSupportedException("Write operations are not supported on a read-only instance.");
    }
}
