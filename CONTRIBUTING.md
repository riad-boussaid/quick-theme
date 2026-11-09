# Contributing to Quick Theme

Thanks for taking the time to contribute.

## Getting started

Requirements:

- Windows 10 or later
- .NET 10 SDK
- Inno Setup 6, only if you want to build the installer

```powershell
git clone https://github.com/riad-boussaid/quick-theme.git
cd quick-theme
.\build.ps1
```

Run the development build:

```powershell
.\bin\Debug\net10.0-windows\quick-theme.exe --show
```

Package the app:

```powershell
.\publish.ps1
```

## Project layout

| Path | Purpose |
| --- | --- |
| `App/` | Entry point, app context, tray controller, theme/update coordinators, hotkeys |
| `Services/` | Theme registry, settings, startup, Explorer, notifications, updates |
| `Models/` | Settings and update models |
| `UI/` | Flyout and update dialogs, palette, menu renderer, drawing helpers |
| `UI/Controls/` | Custom toggle, row, close-button, and progress-bar controls |
| `installer/` | Inno Setup script |
| `tests/QuickTheme.Tests/` | Unit tests |
| `publish.ps1` | Single-file publish, zip, manifest, installer |
| `.github/workflows/` | CI and release automation |

## Tests

```powershell
.\test.ps1
```

The test project covers settings serialization, update version comparison, CLI
argument parsing, and schedule calculation. UI behavior is still verified manually.

## Pull requests

- Keep changes focused and explain the user-facing behavior.
- Match the existing C# style and WinForms patterns.
- Do not commit `bin/`, `obj/`, `artifacts/`, `.vs/`, or `*.user` files.
- Update `README.md` and `CHANGELOG.md` when behavior changes.
- Mention what you tested, including Windows version and display scaling if relevant.

## Licensing

By contributing, you agree that your contributions are licensed under the
[MIT License](LICENSE).
