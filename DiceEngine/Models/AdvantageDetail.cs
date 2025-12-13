namespace DiceEngine.Models;

public sealed record AdvantageDetail(IReadOnlyList<int> Rolls, int KeptIndex)
{
    public int KeptValue => Rolls[KeptIndex];
}
