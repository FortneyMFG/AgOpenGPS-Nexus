#!/usr/bin/env pwsh
<#!
.SYNOPSIS
    Packages cross-runtime Nexus plugins (Pumpkin Pi HAL and Nexus CLI sample) into zip archives.
.DESCRIPTION
    Builds the Pumpkin Pi Go sidecar for the requested GOOS/GOARCH combinations and
    stages manifest + asset bundles into PackPlugin-style archives. Also compiles the
    Nexus CLI sample plugin and emits a manifest-driven zip so operators can install it
    like any other plugin bundle.
#>

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string[]]$PumpkinRuntimes = @('linux-arm64'),
    [string]$OutputRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir '..' '..')

if (-not $OutputRoot) {
    $OutputRoot = Join-Path $repoRoot 'artifacts/plugins'
}

if (-not (Test-Path $OutputRoot)) {
    New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
}

function Invoke-DotNet {
    param([string[]]$Args)
    $dotnet = if ($env:DOTNET) { $env:DOTNET } else { 'dotnet' }
    & $dotnet @Args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command '$($Args -join ' ') failed with exit code $LASTEXITCODE'"
    }
}

function Invoke-Go {
    param(
        [string[]]$Args,
        [string]$WorkingDirectory
    )
    $go = if ($env:GO) { $env:GO } else { 'go' }
    if ($WorkingDirectory) {
        Push-Location $WorkingDirectory
    }
    try {
        & $go @Args
    }
    finally {
        if ($WorkingDirectory) {
            Pop-Location
        }
    }
    if ($LASTEXITCODE -ne 0) {
        throw "go command '$($Args -join ' ') failed with exit code $LASTEXITCODE'"
    }
}

function New-StagingDirectory {
    param([string]$Prefix)
    $tempRoot = [System.IO.Path]::GetTempPath()
    $guid = [Guid]::NewGuid().ToString('N')
    $path = Join-Path $tempRoot "$Prefix-$guid"
    New-Item -ItemType Directory -Path $path -Force | Out-Null
    return $path
}

function Compress-Staging {
    param(
        [string]$StagePath,
        [string]$Destination
    )
    if (Test-Path $Destination) {
        Remove-Item -Path $Destination -Force
    }
    Compress-Archive -Path (Join-Path $StagePath '*') -DestinationPath $Destination -Force
}

# Package the Nexus CLI sample plugin as a manifest-driven zip.
$cliManifestPath = Join-Path $repoRoot 'plugins/nx-cli-sample/plugin.json'
if (-not (Test-Path $cliManifestPath)) {
    throw "Sample plugin manifest not found at $cliManifestPath"
}
$cliManifest = Get-Content -Path $cliManifestPath -Raw | ConvertFrom-Json
$cliProject = Join-Path $repoRoot 'Nexus SourceCode/src/Nexus.SamplePlugin.Cli/Nexus.SamplePlugin.Cli.csproj'
if (-not (Test-Path $cliProject)) {
    throw "Sample plugin project not found at $cliProject"
}

$cliStageRoot = Join-Path (New-StagingDirectory -Prefix 'nx-cli') 'stage'
New-Item -ItemType Directory -Path $cliStageRoot -Force | Out-Null
$cliLib = Join-Path $cliStageRoot 'lib'
$cliAssets = Join-Path $cliStageRoot 'assets'
New-Item -ItemType Directory -Path $cliLib -Force | Out-Null
New-Item -ItemType Directory -Path $cliAssets -Force | Out-Null

$cliPublish = Join-Path (Split-Path $cliStageRoot -Parent) 'publish'
Invoke-DotNet -Args @('publish', $cliProject, '-c', $Configuration, '-o', $cliPublish)
Copy-Item -Path (Join-Path $cliPublish '*') -Destination $cliLib -Recurse -Force

Copy-Item -Path $cliManifestPath -Destination (Join-Path $cliStageRoot 'manifest.json') -Force
Copy-Item -Path (Join-Path $repoRoot 'plugins/nx-cli-sample/README.md') -Destination (Join-Path $cliAssets 'README.md') -Force

