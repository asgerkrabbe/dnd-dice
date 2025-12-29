using System.Collections.Generic;
using System.Text.Json.Serialization;
using DiceEngine.Parsing;

namespace DiceEngine.Models;

public sealed class Macro
{
    private static readonly DiceParser Parser = new DiceParser();
    
    public string Name { get; set; } = string.Empty;
    public string HitExpression { get; set; } = string.Empty;
    public string DamageExpression { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [JsonIgnore]
    public string CompositionSummary => BuildCompositionSummary(HitExpression, DamageExpression);

    private static string BuildCompositionSummary(string hitExpression, string damageExpression)
    {
        var parts = new List<string>(2);

        var hit = NormalizeExpression("Hit", hitExpression);
        if (hit is not null)
        {
            parts.Add(hit);
        }

        var dmg = NormalizeExpression("Damage", damageExpression);
        if (dmg is not null)
        {
            parts.Add(dmg);
        }

        return string.Join(" | ", parts);
    }

    private static string? NormalizeExpression(string label, string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        var result = Parser.TryParse(expression);
        var normalized = result.Success && result.Expression is not null
            ? result.Expression.ToString()
            : expression.Trim();

        return $"{label}: {normalized}";
    }
}
