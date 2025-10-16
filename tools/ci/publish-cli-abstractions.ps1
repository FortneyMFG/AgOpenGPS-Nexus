#!/usr/bin/env pwsh
<#!
.SYNOPSIS
    Packs and optionally publishes the Nexus CLI plugin abstraction NuGet package.
.DESCRIPTION
    Builds the AgOpenGPS.Nexus.Plugin.Cli.Abstractions package, drops it into the
    local ./artifacts/nuget feed, and validates that the Nexus CLI host can
    restore against the packaged artifacts. Unless -NoPush is passed the script
    pushes the package to the AgOpenGPS GitHub Packages feed using the provided
    credentials.
#>

[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../.."),
    [string]$Configuration = 'Release',
    [string]$VersionPrefix,
    [switch]$NoPush
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $RepoRoot)) {
    throw "Repository root '$RepoRoot' was not found."
}

$projectPath = Join-Path $RepoRoot "Nexus SourceCode/src/Nexus.Plugin.Cli.Abstractions/Nexus.Plugin.Cli.Abstractions.csproj"
if (-not (Test-Path $projectPath)) {
    throw "CLI abstractions project '$projectPath' was not found."
}

if (-not $VersionPrefix) {
    $propsPath = Join-Path $RepoRoot "Nexus SourceCode/Directory.Build.props"
    if (Test-Path $propsPath) {
        [xml]$propsXml = Get-Content -LiteralPath $propsPath
        $VersionPrefix = $propsXml.Project.PropertyGroup.NexusCliAbstractionsBaseVersion
    }
}

if (-not $VersionPrefix) {
    $VersionPrefix = '0.1.0'
}

$artifactsDir = Join-Path $RepoRoot 'artifacts/nuget'
if (-not (Test-Path $artifactsDir)) {
    New-Item -ItemType Directory -Path $artifactsDir | Out-Null
}

function Get-GitShortSha {
    try {
        return (git rev-parse --short HEAD).Trim()
    }
    catch {
        return 'nogit'
    }
}

$packageVersion = $env:PACKAGE_VERSION
if (-not $packageVersion) {
    $tagRef = $env:GITHUB_REF
    if ($tagRef -and $tagRef.StartsWith('refs/tags/')) {
        $tag = $tagRef.Substring('refs/tags/'.Length)
        if ($tag.StartsWith('v')) {
            $tag = $tag.Substring(1)
        }
        if ($tag) {
            $packageVersion = $tag
        }
    }
}

if (-not $packageVersion) {
    $shortSha = Get-GitShortSha
    $runNumber = $env:GITHUB_RUN_NUMBER
    if ($runNumber) {
        $packageVersion = "$VersionPrefix-ci.$runNumber.$shortSha"
    }
    else {
        $packageVersion = "$VersionPrefix-dev.$shortSha"
    }
}

Write-Host "Packing AgOpenGPS.Nexus.Plugin.Cli.Abstractions version $packageVersion" -ForegroundColor Cyan

$packArgs = @(
    'pack',
    $projectPath,
    '-c', $Configuration,
    '/p:ContinuousIntegrationBuild=true',
    "/p:PackageVersion=$packageVersion",
    "/p:Version=$packageVersion",
    "/p:PackageOutputPath=$artifactsDir"
)

dotnet @packArgs

$packages = Get-ChildItem -Path $artifactsDir -Filter 'AgOpenGPS.Nexus.Plugin.Cli.Abstractions*.nupkg' | Where-Object { $_.Name -notlike '*.snupkg' }
if ($packages.Count -eq 0) {
    throw "No CLI abstraction package artifacts were produced in '$artifactsDir'."
}

Write-Host "Package artifacts:" -ForegroundColor Cyan
foreach ($pkg in $packages) {
    Write-Host "  $($pkg.FullName)" -ForegroundColor Green
}

$hostProject = Join-Path $RepoRoot 'Nexus SourceCode/src/Nexus.Cli.Host/Nexus.Cli.Host.csproj'
if (Test-Path $hostProject) {
    Write-Host 'Validating restore against packaged abstractions' -ForegroundColor Cyan
    dotnet restore $hostProject /p:UseLocalNexusCliAbstractions=false
}
else {
    Write-Warning 'Nexus CLI host project not found; skipping restore validation.'
}

if ($NoPush.IsPresent) {
    Write-Host 'Skipping publish because -NoPush was specified.' -ForegroundColor Yellow
    return
}

$sourceUrl = 'https://nuget.pkg.github.com/AgOpenGPS/index.json'
$apiKey = if ($env:NUGET_API_KEY) { $env:NUGET_API_KEY } elseif ($env:GITHUB_TOKEN) { $env:GITHUB_TOKEN } else { $null }

if (-not $apiKey) {
    Write-Host 'No API key found in NUGET_API_KEY or GITHUB_TOKEN; skipping push.' -ForegroundColor Yellow
    return
}

foreach ($pkg in $packages) {
    Write-Host "Pushing $($pkg.Name) to $sourceUrl" -ForegroundColor Cyan
    dotnet nuget push $pkg.FullName --source $sourceUrl --api-key $apiKey --skip-duplicate
}

$symbolPackages = Get-ChildItem -Path $artifactsDir -Filter 'AgOpenGPS.Nexus.Plugin.Cli.Abstractions*.snupkg'
foreach ($sym in $symbolPackages) {
    Write-Host "Pushing symbol package $($sym.Name)" -ForegroundColor Cyan
    dotnet nuget push $sym.FullName --source $sourceUrl --api-key $apiKey --skip-duplicate
}
