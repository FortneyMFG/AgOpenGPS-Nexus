#!/usr/bin/env pwsh
<#!
.SYNOPSIS
    Packs and optionally publishes the AgOpenGPS.Aog.Abstractions NuGet package.
.DESCRIPTION
    Generates a versioned NuGet package from the contracts project, places it in
    the local ./artifacts/nuget feed, and (unless -NoPush is specified) pushes
    it to the AgOpenGPS GitHub Packages feed.  CI builds infer the package
    version from tags or the current commit metadata.
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

$projectPath = Join-Path $RepoRoot "Nexus SourceCode/Aog.Abstractions/Aog.Abstractions.csproj"
if (-not (Test-Path $projectPath)) {
    throw "Contracts project '$projectPath' was not found."
}

if (-not $VersionPrefix) {
    $propsPath = Join-Path $RepoRoot "Nexus SourceCode/Directory.Build.props"
    if (Test-Path $propsPath) {
        [xml]$propsXml = Get-Content -LiteralPath $propsPath
        $VersionPrefix = $propsXml.Project.PropertyGroup.AogAbstractionsBaseVersion
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

Write-Host "Packing AgOpenGPS.Aog.Abstractions version $packageVersion" -ForegroundColor Cyan

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

$packages = Get-ChildItem -Path $artifactsDir -Filter 'AgOpenGPS.Aog.Abstractions*.nupkg' | Where-Object { $_.Name -notlike '*.snupkg' }
if ($packages.Count -eq 0) {
    throw "No package artifacts were produced in '$artifactsDir'."
}

Write-Host "Package artifacts:" -ForegroundColor Cyan
foreach ($pkg in $packages) {
    Write-Host "  $($pkg.FullName)" -ForegroundColor Green
}

$restoreProjects = @(
    'Nexus SourceCode/src/Aog.Core/Aog.Core.csproj',
    'Nexus SourceCode/src/Aog.Agio/Aog.Agio.csproj',
    'Nexus SourceCode/src/Aog.Agio.Windows/Aog.Agio.Windows.csproj'
)

foreach ($relativeProject in $restoreProjects) {
    $fullProjectPath = Join-Path $RepoRoot $relativeProject
    if (-not (Test-Path $fullProjectPath)) {
        Write-Host "Skipping missing project $relativeProject" -ForegroundColor Yellow
        continue
    }

    Write-Host "Validating package restore for $relativeProject" -ForegroundColor Cyan
    dotnet restore $fullProjectPath /p:UseLocalAogAbstractions=false
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

$symbolPackages = Get-ChildItem -Path $artifactsDir -Filter 'AgOpenGPS.Aog.Abstractions*.snupkg'
foreach ($sym in $symbolPackages) {
    Write-Host "Pushing symbol package $($sym.Name)" -ForegroundColor Cyan
    dotnet nuget push $sym.FullName --source $sourceUrl --api-key $apiKey --skip-duplicate
}
