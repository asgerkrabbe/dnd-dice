# CLAUDE.md

## What this project is
A Dungeons & Dragons dice roller. Currently a C#/.NET 8 solution; **being migrated** to a
lightweight, cross-platform, single-binary GUI app.

## Migration decisions (locked in during consultation)
- **Target stack:** Go + Fyne (cross-platform GUI, compiles to a single static binary).
- **Platforms:** Linux and Windows, single executable, no runtime dependency.
- **UI:** simple GUI (not TUI/CLI), roughly matching current WPF feature set.
- **Where:** rewrite **in this same repo**, keeping git history. Old C# stays as the
  behavior reference until the Go version reaches parity, then is removed.
- **Mostly AI-coded**, so a new language is acceptable.

## Source of truth for behavior (port these exactly)
- `README.md` — expression rules and advantage/disadvantage semantics.
- `DiceEngine.Tests/` — xUnit tests encode expected behavior; use as the port checklist.

### Dice rules to preserve
- Expression format: `NdM+K` (N count, M sides, K optional +/- modifier). `N` defaults to 1.
- Whitespace ignored; invalid input returns a friendly parse error (no crash).
- **Advantage** (d20 only): roll 2d20, keep highest, then apply modifier.
- **Disadvantage** (d20 only): roll 2d20, keep lowest, then apply modifier.
- Advantage/Disadvantage apply **only** to a single `1d20±K` / `d20±K`; all other
  expressions roll as Normal even if adv/disadv is selected.
- RNG is abstracted (`IRandomSource`) so tests can inject deterministic sequences —
  preserve this seam in Go for testability.

## Current structure (C#)
- `DiceEngine/` — parsing (`Parsing/`), RNG (`Random/`), rolling (`Rolling/`), models (`Models/`).
- `DiceRoller.Wpf/` — WPF MVVM UI (dice selection, macros, results panel, macro editor).
- `DiceEngine.Tests/` — xUnit tests for parsing and rolling.

## Environment
- **Neither Go nor `dotnet` is installed** on this machine yet. Confirm/install the
  needed toolchain before attempting to build or run either stack.

## Commands
- Current .NET (when SDK available): `dotnet build`, `dotnet test`,
  `dotnet run --project DiceRoller.Wpf`.
- Future Go (when toolchain available): `go build`, `go test ./...`;
  cross-compile via `GOOS=windows GOARCH=amd64 go build`.
