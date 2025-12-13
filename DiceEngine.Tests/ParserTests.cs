using DiceEngine.Parsing;
using Xunit;

namespace DiceEngine.Tests;

public class ParserTests
{
    private readonly DiceParser _parser = new();

    [Theory]
    [InlineData("d20", 1, 20, 0)]
    [InlineData("1d20", 1, 20, 0)]
    [InlineData("2d8+3", 2, 8, 3)]
    [InlineData("4d6 - 2", 4, 6, -2)]
    [InlineData("   3d10\t+5  ", 3, 10, 5)]
    public void ParsesValidExpressions(string input, int expectedCount, int expectedSides, int expectedModifier)
    {
        var result = _parser.TryParse(input);

        Assert.True(result.Success);
        Assert.NotNull(result.Expression);
        var expr = result.Expression!;
        Assert.Equal(expectedCount, expr.Count);
        Assert.Equal(expectedSides, expr.Sides);
        Assert.Equal(expectedModifier, expr.Modifier);
    }

    [Theory]
    [InlineData("")]
    [InlineData("dd20")]
    [InlineData("2d")]
    [InlineData("0d6")]
    [InlineData("2d0")]
    [InlineData("2d6++1")]
    [InlineData("abc")]
    public void RejectsInvalidExpressions(string input)
    {
        var result = _parser.TryParse(input);
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }
}
