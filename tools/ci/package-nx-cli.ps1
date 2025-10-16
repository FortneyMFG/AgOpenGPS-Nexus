#!/usr/bin/env pwsh
<#!
.SYNOPSIS
    Builds the Nexus CLI host and sample plugin distribution artifacts.
.DESCRIPTION
    Produces dotnet tool packages, self-contained single-file binaries for common
    RIDs, and a sample plugin bundle containing the calibrate/sniff adapter. The
    script mirrors the packaging requirements from SRS §18 so developers can
    validate changes locally before publishing official releases.
#>

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string[]]$Runtimes = @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64'),
    [string]$OutputRoot,
    [switch]$SkipTool
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
$repoRoot = Resolve-Path (Join-Path $scriptDir '..' '..')

if (-not $OutputRoot) {
    $OutputRoot = Join-Path $repoRoot 'artifacts/nx-cli'
}

$hostProject = Join-Path $repoRoot 'Nexus SourceCode/src/Nexus.Cli.Host/Nexus.Cli.Host.csproj'
if (-not (Test-Path $hostProject)) {
    throw "Nexus CLI host project not found at $hostProject"
}

$sampleProject = Join-Path $repoRoot 'Nexus SourceCode/src/Nexus.SamplePlugin.Cli/Nexus.SamplePlugin.Cli.csproj'
if (-not (Test-Path $sampleProject)) {
    throw "Sample plugin project not found at $sampleProject"
}

$dotnet = if ($env:DOTNET) { $env:DOTNET } else { 'dotnet' }

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

function Invoke-DotNet {
    param([string[]]$Args)
    & $dotnet @Args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command '$($Args -join ' ') failed with exit code $LASTEXITCODE'"
    }
}

if (-not $SkipTool) {
    $toolDir = Join-Path $OutputRoot 'tool'
    New-Item -ItemType Directory -Path $toolDir -Force | Out-Null
    $packArgs = @(
        'pack',
        $hostProject,
        '-c', $Configuration,
        '/p:ContinuousIntegrationBuild=true',
        "/p:PackAsTool=true",
        "/p:PackageOutputPath=$toolDir"
    )
    Invoke-DotNet -Args $packArgs
}

$publishRoot = Join-Path $OutputRoot 'single-file'
if (Test-Path $publishRoot) {
    Remove-Item -Path $publishRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $publishRoot | Out-Null

foreach ($rid in $Runtimes) {
    $ridDir = Join-Path $publishRoot $rid
    $publishArgs = @(
        'publish',
        $hostProject,
        '-c', $Configuration,
        '-r', $rid,
        '--self-contained', 'true',
        '-o', $ridDir,
        '/p:PublishSingleFile=true',
        '/p:IncludeNativeLibrariesForSelfExtract=true',
        '/p:EnableCompressionInSingleFile=true',
        '/p:DebugType=None',
        '/p:DebugSymbols=false'
    )
    Invoke-DotNet -Args $publishArgs
}

$sampleOut = Join-Path $OutputRoot 'sample-plugin'
if (Test-Path $sampleOut) {
    Remove-Item -Path $sampleOut -Recurse -Force
}
New-Item -ItemType Directory -Path $sampleOut | Out-Null

$samplePublish = Join-Path $sampleOut 'Adapters'
New-Item -ItemType Directory -Path $samplePublish -Force | Out-Null

$sampleArgs = @(
    'publish',
    $sampleProject,
    '-c', $Configuration,
    '-o', $samplePublish,
    '/p:BuildProjectReferences=false'
)
Invoke-DotNet -Args $sampleArgs

$manifestSource = Join-Path $repoRoot 'plugins/nx-cli-sample/plugin.json'
if (-not (Test-Path $manifestSource)) {
    throw "Sample plugin manifest not found at $manifestSource"
}
Copy-Item -Path $manifestSource -Destination (Join-Path $sampleOut 'plugin.json') -Force

Copy-Item -Path (Join-Path $repoRoot 'plugins/nx-cli-sample/README.md') -Destination (Join-Path $sampleOut 'README.md') -Force

$notes = @'
Nexus CLI Packaging Summary
===========================

Artifacts:
- dotnet tool package (tool/)
- single-file binaries (single-file/<rid>/)
- sample plugin bundle (sample-plugin/)

Use the sample plugin to validate manifest discovery and calibrate/sniff verbs:
1. Copy the sample-plugin directory into ~/.nexus/plugins/org.agopengps.nx.sample/0.1.0/
2. Run `nx sample calibrate --dry-run --offset 0.12 --gain 1.03`
3. Explore `nx plugin reflect --endpoint http://localhost:5000` once a plugin exposes the reflection service.
'@
Set-Content -Path (Join-Path $OutputRoot 'README.txt') -Value $notes -Encoding UTF8

Write-Host "Artifacts staged under $OutputRoot" -ForegroundColor Cyan
