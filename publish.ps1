param(
    [string]$Version = '',
    [string]$FeedUrl = '',
    [string]$UpdateUrl = 'quick-theme.exe',
    [string]$Notes = '',
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot

if (-not $Version) {
    $match = Select-String -Path (Join-Path $root 'quick-theme.csproj') -Pattern '<Version>([^<]+)</Version>'
    if (-not $match) {
        throw 'Could not determine the project version.'
    }

    $Version = $match.Matches[0].Groups[1].Value.Trim()
}

$parsedVersion = $null
if (-not [version]::TryParse($Version, [ref]$parsedVersion) -or $parsedVersion.Build -lt 0) {
    throw "Invalid version '$Version'. Use a numeric version such as 1.2.0."
}

$Version = $parsedVersion.ToString(3)

if (-not $Notes) {
    $Notes = "Quick Theme $Version"
}

$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { 'dotnet' }

$artifacts = Join-Path $root 'artifacts'
$publishDirectory = Join-Path $artifacts 'publish-win-x64'
$portableDirectory = Join-Path $artifacts 'portable-win-x64'

Remove-Item $publishDirectory, $portableDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $publishDirectory, $portableDirectory -Force | Out-Null

$publishArguments = @(
    'publish', (Join-Path $root 'quick-theme.csproj'),
    '-c', 'Release',
    '-r', 'win-x64',
    '--self-contained', 'true',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:EnableCompressionInSingleFile=true',
    '-p:DebugType=None',
    '-p:DebugSymbols=false',
    "-p:Version=$Version",
    "-p:AssemblyVersion=$Version.0",
    "-p:FileVersion=$Version.0",
    "-p:InformationalVersion=$Version",
    '-o', $publishDirectory,
    '--nologo'
)

if ($FeedUrl) {
    $publishArguments += "-p:UpdateFeedUrl=$FeedUrl"
}

& $dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed.'
}

Get-ChildItem $publishDirectory -Filter '*.pdb' -ErrorAction SilentlyContinue | Remove-Item -Force

$exe = Join-Path $publishDirectory 'quick-theme.exe'
if (-not (Test-Path $exe)) {
    throw 'The published executable was not found.'
}

$publishedFiles = @(Get-ChildItem $publishDirectory -File)
if ($publishedFiles.Count -ne 1) {
    throw "Expected a single-file publish, but found: $($publishedFiles.Name -join ', ')"
}

Copy-Item $exe (Join-Path $artifacts 'quick-theme.exe') -Force
Copy-Item $exe (Join-Path $portableDirectory 'quick-theme.exe') -Force
Copy-Item (Join-Path $root 'README.md') (Join-Path $portableDirectory 'README.md') -Force

$zipPath = Join-Path $artifacts "quick-theme-$Version-win-x64.zip"
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $portableDirectory '*') -DestinationPath $zipPath -CompressionLevel Optimal

$sha256 = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()
$manifest = [ordered]@{
    version = $Version
    url = $UpdateUrl
    sha256 = $sha256
    notes = $Notes
}

$manifestPath = Join-Path $artifacts 'update.json'
$manifest | ConvertTo-Json | Set-Content -Path $manifestPath -Encoding UTF8

$installerPath = $null
if (-not $SkipInstaller) {
    $isccCommand = Get-Command iscc.exe -ErrorAction SilentlyContinue
    $iscc = if ($isccCommand) { $isccCommand.Source } else { $null }

    if (-not $iscc) {
        $candidates = @(
            (Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Inno Setup 6\ISCC.exe'),
            (Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'Inno Setup 6\ISCC.exe'),
            (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
        )
        $iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    }

    if ($iscc) {
        & $iscc "/DAppVersion=$Version" "/DOutputDir=$artifacts" (Join-Path $root 'installer\quick-theme.iss')
        if ($LASTEXITCODE -ne 0) {
            throw 'Inno Setup compilation failed.'
        }

        $installerPath = Join-Path $artifacts "quick-theme-$Version-setup.exe"
    }
    else {
        Write-Warning 'Inno Setup 6 was not found; skipping the installer. Install it or pass -SkipInstaller.'
    }
}

Write-Host ''
Write-Host "Version:   $Version"
Write-Host "Executable: $exe"
Write-Host "Portable:   $zipPath"
Write-Host "Manifest:   $manifestPath"
if ($installerPath) {
    Write-Host "Installer:  $installerPath"
}
