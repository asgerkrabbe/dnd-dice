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
    private string _selectedDiceType = "1d20";
    private int _selectedQuantity = 1;
    private int? _selectedQuantityDropdown = null;
    private int _selectedModifier = 0;
    private int? _selectedModifierDropdown = null;
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
        SelectDiceTypeCommand = new RelayCommand<string>(ExecuteSelectDiceType);
        SelectQuantityCommand = new RelayCommand<int>(ExecuteSelectQuantity);
        SelectModifierCommand = new RelayCommand<int>(ExecuteSelectModifier);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand RollCommand { get; }
    public ICommand SelectDiceTypeCommand { get; }
    public ICommand SelectQuantityCommand { get; }
    public ICommand SelectModifierCommand { get; }

    public IReadOnlyList<RollMode> RollModes { get; } = Enum.GetValues<RollMode>();

    public IReadOnlyList<string> AvailableDiceTypes { get; } = new[]
    {
        "1d4",
        "1d6",
        "1d8",
        "1d12",
        "1d20",
        "1d100",
    };

    public IReadOnlyList<int> QuantityDropdownOptions { get; } = Enumerable.Range(6, 15).ToArray();

    public IReadOnlyList<int> ModifierDropdownOptions { get; } = Enumerable.Range(-10, 21).ToArray();

    public string SelectedDiceType
    {
        get => _selectedDiceType;
        set => SetField(ref _selectedDiceType, value);
    }

    public int SelectedQuantity
    {
        get => _selectedQuantity;
        set
        {
            if (SetField(ref _selectedQuantity, value))
            {
                // Clear dropdown when button is used
                SelectedQuantityDropdown = null;
            }
        }
    }

    public int? SelectedQuantityDropdown
    {
        get => _selectedQuantityDropdown;
        set
        {
            if (SetField(ref _selectedQuantityDropdown, value) && value.HasValue)
            {
                // Update quantity when dropdown is used
                _selectedQuantity = value.Value;
                OnPropertyChanged(nameof(SelectedQuantity));
            }
        }
    }

    public int SelectedModifier
    {
        get => _selectedModifier;
        set
        {
            if (SetField(ref _selectedModifier, value))
            {
                // Clear dropdown when button is used
                SelectedModifierDropdown = null;
            }
        }
    }

    public int? SelectedModifierDropdown
    {
        get => _selectedModifierDropdown;
        set
        {
            if (SetField(ref _selectedModifierDropdown, value) && value.HasValue)
            {
                // Update modifier when dropdown is used
                _selectedModifier = value.Value;
                OnPropertyChanged(nameof(SelectedModifier));
            }
        }
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

    private void ExecuteSelectDiceType(string? diceType)
    {
        if (!string.IsNullOrEmpty(diceType))
        {
            SelectedDiceType = diceType;
        }
    }

    private void ExecuteSelectQuantity(int quantity)
    {
        SelectedQuantity = quantity;
    }

    private void ExecuteSelectModifier(int modifier)
    {
        SelectedModifier = modifier;
    }

    private void ExecuteRoll()
    {
        // Build the expression from quantity, dice type, and modifier
        var expression = $"{SelectedQuantity}{SelectedDiceType.Substring(1)}";
        if (SelectedModifier != 0)
        {
            expression += $"{SelectedModifier:+#;-#;0}";
        }

        var parseResult = _parser.TryParse(expression);
        if (!parseResult.Success || parseResult.Expression is null)
        {
            StatusMessage = parseResult.ErrorMessage ?? "Unable to parse expression.";
            TotalText = "Total: -";
            RollsText = "Rolls: -";
            AdvantageText = string.Empty;
            return;
        }

        var rollResult = _roller.Roll(parseResult.Expression, SelectedRollMode, expression);
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

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
