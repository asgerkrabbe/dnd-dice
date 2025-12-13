using System.Text.RegularExpressions;
using DiceEngine.Models;

namespace DiceEngine.Parsing;

public sealed class DiceParser
{
    private static readonly Regex Pattern = new(
        @"^\s*(?<count>\d*)\s*d\s*(?<sides>\d+)\s*(?<mod>[+-]\s*\d+)?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public DiceParseResult TryParse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DiceParseResult.Failure("Expression cannot be empty.");
        }

        var match = Pattern.Match(input);
        if (!match.Success)
        {
            return DiceParseResult.Failure("Expression must look like NdM+K (e.g., 2d6+1).");
        }

        var countGroup = match.Groups["count"].Value;
        var sidesGroup = match.Groups["sides"].Value;
        var modGroup = match.Groups["mod"].Value;

        if (!int.TryParse(string.IsNullOrEmpty(countGroup) ? "1" : countGroup, out var count) || count <= 0)
        {
            return DiceParseResult.Failure("Number of dice must be a positive integer.");
        }

        if (!int.TryParse(sidesGroup, out var sides) || sides <= 0)
        {
            return DiceParseResult.Failure("Number of sides must be a positive integer.");
        }

        var modifier = 0;
        if (!string.IsNullOrWhiteSpace(modGroup))
        {
            var cleaned = modGroup.Replace(" ", string.Empty);
            if (!int.TryParse(cleaned, out modifier))
            {
                return DiceParseResult.Failure("Modifier must be a whole number with + or -.");
            }
        }

        var expression = new DiceExpression(count, sides, modifier);
        return DiceParseResult.Ok(expression);
    }
}
