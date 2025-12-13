namespace DiceEngine.Models;

public sealed record DiceExpression(int Count, int Sides, int Modifier)
{
    public override string ToString()
    {
        var modifierPart = Modifier switch
        {
            > 0 => $"+{Modifier}",
            < 0 => Modifier.ToString(),
            _ => string.Empty
        };
        return $"{Count}d{Sides}{modifierPart}";
    }
}