$cliZipName = "{0}-{1}.zip" -f $cliManifest.id, $cliManifest.version
$cliZipPath = Join-Path $OutputRoot $cliZipName
Compress-Staging -StagePath $cliStageRoot -Destination $cliZipPath
Write-Host "Packaged Nexus CLI sample plugin => $cliZipPath" -ForegroundColor Cyan

# Package Pumpkin Pi for each requested runtime.
$pumpkinRoot = Join-Path $repoRoot 'plugins/pumpkin-pi'
$pumpkinManifestPath = Join-Path $pumpkinRoot 'manifest.json'
if (-not (Test-Path $pumpkinManifestPath)) {
    throw "Pumpkin Pi manifest not found at $pumpkinManifestPath"
}
$pumpkinManifest = Get-Content -Path $pumpkinManifestPath -Raw | ConvertFrom-Json

foreach ($rid in $PumpkinRuntimes) {
    if ($rid -notmatch '^(?<goos>[^-]+)-(?<goarch>[^-]+)$') {
        throw "Pumpkin runtime '$rid' must be formatted as <goos>-<goarch> (e.g., linux-arm64)."
    }
    $goos = $Matches['goos']
    $goarch = $Matches['goarch']

    $stageBase = New-StagingDirectory -Prefix "pumpkin-$goos-$goarch"
    $stageRoot = Join-Path $stageBase 'stage'
    New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null
    $nativeDir = Join-Path $stageRoot 'native'
    $assetDir = Join-Path $stageRoot 'assets'
    $configDir = Join-Path $assetDir 'config'
    $systemdDir = Join-Path $assetDir 'systemd'
    New-Item -ItemType Directory -Path $nativeDir -Force | Out-Null
    New-Item -ItemType Directory -Path $configDir -Force | Out-Null
    New-Item -ItemType Directory -Path $systemdDir -Force | Out-Null

    $publishBin = Join-Path $stageBase 'bin'
    New-Item -ItemType Directory -Path $publishBin -Force | Out-Null

    $oldGoos = $env:GOOS
    $oldGoarch = $env:GOARCH
    $env:GOOS = $goos
    $env:GOARCH = $goarch
    try {
        Invoke-Go -Args @('build', '-o', (Join-Path $publishBin 'pumpkin-pi'), './cmd/pumpkin-pi') -WorkingDirectory $pumpkinRoot
    } finally {
        if ($null -eq $oldGoos) {
            Remove-Item Env:GOOS -ErrorAction SilentlyContinue
        } else {
            $env:GOOS = $oldGoos
        }
        if ($null -eq $oldGoarch) {
            Remove-Item Env:GOARCH -ErrorAction SilentlyContinue
        } else {
            $env:GOARCH = $oldGoarch
        }
    }

    Copy-Item -Path (Join-Path $publishBin 'pumpkin-pi') -Destination (Join-Path $nativeDir 'pumpkin-pi') -Force
    if (Test-Path (Join-Path $pumpkinRoot 'config')) {
        Copy-Item -Path (Join-Path (Join-Path $pumpkinRoot 'config') '*') -Destination $configDir -Recurse -Force
    }
    if (Test-Path (Join-Path $pumpkinRoot 'systemd')) {
        Copy-Item -Path (Join-Path $pumpkinRoot 'systemd' '*') -Destination $systemdDir -Recurse -Force
    }
    if (Test-Path (Join-Path $pumpkinRoot 'README.md')) {
        Copy-Item -Path (Join-Path $pumpkinRoot 'README.md') -Destination (Join-Path $assetDir 'README.md') -Force
    }
    Copy-Item -Path $pumpkinManifestPath -Destination (Join-Path $stageRoot 'manifest.json') -Force

    $zipName = "{0}-{1}-{2}.zip" -f $pumpkinManifest.id, $pumpkinManifest.version, $rid
    $zipPath = Join-Path $OutputRoot $zipName
    Compress-Staging -StagePath $stageRoot -Destination $zipPath
    Write-Host "Packaged Pumpkin Pi ($rid) => $zipPath" -ForegroundColor Cyan
}
