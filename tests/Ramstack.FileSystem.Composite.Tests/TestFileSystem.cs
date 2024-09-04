namespace Ramstack.FileSystem.Composite;

internal sealed class TestFileSystem : IVirtualFileSystem
{
    public bool IsReadOnly => throw new NotImplementedException();
    public VirtualFile GetFile(string path) => throw new NotImplementedException();
    public VirtualDirectory GetDirectory(string path) => throw new NotImplementedException();
    public void Dispose() => throw new NotImplementedException();
}
