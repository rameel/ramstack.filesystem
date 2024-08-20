namespace Ramstack.FileSystem;

/// <summary>
/// Provides extension methods for the <see cref="VirtualNode"/> class.
/// </summary>
public static class VirtualNodeExtensions
{
    /// <summary>
    /// Asynchronously retrieves the properties of the specified file or directory.
    /// </summary>
    /// <param name="node">The source file or directory for which to get the <see cref="VirtualNodeProperties"/>.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// with a result of type <see cref="VirtualNodeProperties"/> containing the node properties.
    /// </returns>
    public static ValueTask<VirtualNodeProperties> GetPropertiesAsync(this VirtualNode node, CancellationToken cancellationToken = default) =>
        node.GetPropertiesAsync(refresh: false, cancellationToken);
}
