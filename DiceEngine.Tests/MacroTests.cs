using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;
using DiceEngine.Models;

namespace DiceEngine.Tests;

public class MacroTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), $"DnDDice_Test_{Guid.NewGuid()}");

    public MacroTests()
    {
        Directory.CreateDirectory(_testDirectory);
    }

    private string GetTestMacrosPath() => Path.Combine(_testDirectory, "macros.json");

    [Fact]
    public void SerializesAndDeserializesValidMacro()
    {
        var macros = new List<Macro>
        {
            new() { Name = "Fireball", HitExpression = "1d20+5", DamageExpression = "8d6" },
            new() { Name = "Sword", HitExpression = "1d20+3", DamageExpression = "1d8+2" }
        };

        var json = JsonSerializer.Serialize(macros, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<List<Macro>>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Count);
        Assert.Equal("Fireball", deserialized[0].Name);
        Assert.Equal("8d6", deserialized[0].DamageExpression);
    }

    [Fact]
    public void HandlesEmptyMacroList()
    {
        var macros = new List<Macro>();
        var json = JsonSerializer.Serialize(macros, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<List<Macro>>(json);

        Assert.NotNull(deserialized);
        Assert.Empty(deserialized);
    }

    [Fact]
    public void HandlesCorruptedJsonGracefully()
    {
        var corruptedJson = "{ invalid json [ }";

        var exception = Record.Exception(() =>
        {
            JsonSerializer.Deserialize<List<Macro>>(corruptedJson);
        });

        Assert.NotNull(exception);
        Assert.IsType<JsonException>(exception);
    }

    [Fact]
    public void FiltersOutInvalidMacrosWithEmptyNames()
    {
        var json = JsonSerializer.Serialize(new List<Macro>
        {
            new() { Name = "Valid", HitExpression = "1d20", DamageExpression = "1d6" },
            new() { Name = "", HitExpression = "1d20", DamageExpression = "1d6" },
            new() { Name = "  ", HitExpression = "1d20", DamageExpression = "1d6" }
        });

        var deserialized = JsonSerializer.Deserialize<List<Macro>>(json);
        var validMacros = new List<Macro>();
        foreach (var m in deserialized ?? new())
        {
            if (!string.IsNullOrWhiteSpace(m.Name))
            {
                validMacros.Add(m);
            }
        }

        Assert.Single(validMacros);
        Assert.Equal("Valid", validMacros[0].Name);
    }

    [Fact]
    public void PreservesMacroDataOnDiskRoundTrip()
    {
        var path = GetTestMacrosPath();
        var original = new List<Macro>
        {
            new() { Name = "Test1", HitExpression = "2d6+1", DamageExpression = "3d8-2" },
            new() { Name = "Test2", HitExpression = "d20", DamageExpression = "1d4" }
        };

        var json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        var readJson = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<List<Macro>>(readJson);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Count);
        Assert.Equal("Test1", loaded[0].Name);
        Assert.Equal("3d8-2", loaded[0].DamageExpression);
    }

    [Fact]
    public void ValidatesMacroRequiredFields()
    {
        var macro = new Macro();
        
        var hasEmptyName = string.IsNullOrWhiteSpace(macro.Name);
        var hasEmptyHit = string.IsNullOrWhiteSpace(macro.HitExpression);
        var hasEmptyDamage = string.IsNullOrWhiteSpace(macro.DamageExpression);

        Assert.True(hasEmptyName && hasEmptyHit && hasEmptyDamage, "Macro fields should start empty");
    }

    [Fact]
    public void BuildsConciseCompositionSummary()
    {
        var macro = new Macro
        {
            Name = "Test",
            HitExpression = " 2d6 +1",
            DamageExpression = "d8-2"
        };

        Assert.Equal("Hit: 2d6+1 | Damage: 1d8-2", macro.CompositionSummary);
    }

    [Fact]
    public void AllowsNullMacroListFromDeserialization()
    {
        var nullJson = "null";
        var result = JsonSerializer.Deserialize<List<Macro>>(nullJson);

        Assert.Null(result);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch { /* ignore cleanup errors */ }
    }
}
