param(
    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$InstallerUrl,

    [Parameter(Mandatory)]
    [string]$InstallerSha256,

    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'manifests'),

    [string]$ReleaseDate = (Get-Date -Format 'yyyy-MM-dd')
)

$ErrorActionPreference = 'Stop'

$packageId = 'RiadBoussaid.QuickTheme'
$manifestVersion = '1.6.0'
$repositoryUrl = 'https://github.com/riad-boussaid/quick-theme'
$releaseTag = "v$Version"
$targetDirectory = Join-Path $OutputDirectory "r\RiadBoussaid\QuickTheme\$Version"

New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null

$versionManifest = @"
PackageIdentifier: $packageId
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: $manifestVersion
"@

$localeManifest = @"
PackageIdentifier: $packageId
PackageVersion: $Version
PackageLocale: en-US
Publisher: Riad Boussaid
PublisherUrl: https://github.com/riad-boussaid
PublisherSupportUrl: $repositoryUrl/issues
PackageName: Quick Theme
PackageUrl: $repositoryUrl
License: MIT
LicenseUrl: $repositoryUrl/blob/main/LICENSE
Copyright: Copyright (c) 2026 Riad Boussaid
ShortDescription: A lightweight Windows tray utility for switching the Windows light and dark appearance.
Description: >-
  Quick Theme lives in the Windows notification area and switches system and app
  light/dark themes, transparency, and scheduled appearance changes. It includes
  sunrise/sunset scheduling, configurable hotkeys, presets, and self-updates.
Moniker: quick-theme
Tags:
  - windows
  - dark-mode
  - light-dark-theme
  - tray
  - winforms
  - dotnet
ReleaseNotes: Quick Theme $Version
ReleaseNotesUrl: $repositoryUrl/releases/tag/$releaseTag
ManifestType: defaultLocale
ManifestVersion: $manifestVersion
"@

$installerManifest = @"
PackageIdentifier: $packageId
PackageVersion: $Version
Platform:
  - Windows.Desktop
MinimumOSVersion: 10.0.0.0
InstallerType: inno
Scope: user
InstallModes:
  - interactive
  - silent
  - silentWithProgress
UpgradeBehavior: install
ReleaseDate: $ReleaseDate
Installers:
  - Architecture: x64
    InstallerUrl: $InstallerUrl
    InstallerSha256: $InstallerSha256
    InstallerSwitches:
      Silent: /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
      SilentWithProgress: /SILENT /SUPPRESSMSGBOXES /NORESTART
ManifestType: installer
ManifestVersion: $manifestVersion
"@

Set-Content -Path (Join-Path $targetDirectory "$packageId.yaml") -Value $versionManifest -Encoding utf8
Set-Content -Path (Join-Path $targetDirectory "$packageId.locale.en-US.yaml") -Value $localeManifest -Encoding utf8
Set-Content -Path (Join-Path $targetDirectory "$packageId.installer.yaml") -Value $installerManifest -Encoding utf8

Write-Host "Generated manifests in $targetDirectory"
