using System.Diagnostics;

using Ramstack.FileSystem.Utilities;

namespace Ramstack.FileSystem.Zip;

/// <summary>
/// Represents directory contents and file information within a ZIP archive for the specified path.
/// </summary>
[DebuggerTypeProxy(typeof(ZipDirectoryDebuggerProxy))]
internal sealed class ZipDirectory : VirtualDirectory
{
    private readonly ZipFileSystem _fileSystem;
    private readonly List<VirtualNode> _nodes = [];

    /// <inheritdoc />
    public override IVirtualFileSystem FileSystem => _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZipDirectory"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this directory.</param>
    /// <param name="path">The path of directory.</param>
    public ZipDirectory(ZipFileSystem fileSystem, string path) : base(path) =>
        _fileSystem = fileSystem;

    /// <inheritdoc />
    protected override ValueTask<VirtualNodeProperties?> GetPropertiesCoreAsync(CancellationToken cancellationToken)
    {
        var properties = VirtualNodeProperties.CreateDirectoryProperties(default, default, default);
        return new ValueTask<VirtualNodeProperties?>(properties);
    }

    /// <inheritdoc />
    protected override ValueTask<bool> ExistsCoreAsync(CancellationToken cancellationToken) =>
        new ValueTask<bool>(true);

    /// <inheritdoc />
    protected override ValueTask CreateCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override ValueTask DeleteCoreAsync(CancellationToken cancellationToken) =>
        default;

    /// <inheritdoc />
    protected override IAsyncEnumerable<VirtualNode> GetFileNodesCoreAsync(CancellationToken cancellationToken) =>
        _nodes.ToAsyncEnumerable();

    /// <summary>
    /// Register a file node associated with this directory.
    /// </summary>
    /// <param name="node">The file node associated with this directory.</param>
    internal void RegisterNode(VirtualNode node) =>
        _nodes.Add(node);

    #region Inner type: ZipDirectoryDebuggerProxy

    /// <summary>
    /// Represents a debugger proxy for viewing the contents of a <see cref="ZipDirectory"/> instance.
    /// </summary>
    /// <param name="directory">The <see cref="ZipDirectory"/> instance to provide debugging information for.</param>
    private sealed class ZipDirectoryDebuggerProxy(ZipDirectory directory)
    {
        /// <summary>
        /// Gets an array of <see cref="VirtualNode"/> instances representing
        /// the files within the associated <see cref="ZipDirectory"/>.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public VirtualNode[] Nodes { get; } = directory._nodes.ToArray();
    }

    #endregion
}
