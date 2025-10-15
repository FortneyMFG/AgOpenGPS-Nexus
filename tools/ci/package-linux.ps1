#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'linux-x64',
    [string]$Project = 'Nexus SourceCode/src/Aog.UI.Avalonia/Aog.UI.Avalonia.csproj',
    [string]$OutputRoot,
    [string]$ArtifactName = 'AgOpenGPS.Nexus'
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

$expectedBinaryName = if ([string]::IsNullOrEmpty($binary.Extension)) {
    $ArtifactName
} else {
    "$ArtifactName$($binary.Extension)"
}

if ($binary.Name -ne $expectedBinaryName) {
    $renamedBinaryPath = Join-Path $binary.DirectoryName $expectedBinaryName
    Move-Item -Path $binary.FullName -Destination $renamedBinaryPath -Force
    $binary = Get-Item -Path $renamedBinaryPath
}

$artifactBase = "$ArtifactName-$Runtime"
$singleFileName = if ([string]::IsNullOrEmpty($binary.Extension)) {
    $ArtifactName
} else {
    "$ArtifactName$($binary.Extension)"
}
$singleFilePath = Join-Path $OutputRoot $singleFileName
Copy-Item -Path $binary.FullName -Destination $singleFilePath -Force

$stagingDir = Join-Path $OutputRoot 'staging'
if (Test-Path $stagingDir) {
    Remove-Item -Path $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir | Out-Null

Copy-Item -Path $singleFilePath -Destination (Join-Path $stagingDir $ArtifactName) -Force
Copy-Item -Path (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $stagingDir 'LICENSE.txt') -Force

$readmeText = @"
$ArtifactName Linux package
===========================

Contents:
- $ArtifactName (self-contained single-file publish)
- LICENSE.txt
- README.txt (this file)

Usage:
1. Extract the archive.
2. Run `chmod +x $ArtifactName` if the executable bit is not preserved.
3. Launch the app with `./$ArtifactName`.

The package targets the specified runtime identifier (RID). Use `dotnet publish` with a different `-Runtime` value if you need another platform.
"@
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
