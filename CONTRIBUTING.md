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
| `Program.cs` | Tray icon, menu, CLI commands, update orchestration |
| `Form1.cs` | Flyout window |
| `ThemeService.cs` | Windows registry access and theme values |
| `SettingsService.cs` | JSON settings under `%APPDATA%\Quick Theme` |
| `UpdateService.cs` | Update manifest, download, hash verification, self-update |
| `UI/` | Custom controls, palette, dialogs, renderers |
| `installer/` | Inno Setup script |
| `publish.ps1` | Single-file publish, zip, manifest, installer |
| `.github/workflows/` | CI and release automation |

There is no automated test suite yet. Please build, run the app, and manually verify
the relevant behavior before opening a pull request.

## Pull requests

- Keep changes focused and explain the user-facing behavior.
- Match the existing C# style and WinForms patterns.
- Do not commit `bin/`, `obj/`, `artifacts/`, `.vs/`, or `*.user` files.
- Update `README.md` and `CHANGELOG.md` when behavior changes.
- Mention what you tested, including Windows version and display scaling if relevant.

## Licensing

By contributing, you agree that your contributions are licensed under the
[MIT License](LICENSE).
