using Ramstack.FileSystem.Globbing.Internal;

namespace Ramstack.FileSystem.Globbing;

[TestFixture]
public class PathHelperTests
{
    [Test]
    public void IsMatch_EmptyPatterns_ReturnsFalse()
    {
        Assert.That(PathHelper.IsMatch("a.txt", []), Is.False);
    }

    [Test]
    public void IsPartialMatch_SecondPatternMatches_ReturnsTrue()
    {
        Assert.That(PathHelper.IsPartialMatch("docs", ["src/**", "docs/*.md"]), Is.True);
    }

    [TestCase("", 1)]
    [TestCase("/", 1)]
    [TestCase("////", 1)]
    [TestCase("a", 1)]
    [TestCase("a/b", 2)]
    [TestCase("/a/b/", 2)]
    [TestCase("a//b///c", 3)]
    [TestCase("a\\b", 1)]
    [TestCase("a\0/b", 2)]
    [TestCase("a\u012fb\uff2fc", 1)]
    public void CountPathSegments_ReturnsExpectedCount(string path, int expected)
    {
        Assert.That(PathHelper.CountPathSegments(path), Is.EqualTo(expected));
    }

    [Test]
    public void CountPathSegments_DefaultSpan_ReturnsOne()
    {
        Assert.That(PathHelper.CountPathSegments(default), Is.EqualTo(1));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(15)]
    [TestCase(16)]
    [TestCase(17)]
    [TestCase(31)]
    [TestCase(32)]
    [TestCase(33)]
    [TestCase(63)]
    [TestCase(64)]
    [TestCase(65)]
    public void CountPathSegments_VectorBoundaries_ReturnsExpectedCount(int length)
    {
        var chars = new string('a', length).ToCharArray();
        Assert.That(PathHelper.CountPathSegments(chars), Is.EqualTo(1));

        for (var separator = 0; separator < length; separator++)
        {
            chars[separator] = '/';
            var expected = separator == 0 || separator == length - 1 ? 1 : 2;
            Assert.That(PathHelper.CountPathSegments(chars), Is.EqualTo(expected),
                $"Separator at {separator}, length {length}");
            chars[separator] = 'a';
        }

        chars.AsSpan().Fill('/');
        Assert.That(PathHelper.CountPathSegments(chars), Is.EqualTo(1));

        for (var parity = 0; parity < 2; parity++)
        {
            for (var i = 0; i < length; i++)
                chars[i] = (i & 1) == parity ? '/' : 'a';

            var expected = Math.Max(1, (length + parity) / 2);
            Assert.That(PathHelper.CountPathSegments(chars), Is.EqualTo(expected),
                $"Alternating separators, parity {parity}, length {length}");
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(15)]
    [TestCase(16)]
    [TestCase(17)]
    [TestCase(31)]
    [TestCase(32)]
    [TestCase(33)]
    [TestCase(63)]
    [TestCase(64)]
    [TestCase(65)]
    public void CountPathSegments_Slices_IgnoreSurroundingCharacters(int length)
    {
        foreach (var offset in new[] { 0, 1, 7, 8, 15, 16, 17 })
        {
            var storage = new string('/', offset + length + 32).ToCharArray();
            var path = storage.AsSpan(offset, length);
            path.Fill('a');

            Assert.That(PathHelper.CountPathSegments(path), Is.EqualTo(1),
                $"Offset {offset}, length {length}");

            for (var i = 0; i < length; i += 3)
                path[i] = '/';

            Assert.That(PathHelper.CountPathSegments(path), Is.EqualTo(CountSegmentsReference(path)),
                $"Offset {offset}, length {length}, repeated separators");
        }
    }

    [TestCase("", 1, "")]
    [TestCase("/", 1, "/")]
    [TestCase("///", 4, "///")]
    [TestCase("a/b/c", 1, "a")]
    [TestCase("a/b/c", 2, "a/b")]
    [TestCase("a/b/c", 3, "a/b/c")]
    [TestCase("a/b/c", 4, "a/b/c")]
    [TestCase("/a//b///c/", 2, "/a//b")]
    [TestCase("//a///b/", 3, "//a///b/")]
    [TestCase("a/**/b/c", 1, "a")]
    [TestCase("a/**/b/c", 2, "a/**")]
    [TestCase("a/**/b/c", 3, "a/**")]
    [TestCase("a/**/b/c", 8, "a/**")]
    [TestCase("**/a/b", 5, "**")]
    [TestCase("a/***/b", 3, "a/***/b")]
    [TestCase("a/b**/c", 3, "a/b**/c")]
    [TestCase("a/*/b", 3, "a/*/b")]
    [TestCase("a/\u012a\u012a/b", 3, "a/\u012a\u012a/b")]
    [TestCase("a/\0\0/b", 3, "a/\0\0/b")]
    [TestCase("a\\b/c", 1, "a\\b")]
    public void GetPartialPattern_ReturnsExpectedPrefix(string pattern, int depth, string expected)
    {
        Assert.That(PathHelper.GetPartialPattern(pattern, depth).ToString(), Is.EqualTo(expected));
    }

    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(14)]
    [TestCase(15)]
    [TestCase(16)]
    [TestCase(30)]
    [TestCase(31)]
    [TestCase(32)]
    public void GetPartialPattern_VectorBoundaries_ReturnsRequestedPrefix(int prefixLength)
    {
        var prefix = new string('a', prefixLength);
        var pattern = prefix + "/next/tail";

        Assert.That(PathHelper.GetPartialPattern(pattern, 1).ToString(), Is.EqualTo(prefix));
        Assert.That(PathHelper.GetPartialPattern(pattern, 2).ToString(), Is.EqualTo(prefix + "/next"));
        Assert.That(PathHelper.GetPartialPattern(pattern, 3).ToString(), Is.EqualTo(pattern));
        Assert.That(PathHelper.GetPartialPattern(pattern, 8).ToString(), Is.EqualTo(pattern));
    }

    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(14)]
    [TestCase(15)]
    [TestCase(16)]
    [TestCase(30)]
    [TestCase(31)]
    [TestCase(32)]
    public void GetPartialPattern_GlobStarAtVectorBoundaries_StopsAtGlobStar(int prefixLength)
    {
        var prefix = new string('a', prefixLength);
        foreach (var separator in new[] { "/", "//" })
        {
            var expected = prefix + separator + "**";
            var pattern = expected + "/next/tail";

            Assert.That(PathHelper.GetPartialPattern(pattern, 1).ToString(), Is.EqualTo(prefix));
            Assert.That(PathHelper.GetPartialPattern(pattern, 2).ToString(), Is.EqualTo(expected));
            Assert.That(PathHelper.GetPartialPattern(pattern, 3).ToString(), Is.EqualTo(expected));
            Assert.That(PathHelper.GetPartialPattern(pattern, 8).ToString(), Is.EqualTo(expected));
        }
    }

    [Test]
    public void PathSegments_AllShortSeparatorPatterns_MatchReference()
    {
        // Every separator mask in one 256-bit chunk, including empty segments.
        for (var length = 0; length <= 16; length++)
        {
            var chars = new char[length];
            for (var mask = 0; mask < 1 << length; mask++)
            {
                for (var i = 0; i < length; i++)
                    chars[i] = (mask & 1 << i) != 0 ? '/' : 'a';

                AssertMatchesReference(new string(chars));
            }
        }
    }

    [Test]
    public void PathSegments_RandomInputs_MatchReference()
    {
        var random = new Random(5471);
        const string alphabet = "aaab//**\\\0\u012f\uff2f\ud800\udfff";

        for (var trial = 0; trial < 20000; trial++)
        {
            var chars = new char[random.Next(1025)];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = alphabet[random.Next(alphabet.Length)];

            AssertMatchesReference(new string(chars));
        }
    }

    private static void AssertMatchesReference(string pattern)
    {
        var expectedCount = CountSegmentsReference(pattern);
        var actualCount = PathHelper.CountPathSegments(pattern);
        if (actualCount != expectedCount)
            Assert.Fail($"Path: '{pattern}', expected count: {expectedCount}, actual: {actualCount}");

        foreach (var depth in new[] { 1, 2, 3, 8, 32 })
        {
            var expected = GetPartialPatternReference(pattern, depth);
            var actual = PathHelper.GetPartialPattern(pattern, depth);
            if (!actual.SequenceEqual(expected))
                Assert.Fail($"Pattern: '{pattern}', depth: {depth}, expected: '{expected}', actual: '{actual.ToString()}'");
        }
    }

    private static int CountSegmentsReference(ReadOnlySpan<char> path)
    {
        var count = 0;
        var inside = false;
        foreach (var ch in path)
        {
            if (ch == '/')
                inside = false;
            else if (!inside)
            {
                count++;
                inside = true;
            }
        }

        return Math.Max(1, count);
    }

    private static string GetPartialPatternReference(string pattern, int depth)
    {
        var segments = pattern.Split('/');
        var count = 0;
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i].Length != 0)
                count++;

            if (count >= depth || segments[i] == "**")
                return string.Join("/", segments, 0, i + 1);
        }

        return pattern;
    }
}
