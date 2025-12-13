using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DiceEngine.Models;
using DiceEngine.Parsing;
using DiceEngine.Rolling;
using DiceRoller.Wpf.Commands;

namespace DiceRoller.Wpf.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly DiceParser _parser;
    private readonly DiceEngine.Rolling.DiceRoller _roller;
    private string _expressionText = "1d20";
    private RollMode _selectedRollMode = RollMode.Normal;
    private string _totalText = "Total: -";
    private string _rollsText = "Rolls: -";
    private string _advantageText = string.Empty;
    private string _statusMessage = string.Empty;

    public MainViewModel(DiceParser parser, DiceEngine.Rolling.DiceRoller roller)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _roller = roller ?? throw new ArgumentNullException(nameof(roller));
        RollCommand = new RelayCommand(ExecuteRoll);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand RollCommand { get; }

    public IReadOnlyList<RollMode> RollModes { get; } = Enum.GetValues<RollMode>();

    public string ExpressionText
    {
        get => _expressionText;
        set => SetField(ref _expressionText, value);
    }

    public RollMode SelectedRollMode
    {
        get => _selectedRollMode;
        set => SetField(ref _selectedRollMode, value);
    }

    public string TotalText
    {
        get => _totalText;
        private set => SetField(ref _totalText, value);
    }

    public string RollsText
    {
        get => _rollsText;
        private set => SetField(ref _rollsText, value);
    }

    public string AdvantageText
    {
        get => _advantageText;
        private set => SetField(ref _advantageText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    private void ExecuteRoll()
    {
        var parseResult = _parser.TryParse(ExpressionText);
        if (!parseResult.Success || parseResult.Expression is null)
        {
            StatusMessage = parseResult.ErrorMessage ?? "Unable to parse expression.";
            TotalText = "Total: -";
            RollsText = "Rolls: -";
            AdvantageText = string.Empty;
            return;
        }

        var rollResult = _roller.Roll(parseResult.Expression, SelectedRollMode, ExpressionText);
        TotalText = $"Total: {rollResult.Total}";
        RollsText = $"Rolls: {string.Join(", ", rollResult.Rolls)} (modifier {rollResult.Modifier:+#;-#;0})";

        if (rollResult.Advantage is { } adv)
        {
            AdvantageText = $"Advantage rolls: {string.Join(", ", adv.Rolls)} (kept #{adv.KeptIndex + 1}: {adv.KeptValue})";
        }
        else
        {
            AdvantageText = string.Empty;
        }

        StatusMessage = $"Rolled {rollResult.NormalizedExpression} in {rollResult.RollMode} mode.";
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
