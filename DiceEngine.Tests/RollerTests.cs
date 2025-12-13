using DiceEngine.Models;
using DiceEngine.Random;
using DiceEngine.Rolling;
using Xunit;

namespace DiceEngine.Tests;

public class RollerTests
{
    private sealed class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<int> _values;

        public SequenceRandomSource(IEnumerable<int> values)
        {
            _values = new Queue<int>(values);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (_values.Count == 0)
            {
                throw new InvalidOperationException("Ran out of deterministic values.");
            }

            return _values.Dequeue();
        }
    }

    [Fact]
    public void RollsExpressionUsingDeterministicValues()
    {
        var rng = new SequenceRandomSource(new[] { 3, 4, 6 });
        var roller = new DiceRoller(rng);
        var expression = new DiceExpression(2, 6, 1);

        var result = roller.Roll(expression, RollMode.Normal, "2d6+1");

        Assert.Equal(3 + 4 + 1, result.Total);
        Assert.Equal(new[] { 3, 4 }, result.Rolls);
        Assert.Equal(1, result.Modifier);
        Assert.Null(result.Advantage);
    }

    [Fact]
    public void UsesHighestForAdvantageOnD20()
    {
        var rng = new SequenceRandomSource(new[] { 5, 17 });
        var roller = new DiceRoller(rng);
        var expression = new DiceExpression(1, 20, 2);

        var result = roller.Roll(expression, RollMode.Advantage, "1d20+2");

        Assert.Equal(19, result.Total);
        Assert.Single(result.Rolls, 17);
        Assert.NotNull(result.Advantage);
        var adv = result.Advantage!;
        Assert.Equal(new[] { 5, 17 }, adv.Rolls);
        Assert.Equal(1, adv.KeptIndex);
    }

    [Fact]
    public void UsesLowestForDisadvantageOnD20()
    {
        var rng = new SequenceRandomSource(new[] { 15, 2 });
        var roller = new DiceRoller(rng);
        var expression = new DiceExpression(1, 20, -1);

        var result = roller.Roll(expression, RollMode.Disadvantage, "d20-1");

        Assert.Equal(1, result.Total);
        Assert.Single(result.Rolls, 2);
        Assert.NotNull(result.Advantage);
        var adv = result.Advantage!;
        Assert.Equal(1, adv.KeptIndex);
    }
}
