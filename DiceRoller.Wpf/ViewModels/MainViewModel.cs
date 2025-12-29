using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using Microsoft.Win32;
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
    private bool _isHistoryCollapsed = false;
    private bool _showMacroDeleteButtons = false;
    private bool _showMacroEditButtons = false;
    private bool _showMacroReorderButtons = false;
    private bool _showMacroDetails = false;
    private string _macroCategory = string.Empty;
    private string _macroDescription = string.Empty;

    public MainViewModel(DiceParser parser, DiceEngine.Rolling.DiceRoller roller)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _roller = roller ?? throw new ArgumentNullException(nameof(roller));
        RollHistory = new ObservableCollection<string>();
        Macros = new ObservableCollection<Macro>();
        RollCommand = new RelayCommand(ExecuteRoll);
        SelectDiceTypeCommand = new RelayCommand<string>(ExecuteSelectDiceType);
        SelectQuantityCommand = new RelayCommand<int>(ExecuteSelectQuantity);
        SelectModifierCommand = new RelayCommand<int>(ExecuteSelectModifier);
        ExecuteMacroCommand = new RelayCommand<Macro>(ExecuteMacro);
        SaveMacroCommand = new RelayCommand(ExecuteSaveMacro);
        DeleteMacroCommand = new RelayCommand(ExecuteDeleteMacro);
        ClearHistoryCommand = new RelayCommand(ExecuteClearHistory);
        CopyTextCommand = new RelayCommand<string>(ExecuteCopyText);
        ToggleHistoryCollapsedCommand = new RelayCommand(ExecuteToggleHistoryCollapsed);
        ToggleMacroDeleteButtonsCommand = new RelayCommand(ExecuteToggleMacroDeleteButtons);
        DeleteMacroItemCommand = new RelayCommand<Macro>(ExecuteDeleteMacroItem);
        ToggleMacroEditButtonsCommand = new RelayCommand(ExecuteToggleMacroEditButtons);
        EditMacroCommand = new RelayCommand<Macro>(ExecuteEditMacro);
        ClearMacroFieldsCommand = new RelayCommand(ExecuteClearMacroFields);
        MoveMacroUpCommand = new RelayCommand<Macro>(ExecuteMoveMacroUp);
        MoveMacroDownCommand = new RelayCommand<Macro>(ExecuteMoveMacroDown);
        ImportMacrosCommand = new RelayCommand(ExecuteImportMacros);
        ExportMacrosCommand = new RelayCommand(ExecuteExportMacros);
        ExitCommand = new RelayCommand(ExecuteExit);
        ToggleMacroReorderButtonsCommand = new RelayCommand(ExecuteToggleMacroReorderButtons);
        OpenMacroEditorCommand = new RelayCommand(ExecuteOpenMacroEditor);
        ToggleMacroDetailsCommand = new RelayCommand(ExecuteToggleMacroDetails);

        // Keep CanDeleteMacro accurate when the macro list changes
        Macros.CollectionChanged += (_, __) => OnPropertyChanged(nameof(CanDeleteMacro));

        LoadMacros();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand RollCommand { get; }
    public ICommand SelectDiceTypeCommand { get; }
    public ICommand SelectQuantityCommand { get; }
    public ICommand SelectModifierCommand { get; }
    public ICommand ExecuteMacroCommand { get; }
    public ICommand SaveMacroCommand { get; }
    public ICommand DeleteMacroCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand CopyTextCommand { get; }
    public ICommand ToggleHistoryCollapsedCommand { get; }
    public ICommand ToggleMacroDeleteButtonsCommand { get; }
    public ICommand DeleteMacroItemCommand { get; }
    public ICommand ToggleMacroEditButtonsCommand { get; }
    public ICommand EditMacroCommand { get; }
    public ICommand ClearMacroFieldsCommand { get; }
    public ICommand MoveMacroUpCommand { get; }
    public ICommand MoveMacroDownCommand { get; }
    public ICommand ImportMacrosCommand { get; }
    public ICommand ExportMacrosCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand ToggleMacroReorderButtonsCommand { get; }
    public ICommand OpenMacroEditorCommand { get; }
    public ICommand ToggleMacroDetailsCommand { get; }

    public ObservableCollection<string> RollHistory { get; }
    public ObservableCollection<Macro> Macros { get; }

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

    public bool IsHistoryCollapsed
    {
        get => _isHistoryCollapsed;
        set => SetField(ref _isHistoryCollapsed, value);
    }

    public bool ShowMacroDeleteButtons
    {
        get => _showMacroDeleteButtons;
        set => SetField(ref _showMacroDeleteButtons, value);
    }

    public bool ShowMacroEditButtons
    {
        get => _showMacroEditButtons;
        set => SetField(ref _showMacroEditButtons, value);
    }

    public bool ShowMacroReorderButtons
    {
        get => _showMacroReorderButtons;
        set => SetField(ref _showMacroReorderButtons, value);
    }

    public bool ShowMacroDetails
    {
        get => _showMacroDetails;
        set => SetField(ref _showMacroDetails, value);
    }

    public bool CanDeleteMacro =>
        SelectedMacro is not null ||
        (!string.IsNullOrWhiteSpace(MacroName) && Macros.Any(m => string.Equals(m.Name, MacroName, StringComparison.OrdinalIgnoreCase)));

    // Macro editing fields
    private string _macroName = string.Empty;
    private string _macroHitExpression = string.Empty;
    private string _macroDamageExpression = string.Empty;
    private Macro? _selectedMacro;

    public string MacroName
    {
        get => _macroName;
        set
        {
            if (SetField(ref _macroName, value))
            {
                OnPropertyChanged(nameof(CanDeleteMacro));
            }
        }
    }

    public string MacroHitExpression
    {
        get => _macroHitExpression;
        set => SetField(ref _macroHitExpression, value);
    }

    public string MacroDamageExpression
    {
        get => _macroDamageExpression;
        set => SetField(ref _macroDamageExpression, value);
    }

    public string MacroCategory
    {
        get => _macroCategory;
        set => SetField(ref _macroCategory, value);
    }

    public string MacroDescription
    {
        get => _macroDescription;
        set => SetField(ref _macroDescription, value);
    }

    public Macro? SelectedMacro
    {
        get => _selectedMacro;
        set
        {
            if (SetField(ref _selectedMacro, value))
            {
                // Populate fields when selecting a macro for editing
                if (value is not null)
                {
                    MacroName = value.Name;
                    MacroHitExpression = value.HitExpression;
                    MacroDamageExpression = value.DamageExpression;
                    MacroCategory = value.Category;
                    MacroDescription = value.Description;
                }
                OnPropertyChanged(nameof(CanDeleteMacro));
            }
        }
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
            var modeText = rollResult.RollMode == RollMode.Advantage ? "Advantage" : "Disadvantage";
            AdvantageText = $"{modeText} rolls: {string.Join(", ", adv.Rolls)} (kept #{adv.KeptIndex + 1}: {adv.KeptValue})";
        }
        else
        {
            AdvantageText = string.Empty;
        }

        StatusMessage = $"Rolled {rollResult.NormalizedExpression} in {rollResult.RollMode} mode.";
        
        // Add to history
        var historyEntry = $"[{DateTime.Now:HH:mm}] {rollResult.NormalizedExpression} = {rollResult.Total}";
        if (rollResult.Advantage is { } advHistory)
        {
            var modeLabel = rollResult.RollMode == RollMode.Advantage ? "Adv" : "Dis";
            historyEntry += $" ({modeLabel}, kept {advHistory.KeptValue})";
        }
        RollHistory.Insert(0, historyEntry);
        
        // Keep history limited to 50 entries
        while (RollHistory.Count > 50)
        {
            RollHistory.RemoveAt(RollHistory.Count - 1);
        }
    }

    private void ExecuteMacro(Macro? macro)
    {
        if (macro is null)
        {
            return;
        }
        // Parse expressions
        var hitParse = _parser.TryParse(macro.HitExpression);
        var dmgParse = _parser.TryParse(macro.DamageExpression);

        RollResult? hitResult = null;
        RollResult? dmgResult = null;

        // Execute hit (uses current roll mode for adv/dis)
        if (hitParse.Success && hitParse.Expression is not null)
        {
            hitResult = _roller.Roll(hitParse.Expression, SelectedRollMode, macro.HitExpression);
        }

        // Execute damage (always normal mode)
        if (dmgParse.Success && dmgParse.Expression is not null)
        {
            dmgResult = _roller.Roll(dmgParse.Expression, RollMode.Normal, macro.DamageExpression);
        }

        // Update UI result fields in fixed order: Hit first, Damage second
        if (hitResult is not null && dmgResult is not null)
        {
            TotalText = $"Total: Hit {hitResult.Total}, Damage {dmgResult.Total}";
            RollsText =
                $"Rolls: Hit {string.Join(", ", hitResult.Rolls)} (modifier {hitResult.Modifier:+#;-#;0}); " +
                $"Damage {string.Join(", ", dmgResult.Rolls)} (modifier {dmgResult.Modifier:+#;-#;0})";

            if (hitResult.Advantage is { } adv)
            {
                var modeText = hitResult.RollMode == RollMode.Advantage ? "Advantage" : "Disadvantage";
                AdvantageText = $"{modeText} (hit) rolls: {string.Join(", ", adv.Rolls)} (kept #{adv.KeptIndex + 1}: {adv.KeptValue})";
            }
            else
            {
                AdvantageText = string.Empty;
            }

            StatusMessage = $"Macro '{macro.Name}' executed: hit {hitResult.NormalizedExpression} ({hitResult.RollMode}), damage {dmgResult.NormalizedExpression} (Normal).";

            // Single combined history entry to preserve order
            var historyEntry = $"[{DateTime.Now:HH:mm}] {macro.Name} - Hit: {hitResult.NormalizedExpression} = {hitResult.Total}; Dmg: {dmgResult.NormalizedExpression} = {dmgResult.Total}";
            if (hitResult.Advantage is { } advHistory)
            {
                var modeLabel = hitResult.RollMode == RollMode.Advantage ? "Adv" : "Dis";
                historyEntry += $" ({modeLabel}, kept {advHistory.KeptValue})";
            }
            RollHistory.Insert(0, historyEntry);
        }
        else if (hitResult is not null)
        {
            TotalText = $"Total: Hit {hitResult.Total}";
            RollsText = $"Rolls: Hit {string.Join(", ", hitResult.Rolls)} (modifier {hitResult.Modifier:+#;-#;0})";
            AdvantageText = hitResult.Advantage is { } adv
                ? $"{(hitResult.RollMode == RollMode.Advantage ? "Advantage" : "Disadvantage")} (hit) rolls: {string.Join(", ", adv.Rolls)} (kept #{adv.KeptIndex + 1}: {adv.KeptValue})"
                : string.Empty;
            StatusMessage = $"Macro '{macro.Name}' executed: hit {hitResult.NormalizedExpression} ({hitResult.RollMode}).";
            RollHistory.Insert(0, $"[{DateTime.Now:HH:mm}] {macro.Name} - Hit: {hitResult.NormalizedExpression} = {hitResult.Total}");
        }
        else if (dmgResult is not null)
        {
            TotalText = $"Total: Damage {dmgResult.Total}";
            RollsText = $"Rolls: Damage {string.Join(", ", dmgResult.Rolls)} (modifier {dmgResult.Modifier:+#;-#;0})";
            AdvantageText = string.Empty;
            StatusMessage = $"Macro '{macro.Name}' executed: damage {dmgResult.NormalizedExpression} (Normal).";
            RollHistory.Insert(0, $"[{DateTime.Now:HH:mm}] {macro.Name} - Dmg: {dmgResult.NormalizedExpression} = {dmgResult.Total}");
        }
        else
        {
            StatusMessage = $"Macro '{macro.Name}' expressions could not be parsed.";
        }

        // Trim history
        while (RollHistory.Count > 50)
        {
            RollHistory.RemoveAt(RollHistory.Count - 1);
        }
    }

    private void ExecuteClearHistory()
    {
        RollHistory.Clear();
        StatusMessage = "History cleared.";
    }

    private void ExecuteCopyText(string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                Clipboard.SetText(text);
                StatusMessage = "Copied to clipboard.";
            }
            catch
            {
                StatusMessage = "Unable to copy to clipboard.";
            }
        }
    }

    private void ExecuteToggleHistoryCollapsed()
    {
        IsHistoryCollapsed = !IsHistoryCollapsed;
    }

    private void ExecuteToggleMacroDeleteButtons()
    {
        ShowMacroDeleteButtons = !ShowMacroDeleteButtons;
        StatusMessage = ShowMacroDeleteButtons ? "Macro delete buttons shown." : "Macro delete buttons hidden.";
    }

    private string ComposeLastResultText()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(TotalText)) parts.Add(TotalText);
        if (!string.IsNullOrWhiteSpace(RollsText)) parts.Add(RollsText);
        if (!string.IsNullOrWhiteSpace(AdvantageText)) parts.Add(AdvantageText);
        return string.Join("\n", parts);
    }

    private string GetMacrosPath()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DnDDice");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "macros.json");
    }

    private void LoadMacros()
    {
        var path = GetMacrosPath();
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<List<Macro>>(json) ?? new List<Macro>();
                Macros.Clear();
                foreach (var m in loaded)
                {
                    if (!string.IsNullOrWhiteSpace(m.Name))
                    {
                        Macros.Add(m);
                    }
                }
                LogToFile($"Successfully loaded {loaded.Count} macros from {path}");
            }
        }
        catch (Exception ex)
        {
            LogToFile($"Error loading macros: {ex.GetType().Name}: {ex.Message}");
            // If the file is corrupted, move it aside so the app can start
            try
            {
                var badPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, "macros.bad.json");
                if (File.Exists(path))
                {
                    File.Move(path, badPath, true);
                    LogToFile($"Moved corrupted macro file to {badPath}");
                }
            }
            catch (Exception moveEx) 
            { 
                LogToFile($"Failed to move corrupted macro file: {moveEx.Message}");
            }
            Macros.Clear();
        }
    }

    private void SaveMacros()
    {
        try
        {
            var path = GetMacrosPath();
            var json = JsonSerializer.Serialize(Macros.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            LogToFile($"Successfully saved {Macros.Count} macros to {path}");
        }
        catch (Exception ex)
        {
            LogToFile($"Error saving macros: {ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}");
        }
    }

    private void ExecuteSaveMacro()
    {
        if (string.IsNullOrWhiteSpace(MacroName) || string.IsNullOrWhiteSpace(MacroHitExpression) || string.IsNullOrWhiteSpace(MacroDamageExpression))
        {
            StatusMessage = "Macro requires name, hit, and damage expressions.";
            return;
        }

        // Update if exists
        var existing = Macros.FirstOrDefault(m => string.Equals(m.Name, MacroName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.HitExpression = MacroHitExpression;
            existing.DamageExpression = MacroDamageExpression;
            existing.Category = MacroCategory;
            existing.Description = MacroDescription;
        }
        else
        {
            Macros.Add(new Macro
            {
                Name = MacroName,
                HitExpression = MacroHitExpression,
                DamageExpression = MacroDamageExpression,
                Category = MacroCategory,
                Description = MacroDescription,
            });
        }

        SaveMacros();
        StatusMessage = $"Saved macro '{MacroName}'.";
    }

    private void ExecuteDeleteMacro()
    {
        // Prefer deleting current selection; otherwise delete by MacroName
        Macro? target = SelectedMacro;
        if (target is null && !string.IsNullOrWhiteSpace(MacroName))
        {
            target = Macros.FirstOrDefault(m => string.Equals(m.Name, MacroName, StringComparison.OrdinalIgnoreCase));
        }

        if (target is null)
        {
            StatusMessage = "Select a macro or enter a macro name to delete.";
            return;
        }

        var confirm = MessageBox.Show($"Delete macro '{target.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var removed = Macros.Remove(target);
        if (removed)
        {
            SaveMacros();
            StatusMessage = $"Deleted macro '{target.Name}'.";
            MacroName = MacroHitExpression = MacroDamageExpression = string.Empty;
            MacroCategory = MacroDescription = string.Empty;
            SelectedMacro = null;
            OnPropertyChanged(nameof(CanDeleteMacro));
        }
    }

    private void ExecuteDeleteMacroItem(Macro? macro)
    {
        if (macro is null)
        {
            return;
        }

        var confirm = MessageBox.Show($"Delete macro '{macro.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var removed = Macros.Remove(macro);
        if (removed)
        {
            SaveMacros();
            StatusMessage = $"Deleted macro '{macro.Name}'.";
            OnPropertyChanged(nameof(CanDeleteMacro));
        }
    }

    private void ExecuteToggleMacroEditButtons()
    {
        ShowMacroEditButtons = !ShowMacroEditButtons;
    }

    private void ExecuteToggleMacroReorderButtons()
    {
        ShowMacroReorderButtons = !ShowMacroReorderButtons;
    }

    private void ExecuteToggleMacroDetails()
    {
        ShowMacroDetails = !ShowMacroDetails;
        StatusMessage = ShowMacroDetails ? "Macro details shown." : "Macro details hidden.";
    }

    private void ExecuteEditMacro(Macro? macro)
    {
        if (macro is not null)
        {
            SelectedMacro = macro; // This populates the editor fields automatically
            StatusMessage = $"Editing macro: {macro.Name}";
            ExecuteOpenMacroEditor();
        }
    }

    private void ExecuteClearMacroFields()
    {
        MacroName = string.Empty;
        MacroHitExpression = string.Empty;
        MacroDamageExpression = string.Empty;
        MacroCategory = string.Empty;
        MacroDescription = string.Empty;
        SelectedMacro = null;
        StatusMessage = "Macro fields cleared.";
    }

    private void ExecuteMoveMacroUp(Macro? macro)
    {
        MoveMacro(macro, -1);
    }

    private void ExecuteMoveMacroDown(Macro? macro)
    {
        MoveMacro(macro, 1);
    }

    private void MoveMacro(Macro? macro, int delta)
    {
        if (macro is null)
        {
            return;
        }

        var index = Macros.IndexOf(macro);
        if (index < 0)
        {
            return;
        }

        var newIndex = index + delta;
        if (newIndex < 0 || newIndex >= Macros.Count)
        {
            return;
        }

        Macros.Move(index, newIndex);
        SaveMacros();
        StatusMessage = $"Moved macro '{macro.Name}' {(delta < 0 ? "up" : "down")}.";
    }

    private void ExecuteImportMacros()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import Macros",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(dialog.FileName);
            var loaded = JsonSerializer.Deserialize<List<Macro>>(json) ?? new List<Macro>();
            int added = 0, updated = 0;
            foreach (var m in loaded.Where(m => !string.IsNullOrWhiteSpace(m.Name)))
            {
                var existing = Macros.FirstOrDefault(x => string.Equals(x.Name, m.Name, StringComparison.OrdinalIgnoreCase));
                if (existing is not null)
                {
                    existing.HitExpression = m.HitExpression;
                    existing.DamageExpression = m.DamageExpression;
                    existing.Category = m.Category;
                    existing.Description = m.Description;
                    updated++;
                }
                else
                {
                    Macros.Add(new Macro
                    {
                        Name = m.Name,
                        HitExpression = m.HitExpression,
                        DamageExpression = m.DamageExpression,
                        Category = m.Category,
                        Description = m.Description,
                    });
                    added++;
                }
            }

            SaveMacros();
            StatusMessage = $"Imported {added} new, updated {updated} macros.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to import macros.";
            LogToFile($"Error importing macros: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void ExecuteExportMacros()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Export Macros",
            FileName = "macros_export.json",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(Macros.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dialog.FileName, json);
            StatusMessage = $"Exported {Macros.Count} macros to {dialog.FileName}.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to export macros.";
            LogToFile($"Error exporting macros: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void ExecuteExit()
    {
        Application.Current?.Shutdown();
    }

    private void ExecuteOpenMacroEditor()
    {
        try
        {
            var owner = Application.Current?.MainWindow;
            var editor = new DiceRoller.Wpf.MacroEditorWindow
            {
                Owner = owner,
                DataContext = this
            };
            editor.ShowDialog();
        }
        catch (Exception ex)
        {
            StatusMessage = "Unable to open macro editor.";
            LogToFile($"Error opening macro editor: {ex.GetType().Name}: {ex.Message}");
        }
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

    private static void LogToFile(string message)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DnDDice",
                "macro_operations.log");

            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] {message}{Environment.NewLine}";
            File.AppendAllText(logPath, logEntry);
        }
        catch
        {
            // Silently fail to avoid cascading errors
            System.Diagnostics.Debug.WriteLine($"Failed to log macro operation: {message}");
        }
    }
}
