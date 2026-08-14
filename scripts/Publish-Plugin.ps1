[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repoRoot 'Jellyfin.Plugin.MediaImport\Jellyfin.Plugin.MediaImport.csproj'
$outputPath = Join-Path $repoRoot 'artifacts\local'

& dotnet publish $projectPath --configuration $Configuration --output $outputPath `
    /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary

if ($LASTEXITCODE -ne 0) {
    throw "Plugin publication failed with exit code $LASTEXITCODE."
}

Write-Host "Plugin published to $outputPath"
