[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$JellyfinDataDir
)

$ErrorActionPreference = 'Stop'

$dataDirectory = (Resolve-Path -LiteralPath $JellyfinDataDir).Path
$markerPath = Join-Path $dataDirectory '.media-import-test'

if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
    throw "Refusing deployment: '$dataDirectory' has no .media-import-test marker. This script only deploys to an isolated test instance."
}

& (Join-Path $PSScriptRoot 'Publish-Plugin.ps1')

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$publishDirectory = Join-Path $repoRoot 'artifacts\local'
$pluginDirectory = Join-Path $dataDirectory 'plugins\Jellyfin.Plugin.MediaImport'

New-Item -ItemType Directory -Path $pluginDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $publishDirectory '*') -Destination $pluginDirectory -Recurse -Force

Write-Host "Plugin deployed to test instance: $pluginDirectory"
Write-Host 'Restart the test Jellyfin server before testing the new build.'
