using Ramstack.FileSystem.Null;

namespace Ramstack.FileSystem.Composite;

partial class CompositeFileSystem
{
    /// <summary>
    /// Tries to flatten the specified <see cref="IVirtualFileSystem"/> into a flat list of the <see cref="IVirtualFileSystem"/>.
    /// </summary>
    /// <remarks>
    /// If the <paramref name="fileSystem"/> is not a <see cref="CompositeFileSystem"/>,
    /// the same instance of the <paramref name="fileSystem"/> is returned.
    /// </remarks>
    /// <param name="fileSystem">The <see cref="IVirtualFileSystem"/> to flatten.</param>
    /// <returns>
    /// A <see cref="IVirtualFileSystem"/> that represents the flattened version of the specified <see cref="IVirtualFileSystem"/>.
    /// </returns>
    public static IVirtualFileSystem Flatten(IVirtualFileSystem fileSystem)
    {
        if (fileSystem is CompositeFileSystem composite)
            foreach (var fs in composite.InternalFileSystems)
                if (fs is CompositeFileSystem)
                    return Create(composite.InternalFileSystems);

        return fileSystem;
    }

    /// <summary>
    /// Creates an instance of the <see cref="IVirtualFileSystem"/> from the specified list and flattens it into a flat list of file systems.
    /// </summary>
    /// <remarks>
    /// This method returns a <see cref="CompositeFileSystem"/> if more than one file system remains after flattening.
    /// </remarks>
    /// <param name="list">The list of <see cref="IVirtualFileSystem"/> instances to compose and flatten.</param>
    /// <returns>
    /// A <see cref="IVirtualFileSystem"/> that represents the flattened version of the specified list of the <see cref="IVirtualFileSystem"/>.
    /// </returns>
    public static IVirtualFileSystem Create(params IVirtualFileSystem[] list) =>
        list.Length != 1
            ? Create(list.AsEnumerable())
            : Flatten(list[0]);

    /// <summary>
    /// Creates an instance of the <see cref="IVirtualFileSystem"/> from the specified list and flattens it into a flat list of file systems.
    /// </summary>
    /// <remarks>
    /// This method returns a <see cref="CompositeFileSystem"/> if more than one file system remains after flattening.
    /// </remarks>
    /// <param name="list">The list of <see cref="IVirtualFileSystem"/> instances to compose and flatten.</param>
    /// <returns>
    /// A <see cref="IVirtualFileSystem"/> that represents the flattened version of the specified list of the <see cref="IVirtualFileSystem"/>.
    /// </returns>
    public static IVirtualFileSystem Create(IEnumerable<IVirtualFileSystem> list)
    {
        var queue = new Queue<IVirtualFileSystem>();
        var collection = new List<IVirtualFileSystem>();

        foreach (var fs in list)
        {
            queue.Enqueue(fs);

            while (queue.TryDequeue(out var current))
            {
                if (current is CompositeFileSystem composite)
                {
                    foreach (var v in composite.InternalFileSystems)
                        queue.Enqueue(v);
                }
                else if (current is not NullFileSystem)
                {
                    collection.Add(current);
                }
            }
        }

        return collection.Count switch
        {
            0 => new NullFileSystem(),
            1 => collection[0],
            _ => new CompositeFileSystem(collection.ToArray())
        };
    }
}
