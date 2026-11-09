param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# Prefer the user-local .NET 10 SDK this project was set up with, then fall back
# to a dotnet installation on PATH.
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { 'dotnet' }

& $dotnet build (Join-Path $PSScriptRoot 'quick-theme.csproj') -c $Configuration --nologo
exit $LASTEXITCODE
