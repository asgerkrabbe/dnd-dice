# DnD Dice Roller (WPF, .NET 8)

A minimal but extensible Dungeons & Dragons dice roller for Windows 11. The solution separates a reusable core engine from a WPF MVVM UI and includes xUnit tests for the parsing and rolling logic.

## Solution layout
- **DnDDice.sln** – solution file.
- **DiceEngine** – .NET 8 class library with parsing, RNG abstraction, and rolling logic (no UI dependencies).
- **DiceRoller.Wpf** – .NET 8 WPF desktop app targeting Windows (MVVM).
- **DiceEngine.Tests** – xUnit test project covering parsing and deterministic rolling scenarios.

## Build and run
Prerequisite: .NET 8 SDK on Windows 11.

```bash
# Restore and build everything (Debug)
dotnet build

# Run the WPF app
dotnet run --project DiceRoller.Wpf
```

## Publish self-contained for Windows 11 (win-x64)
Produces a self-contained folder you can copy to another Windows 11 machine.

```bash
dotnet publish DiceRoller.Wpf -c Release -r win-x64 --self-contained true
```

You can add `-p:PublishSingleFile=true` to emit a single-file executable if desired.

## Usage
1. Enter a dice expression such as `1d20+5`, `4d6`, or `2d8-1`.
2. Choose roll mode: **Normal**, **Advantage**, or **Disadvantage**.
3. Click **Roll** to see the total, individual dice rolls, and any advantage/disadvantage detail.

### Expression rules (v1)
- Format: `NdM + K` where `N` (count) and `M` (sides) are positive integers.
- `N` defaults to `1` (e.g., `d20` == `1d20`).
- Modifier `K` is optional and can be `+K` or `-K`.
- Whitespace is ignored.
- Invalid input returns a friendly parse error (no crash).

### Advantage/Disadvantage behavior
- Applies only to a single d20 roll with optional modifier (`1d20±K` or `d20±K`).
- Advantage: roll 2d20, keep the highest, then apply the modifier.
- Disadvantage: roll 2d20, keep the lowest, then apply the modifier.
- Other expressions (e.g., `4d6`) are rolled as **Normal** even if Advantage/Disadvantage is selected.

## WPF/.NET primer for Java OOP developers
- **Solution vs. project**: A solution (`.sln`) can host multiple projects. Here we have a class library (engine), a WPF app, and a test project.
- **XAML**: UI is declared in `MainWindow.xaml`. The code-behind (`MainWindow.xaml.cs`) only wires up the ViewModel; business logic stays out of the view.
- **DataContext & bindings**: Controls bind to properties on `MainViewModel` via `DataContext`. For example, the expression `TextBox` binds to `ExpressionText`, and the `Roll` button binds to `RollCommand`.
- **MVVM**: `MainViewModel` implements `INotifyPropertyChanged` to update the UI. Commands (`ICommand`) are provided by `RelayCommand` to keep logic out of code-behind.
- **Targeting Windows**: The WPF project targets `net8.0-windows` with `EnableWindowsTargeting` so it builds and runs on Windows 11.

## Extension points
- **Engine**: Swap RNG implementations via `IRandomSource`; extend the parser to support advanced dice syntax or macros; add richer result metadata (e.g., roll history).
- **UI**: Add presets, roll history, or macro buttons by extending the ViewModel and binding new controls. The core engine already exposes structured roll details for display.
- **Tests**: Add deterministic RNG sequences to verify new parsing rules or rolling behaviors.

## Notes
- The engine has no UI references and can be reused by other front-ends.
- Advantage/disadvantage behavior for non-d20 expressions is intentionally treated as Normal in this version.
