using DiceEngine.Models;
using DiceEngine.Random;

namespace DiceEngine.Rolling;

public sealed class DiceRoller
{
    private readonly IRandomSource _random;

    public DiceRoller(IRandomSource random)
    {
        _random = random;
    }

    public RollResult Roll(DiceExpression expression, RollMode rollMode, string originalInput)
    {
        if (expression is null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        var rolls = new List<int>();
        AdvantageDetail? advantage = null;
        var modifier = expression.Modifier;

        if (rollMode != RollMode.Normal && expression.Count == 1 && expression.Sides == 20)
        {
            var first = _random.Next(1, 21);
            var second = _random.Next(1, 21);
            var both = new List<int> { first, second };
            var keptIndex = rollMode == RollMode.Advantage
                ? (first >= second ? 0 : 1)
                : (first <= second ? 0 : 1);
            rolls.Add(both[keptIndex]);
            advantage = new AdvantageDetail(both, keptIndex);
        }
        else
        {
            for (var i = 0; i < expression.Count; i++)
            {
                rolls.Add(_random.Next(1, expression.Sides + 1));
            }
        }

        var total = rolls.Sum() + modifier;

        return new RollResult(
            originalInput,
            expression,
            rollMode,
            rolls,
            modifier,
            total,
            advantage);
    }
}
