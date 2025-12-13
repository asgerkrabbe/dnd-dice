namespace DiceEngine.Models;

public sealed record RollResult(
    string OriginalInput,
    DiceExpression Expression,
    RollMode RollMode,
    IReadOnlyList<int> Rolls,
    int Modifier,
    int Total,
    AdvantageDetail? Advantage)
{
    public string NormalizedExpression => Expression.ToString();
}
