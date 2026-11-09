# Changelog

All notable changes to Quick Theme are documented here.

## [1.2.0] - 2026-11-09

### Added

- Light flyout theme with System, Dark, and Light modes
- Persisted settings in `%APPDATA%\Quick Theme\settings.json`
- Update check, SHA-256-verified download, and self-update
- Update menu, About dialog, and startup update checks
- Self-contained single-file publish script
- Portable zip and per-user Inno Setup installer
- GitHub Actions build and release workflow

### Changed

- Flyout palette, menu, and custom controls now support light and dark modes
- Published builds include product/version metadata

### Fixed

- Stray pixels at the corners of custom-painted toggle controls
- Theme-change broadcasts no longer block the UI thread

## [1.1.0] - 2026-11-09

### Added

- Live refresh when Windows theme changes externally
- Global hotkeys `Ctrl+Alt+D` and `Ctrl+Alt+Q`
- Middle-click dark/light toggle
- Quick theme actions in the tray menu
- CLI commands `--dark`, `--light`, `--toggle`, and `--show`

## [1.0.0] - 2026-11-09

### Added

- Initial .NET 10 rewrite of the original Win10_Colors tray utility
- Dark flyout with app, system, and transparency toggles
- Tray icon, custom menu, single-instance handling, and Explorer restart
