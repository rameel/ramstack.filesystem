namespace Ramstack.FileSystem.Specification.Tests.Utilities;

public static class VirtualProviderHelpers
{
    public static IAsyncEnumerable<VirtualNode> GetAllFileNodesRecursively(this IVirtualFileSystem fs, string path) =>
        fs.GetDirectory(path).GetAllFileNodesRecursively();

    public static IAsyncEnumerable<VirtualFile> GetAllFilesRecursively(this IVirtualFileSystem fs, string path) =>
        fs.GetDirectory(path).GetAllFilesRecursively();

    public static IAsyncEnumerable<VirtualDirectory> GetAllDirectoriesRecursively(this IVirtualFileSystem fs, string path) =>
        fs.GetDirectory(path).GetAllDirectoriesRecursively();

    public static async IAsyncEnumerable<VirtualNode> GetAllFileNodesRecursively(this VirtualDirectory root)
    {
        var queue = new Queue<VirtualDirectory>();
        queue.Enqueue(root);

        while (queue.TryDequeue(out var directory))
        {
            await foreach (var node in directory.GetFileNodesAsync())
            {
                yield return node;

                if (node is VirtualDirectory d)
                    queue.Enqueue(d);
            }
        }
    }

    public static async IAsyncEnumerable<VirtualFile> GetAllFilesRecursively(this VirtualDirectory root)
    {
        await foreach (var node in GetAllFileNodesRecursively(root))
            if (node is VirtualFile file)
                yield return file;
    }

    public static async IAsyncEnumerable<VirtualDirectory> GetAllDirectoriesRecursively(this VirtualDirectory root)
    {
        await foreach (var node in GetAllFileNodesRecursively(root))
            if (node is VirtualDirectory directory)
                yield return directory;
    }
}
