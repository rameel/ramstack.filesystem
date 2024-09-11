namespace Ramstack.FileSystem;

[TestFixture]
public class VirtualPathTests
{
    [TestCase("", "")]
    [TestCase(".", "")]
    [TestCase("/", "")]
    [TestCase("/.", "")]
    [TestCase("file.txt", ".txt")]
    [TestCase("/path/to/file.txt", ".txt")]
    [TestCase("/path/to/.hidden", ".hidden")]
    [TestCase("/path/to/file", "")]
    [TestCase("/path.with.dots/to/file.txt", ".txt")]
    [TestCase("/path/with.dots/file.", "")]
    [TestCase("/path.with.dots/to/.hidden.ext", ".ext")]
    [TestCase("file.with.multiple.dots.ext", ".ext")]
    [TestCase("/path/to/file.with.multiple.dots.ext", ".ext")]
    [TestCase("/.hidden", ".hidden")]
    public void GetExtension(string path, string expected)
    {
        foreach (var p in GetPathVariations(path))
        {
            Assert.That(VirtualPath.GetExtension(p), Is.EqualTo(expected));
            Assert.That(VirtualPath.GetExtension(p.AsSpan()).ToString(), Is.EqualTo(expected));
        }
    }

    [TestCase("", "")]
    [TestCase(".", ".")]
    [TestCase(".hidden", ".hidden")]
    [TestCase("file.txt", "file.txt")]
    [TestCase("/path/to/file.txt", "file.txt")]
    [TestCase("/path/to/.hidden", ".hidden")]
    [TestCase("/path/to/file", "file")]
    [TestCase("/path/with.dots/file.txt", "file.txt")]
    [TestCase("/path/with.dots/file.", "file.")]
    [TestCase("/path/to/file.with.multiple.dots.ext", "file.with.multiple.dots.ext")]
    [TestCase("/path/to/.hidden.ext", ".hidden.ext")]
    [TestCase("/.hidden", ".hidden")]
    [TestCase("/path/to/", "")]
    [TestCase("/path/to/directory/", "")]
    public void GetFileName(string path, string expected)
    {
        foreach (var p in GetPathVariations(path))
        {
            Assert.That(VirtualPath.GetFileName(p), Is.EqualTo(expected));
            Assert.That(VirtualPath.GetFileName(p.AsSpan()).ToString(), Is.EqualTo(expected));
        }
    }

    [TestCase("", "")]
    [TestCase("/", "")]
    [TestCase("/dir", "/")]
    [TestCase("/dir/file", "/dir")]
    [TestCase("/dir/dir/", "/dir/dir")]
    [TestCase("dir/dir", "dir")]
    [TestCase("dir/dir/", "dir/dir")]
    [TestCase("//", "")]
    [TestCase("///", "")]
    [TestCase("//dir", "/")]
    [TestCase("///dir", "/")]
    [TestCase("////dir", "/")]
    [TestCase("/dir///dir", "/dir")]
    [TestCase("/dir///dir///", "/dir///dir")]
    [TestCase("//dir///dir///", "//dir///dir")]
    [TestCase("dir///dir", "dir")]
    public void GetDirectoryName(string path, string expected)
    {
        foreach (var p in GetPathVariations(path))
        {
            if (p.Contains('\\') && expected != "/")
                expected = expected.Replace("/", "\\");

            Assert.That(VirtualPath.GetDirectoryName(p), Is.EqualTo(expected));
            Assert.That(VirtualPath.GetDirectoryName(p.AsSpan()).ToString(), Is.EqualTo(expected));
        }
    }

    [TestCase("/", ExpectedResult = true)]
    [TestCase("/a/b/c", ExpectedResult = true)]
    [TestCase("/a/ /c", ExpectedResult = true)]
    [TestCase("/a/ ", ExpectedResult = true)]
    [TestCase("/a/ /c", ExpectedResult = true)]
    [TestCase("/a/./b/c", ExpectedResult = false)]
    [TestCase("/a/../b/c", ExpectedResult = false)]
    [TestCase("/a/./../b/c", ExpectedResult = false)]
    [TestCase("/a/", ExpectedResult = false)]
    [TestCase("/a//b", ExpectedResult = false)]
    [TestCase("a/b", ExpectedResult = false)]
    [TestCase("a/b/", ExpectedResult = false)]
    [TestCase("/a\\b", ExpectedResult = false)]
    [TestCase("", ExpectedResult = false)]
    [TestCase(" ", ExpectedResult = false)]
    [TestCase(" /", ExpectedResult = false)]
    public bool IsNormalized(string path) =>
        VirtualPath.IsNormalized(path);

    [TestCase("", "/")]
    [TestCase(".", "/")]
    [TestCase(".", "/")]
    [TestCase("/home/", "/home")]
    [TestCase("/home/..folder1/.folder2/file", "/home/..folder1/.folder2/file")]
    [TestCase("/home/././", "/home")]
    [TestCase("/././././/home/user/documents", "/home/user/documents")]
    [TestCase("/home/./user/./././/documents", "/home/user/documents")]
    [TestCase("/home/../home/user//documents", "/home/user/documents")]
    [TestCase("/home/../home/user/../../home/config/documents", "/home/config/documents")]
    [TestCase("/home/../home/user/./.././.././home/config/documents", "/home/config/documents")]
    public void Normalize(string path, string expected)
    {
        foreach (var p in GetPathVariations(path))
            Assert.That(VirtualPath.Normalize(p),Is.EqualTo(expected));
    }

    [TestCase("..")]
    [TestCase("/home/../..")]
    public void Normalize_Error(string path) =>
        Assert.Throws<ArgumentException>(() => VirtualPath.Normalize(path));

    private static string[] GetPathVariations(string path) =>
        [path, path.Replace('/', '\\')];
}
