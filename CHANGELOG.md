# Changelog

All notable changes to Quick Theme are documented here.

## [1.4.0] - 2026-11-09

### Added

- Sunrise/sunset scheduling using configurable latitude and longitude
- Configurable global hotkeys with capture and conflict feedback
- Hotkey shortcuts update in the tray menu when changed
- Tests for solar calculations and hotkey gesture parsing; the suite is now 39 tests

### Changed

- Fixed times remain the fallback when sunrise or sunset is unavailable
- Settings now include schedule mode, coordinates, and hotkey bindings

## [1.3.0] - 2026-11-09

### Added

- Daily light/dark schedule with configurable times, scope, and day selection
- Schedule pause/resume from settings and the tray menu
- Settings window for schedule, startup, update feed, and about
- Theme presets: all dark, all light, and apps follow system
- Settings link in the flyout footer
- Schedule calculation tests, bringing the suite to 27 tests

### Changed

- Reorganized the project into `App/`, `Services/`, `Models/`, and `UI/`
- Split the application context, tray menu, theme actions, update flow, CLI, and
  single-instance handling into focused classes
- Renamed the namespace to `QuickTheme` and the flyout to `ThemeFlyoutForm`
- Added unit tests for settings, version comparison, CLI parsing, and scheduling

## [1.2.1] - 2026-11-09

### Changed

- Include the MIT license in portable packages and the installer
- Refresh GitHub Actions to their current major versions

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
