param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { 'dotnet' }

& $dotnet test (Join-Path $PSScriptRoot 'tests\QuickTheme.Tests\QuickTheme.Tests.csproj') -c $Configuration --nologo
exit $LASTEXITCODE
