using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.UnitTests.Phase2;

public sealed class TextNormalizerTests
{
    [Fact]
    public void Normalize_EmptyOrNull_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, TextNormalizer.Normalize(null));
        Assert.Equal(string.Empty, TextNormalizer.Normalize(string.Empty));
        Assert.Equal(string.Empty, TextNormalizer.Normalize("   "));
    }

    [Fact]
    public void Normalize_UnifiesLineEndings_And_CollapsesHorizontalWhitespace()
    {
        var input = "  hello\r\n  world  \t foo  ";
        var result = TextNormalizer.Normalize(input);
        Assert.Equal("hello\nworld foo", result);
    }

    [Fact]
    public void Normalize_LimitsConsecutiveNewlines()
    {
        var input = "a\n\n\n\nb";
        var result = TextNormalizer.Normalize(input);
        Assert.Equal("a\n\nb", result);
    }
}
