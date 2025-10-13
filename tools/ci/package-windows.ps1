#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$Project = 'Nexus SourceCode/src/Aog.UI.Avalonia/Aog.UI.Avalonia.csproj',
    [string]$OutputRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
$repoRoot = Resolve-Path (Join-Path $scriptDir '..' '..')

if (-not $OutputRoot) {
    $OutputRoot = Join-Path $repoRoot 'artifacts/windows'
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

$exe = Get-ChildItem -Path $publishDir -Filter '*.exe' | Sort-Object Length -Descending | Select-Object -First 1
if (-not $exe) {
    throw "No executable produced in $publishDir."
}

$artifactBase = "AgOpenGPS.Nexus-$Runtime"
$singleFilePath = Join-Path $OutputRoot "$artifactBase.exe"
Copy-Item -Path $exe.FullName -Destination $singleFilePath -Force

$stagingDir = Join-Path $OutputRoot 'staging'
if (Test-Path $stagingDir) {
    Remove-Item -Path $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir | Out-Null

Copy-Item -Path $singleFilePath -Destination (Join-Path $stagingDir 'AgOpenGPS.Nexus.exe')
Copy-Item -Path (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $stagingDir 'LICENSE.txt')

$readmeText = @'
AgOpenGPS Nexus Windows package
===============================

Contents:
- AgOpenGPS.Nexus.exe (single-file publish)
- LICENSE.txt
- README.txt (this file)

Usage:
1. Extract the archive.
2. Launch AgOpenGPS.Nexus.exe.
3. Optionally run the installer bundle to place the app under Program Files.

See INSTALLING.txt in the installer bundle for elevated install instructions.
'@
Set-Content -Path (Join-Path $stagingDir 'README.txt') -Value $readmeText -Encoding UTF8

$zipPath = Join-Path $OutputRoot "$artifactBase.zip"
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}
Compress-Archive -Path (Join-Path $stagingDir '*') -DestinationPath $zipPath

$installerDir = Join-Path $OutputRoot 'installer'
if (Test-Path $installerDir) {
    Remove-Item -Path $installerDir -Recurse -Force
}
New-Item -ItemType Directory -Path $installerDir | Out-Null

Copy-Item -Path $singleFilePath -Destination (Join-Path $installerDir 'AgOpenGPS.Nexus.exe') -Force

$installerScript = @'
<#
.SYNOPSIS
    Installs the AgOpenGPS Nexus desktop shell on Windows.

.DESCRIPTION
    Copies the packaged single-file executable into Program Files (or a custom directory)
    and writes a Start Menu shortcut. Run from an elevated PowerShell prompt.
#>
param(
    [string]$InstallDir = (Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'AgOpenGPS\Nexus')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -Path $PSScriptRoot -PathType Container)) {
    throw 'Installer must be run from the extracted archive.'
}

$packageExe = Join-Path $PSScriptRoot 'AgOpenGPS.Nexus.exe'
if (-not (Test-Path -Path $packageExe -PathType Leaf)) {
    throw "Missing packaged executable at $packageExe."
}

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item -Path $packageExe -Destination (Join-Path $InstallDir 'AgOpenGPS.Nexus.exe') -Force

try {
    $programs = Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'AgOpenGPS'
    New-Item -ItemType Directory -Force -Path $programs | Out-Null
    $shortcutPath = Join-Path $programs 'AgOpenGPS Nexus.lnk'
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = Join-Path $InstallDir 'AgOpenGPS.Nexus.exe'
    $shortcut.WorkingDirectory = $InstallDir
    $shortcut.IconLocation = $shortcut.TargetPath
    $shortcut.Save()
} catch {
    Write-Warning "Failed to create Start Menu shortcut: $($_.Exception.Message)"
}

Write-Host "AgOpenGPS Nexus installed to $InstallDir"
'@
Set-Content -Path (Join-Path $installerDir 'install.ps1') -Value $installerScript -Encoding UTF8

$installerReadme = @'
AgOpenGPS Nexus Installer
=========================

Run install.ps1 from an elevated PowerShell prompt to copy AgOpenGPS Nexus into Program Files
and create a Start Menu shortcut.

Example:

    Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
    .\install.ps1

Uninstall by removing the installation directory (default: %ProgramFiles%\AgOpenGPS\Nexus)
 and deleting the Start Menu shortcut under %ProgramData%\Microsoft\Windows\Start Menu\Programs\AgOpenGPS.
'@
Set-Content -Path (Join-Path $installerDir 'INSTALLING.txt') -Value $installerReadme -Encoding UTF8

$installerZip = Join-Path $OutputRoot "$artifactBase-installer.zip"
if (Test-Path $installerZip) {
    Remove-Item -Path $installerZip -Force
}
Compress-Archive -Path (Join-Path $installerDir '*') -DestinationPath $installerZip

Remove-Item -Path $stagingDir -Recurse -Force
Remove-Item -Path $installerDir -Recurse -Force
Remove-Item -Path $publishRoot -Recurse -Force

Write-Output "Single-file executable: $singleFilePath"
Write-Output "Zip archive: $zipPath"
Write-Output "Installer archive: $installerZip"
