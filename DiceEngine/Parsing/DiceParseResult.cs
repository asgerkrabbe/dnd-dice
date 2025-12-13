using DiceEngine.Models;

namespace DiceEngine.Parsing;

public sealed record DiceParseResult(bool Success, DiceExpression? Expression, string? ErrorMessage)
{
    public static DiceParseResult Failure(string message) => new(false, null, message);
    public static DiceParseResult Ok(DiceExpression expression) => new(true, expression, null);
}
