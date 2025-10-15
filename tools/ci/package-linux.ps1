#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'linux-x64',
    [string]$Project = 'Nexus SourceCode/src/Aog.UI.Avalonia/Aog.UI.Avalonia.csproj',
    [string]$OutputRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
$repoRoot = Resolve-Path (Join-Path $scriptDir '..' '..')

if (-not $OutputRoot) {
    $OutputRoot = Join-Path $repoRoot 'artifacts/linux'
}

$dotnetCmd = if ($env:DOTNET) { $env:DOTNET } else { 'dotnet' }
try {
    & $dotnetCmd --version | Out-Null
} catch {
    throw "dotnet CLI not found (DOTNET=$dotnetCmd)."
}

$projectPath = Join-Path $repoRoot $Project
if (-not (Test-Path -Path $projectPath -PathType Leaf)) {
    throw "Project file not found at $projectPath. Override via -Project."
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$publishRoot = Join-Path $OutputRoot 'publish'
if (Test-Path $publishRoot) {
    Remove-Item -Path $publishRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $publishRoot | Out-Null

$publishDir = Join-Path $publishRoot $Runtime

$publishArgs = @(
    'publish',
    $projectPath,
    '-c', $Configuration,
    '-r', $Runtime,
    '--self-contained', 'true',
    '-o', $publishDir,
    '/p:PublishSingleFile=true',
    '/p:IncludeNativeLibrariesForSelfExtract=true',
    '/p:IncludeAllContentForSelfExtract=true',
    '/p:EnableCompressionInSingleFile=true',
    '/p:DebugType=None',
    '/p:DebugSymbols=false'
)

& $dotnetCmd @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$binary = Get-ChildItem -Path $publishDir -File | Where-Object { $_.Name -notmatch '\.pdb$' } | Sort-Object Length -Descending | Select-Object -First 1
if (-not $binary) {
    throw "No binary produced in $publishDir."
}

$artifactBase = "AgOpenGPS.Nexus-$Runtime"
$singleFilePath = Join-Path $OutputRoot 'AgOpenGPS.Nexus'
Copy-Item -Path $binary.FullName -Destination $singleFilePath -Force
if (-not (Test-Path -Path $singleFilePath -PathType Leaf)) {
    throw "Expected single-file binary at $singleFilePath after packaging."
}
Write-Output "Packaged single-file binary: $singleFilePath"

$stagingDir = Join-Path $OutputRoot 'staging'
if (Test-Path $stagingDir) {
    Remove-Item -Path $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir | Out-Null

Copy-Item -Path $singleFilePath -Destination (Join-Path $stagingDir 'AgOpenGPS.Nexus') -Force
Copy-Item -Path (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $stagingDir 'LICENSE.txt') -Force

$readmeText = @'
AgOpenGPS Nexus Linux package
=============================

Contents:
- AgOpenGPS.Nexus (self-contained single-file publish)
- LICENSE.txt
- README.txt (this file)

Usage:
1. Extract the archive.
2. Run `chmod +x AgOpenGPS.Nexus` if the executable bit is not preserved.
3. Launch the app with `./AgOpenGPS.Nexus`.

The package targets the specified runtime identifier (RID). Use `dotnet publish` with a different `-Runtime` value if you need another platform.
'@
Set-Content -Path (Join-Path $stagingDir 'README.txt') -Value $readmeText -Encoding UTF8

$zipPath = Join-Path $OutputRoot "$artifactBase.zip"
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}

$originalLocation = Get-Location
try {
    Set-Location $stagingDir
    if (Get-Command zip -ErrorAction SilentlyContinue) {
        & zip -r "$zipPath" * | Out-Null
    }
    else {
        Compress-Archive -Path * -DestinationPath $zipPath
    }
}
finally {
    Set-Location $originalLocation
}

Remove-Item -Path $stagingDir -Recurse -Force
Remove-Item -Path $publishRoot -Recurse -Force

Write-Output "Single-file binary: $singleFilePath"
Write-Output "Zip archive: $zipPath"
