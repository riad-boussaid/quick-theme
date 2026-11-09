#define AppName "Quick Theme"

#ifndef AppVersion
  #define AppVersion "1.2.0"
#endif

#ifndef OutputDir
  #define OutputDir "..\artifacts"
#endif

[Setup]
AppId={{7C2E1D9A-4F3B-4A76-9E2D-2B5A8C1F6D34}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Quick Theme
DefaultDirName={localappdata}\Programs\Quick Theme
DefaultGroupName=Quick Theme
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=quick-theme-{#AppVersion}-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\quick-theme.exe
SetupIconFile=..\icons\themes-256-black.ico
LicenseFile=..\LICENSE
CloseApplications=yes
RestartApplications=no

[Files]
Source: "..\artifacts\publish-win-x64\quick-theme.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Icons]
Name: "{autoprograms}\Quick Theme"; Filename: "{app}\quick-theme.exe"
Name: "{autodesktop}\Quick Theme"; Filename: "{app}\quick-theme.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\quick-theme.exe"; Description: "Launch Quick Theme"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\reg.exe"; Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v QuickTheme /f"; Flags: runhidden; RunOnceId: "RemoveQuickThemeStartup"
