namespace Ramstack.FileSystem.Composite;

[TestFixture]
public class CompositionHelperTests
{
    [Test]
    public void Flatten_ReturnsAsIs_WhenNoComposite()
    {
        var fs = new TestFileSystem();
        var result = CompositeFileSystem.Flatten(fs);
        Assert.That(result, Is.SameAs(fs));
    }

    [Test]
    public void Flatten_ReturnsCompositeFileSystem_WhenNeedComposite()
    {
        var fs = new CompositeFileSystem(new TestFileSystem(), new TestFileSystem());

        var result = CompositeFileSystem.Flatten(fs);
        Assert.That(result, Is.InstanceOf<CompositeFileSystem>());
    }

    [Test]
    public void Flatten_ReturnsAsIs_WhenAlreadyFlat()
    {
        var fs = new CompositeFileSystem(new TestFileSystem(), new TestFileSystem());

        var result = CompositeFileSystem.Flatten(fs);
        Assert.That(result, Is.SameAs(fs));
    }

    [Test]
    public void Flatten_ReturnsCompositeFileSystem_Flattened()
    {
        var fs = new CompositeFileSystem(
            new TestFileSystem(),
            new CompositeFileSystem(
                new TestFileSystem(),
                new TestFileSystem(),
                new CompositeFileSystem(
                    new TestFileSystem())));

        var result = CompositeFileSystem.Flatten(fs);

        Assert.That(result, Is.InstanceOf<CompositeFileSystem>());
        Assert.That(((CompositeFileSystem)result).FileSystems, Is.All.InstanceOf<TestFileSystem>());
        Assert.That(((CompositeFileSystem)result).FileSystems.Count, Is.EqualTo(4));
    }

    [Test]
    public void Flatten_ReturnsCompositeFileSystem_WhenNothingReturn()
    {
        var fs = new CompositeFileSystem(
            new CompositeFileSystem(
                new CompositeFileSystem(),
                new CompositeFileSystem(),
                new CompositeFileSystem(
                    new CompositeFileSystem(
                        new CompositeFileSystem(),
                        new CompositeFileSystem()),
                    new CompositeFileSystem()),
                new CompositeFileSystem(
                    new CompositeFileSystem(),
                    new CompositeFileSystem(),
                    new CompositeFileSystem(
                        new CompositeFileSystem(
                            new CompositeFileSystem(),
                            new CompositeFileSystem()),
                        new CompositeFileSystem()))),
            new CompositeFileSystem(),
            new CompositeFileSystem(),
            new CompositeFileSystem(
                new CompositeFileSystem(),
                new CompositeFileSystem(),
                new CompositeFileSystem(
                    new CompositeFileSystem(
                        new CompositeFileSystem(),
                        new CompositeFileSystem()),
                    new CompositeFileSystem())),
            new CompositeFileSystem(),
            new CompositeFileSystem(
                new CompositeFileSystem(
                    new CompositeFileSystem(),
                    new CompositeFileSystem()),
                new CompositeFileSystem()));

        var result = CompositeFileSystem.Flatten(fs);
        Assert.That(result, Is.InstanceOf<CompositeFileSystem>());
        Assert.That(((CompositeFileSystem)result).FileSystems.Count, Is.Zero);
    }

    [Test]
    public void Flatten_ReturnsSingleFileSystem_WhenRemainOneFileSystem()
    {
        var fs = new CompositeFileSystem(
            new CompositeFileSystem(
                new CompositeFileSystem(),
                new CompositeFileSystem(),
                new CompositeFileSystem(
                    new CompositeFileSystem(
                        new CompositeFileSystem(),
                        new CompositeFileSystem()),
                    new CompositeFileSystem()),
                new CompositeFileSystem(
                    new CompositeFileSystem(),
                    new CompositeFileSystem(),
                    new CompositeFileSystem(
                        new CompositeFileSystem(
                            new CompositeFileSystem(
                                new TestFileSystem()),
                            new CompositeFileSystem()),
                        new CompositeFileSystem()))),
            new CompositeFileSystem(),
            new CompositeFileSystem(),
            new CompositeFileSystem(
                new CompositeFileSystem(),
                new CompositeFileSystem(),
                new CompositeFileSystem(
                    new CompositeFileSystem(
                        new CompositeFileSystem(),
                        new CompositeFileSystem()),
                    new CompositeFileSystem())),
            new CompositeFileSystem(),
            new CompositeFileSystem(
                new CompositeFileSystem(
                    new CompositeFileSystem(),
                    new CompositeFileSystem()),
                new CompositeFileSystem()));

        var result = CompositeFileSystem.Flatten(fs);
        Assert.That(result, Is.InstanceOf<TestFileSystem>());
    }

    [Test]
    public void Flatten_MaintainOrder()
    {
        var fs1 = new TestFileSystem();
        var fs2 = new TestFileSystem();
        var fs3 = new TestFileSystem();
        var fs4 = new TestFileSystem();
        var fs5 = new TestFileSystem();
        var fs6 = new TestFileSystem();
        var fs7 = new TestFileSystem();
        var fs8 = new TestFileSystem();
        var fs9 = new TestFileSystem();

        var fs = CompositeFileSystem.Create(
            new CompositeFileSystem(
                new CompositeFileSystem(),
                new CompositeFileSystem(),
                new CompositeFileSystem(
                    fs1,
                    new CompositeFileSystem(
                        fs2,
                        new CompositeFileSystem(),
                        fs3),
                    fs4,
                    new CompositeFileSystem()),
                fs5,
                new CompositeFileSystem(
                    new CompositeFileSystem(),
                    new CompositeFileSystem(),
                    new CompositeFileSystem(
                        new CompositeFileSystem(
                            new CompositeFileSystem(),
                            fs6),
                        new CompositeFileSystem())),
                fs7),
            new CompositeFileSystem(),
            new CompositeFileSystem(),
            new CompositeFileSystem(
                new CompositeFileSystem(),
                new CompositeFileSystem(),
                new CompositeFileSystem(
                    new CompositeFileSystem(
                        new CompositeFileSystem(),
                        new CompositeFileSystem()),
                    new CompositeFileSystem(),
                    fs8)),
            new CompositeFileSystem(),
            new CompositeFileSystem(
                new CompositeFileSystem(
                    new CompositeFileSystem(),
                    new CompositeFileSystem()),
                new CompositeFileSystem(),
                fs9));

        var compositeFileSystem = (CompositeFileSystem)fs;
        var list = new IVirtualFileSystem[] { fs1, fs2, fs3, fs4, fs5, fs6, fs7, fs8, fs9 };

        Assert.That(fs, Is.InstanceOf<CompositeFileSystem>());
        Assert.That(compositeFileSystem.FileSystems, Is.EquivalentTo(list));
    }
}
