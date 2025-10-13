#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [ValidateSet('nightly', 'beta', 'release')]
    [string]$Channel = 'nightly',
    [string]$Version = '0.0.0-dev',
    [string]$Configuration = 'Release',
    [string]$SigningTool = $env:SIGNING_TOOL,
    [string]$CertificatePath = $env:SIGNING_CERT,
    [string]$CertificatePassword = $env:SIGNING_CERT_PASSWORD,
    [string]$TimestampUrl = 'http://timestamp.digicert.com',
    [switch]$SkipPackaging
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
$repoRoot = Resolve-Path (Join-Path $scriptDir '..' '..')

if (-not $SigningTool) {
    $SigningTool = if ($IsWindows) { 'signtool.exe' } else { 'signtool' }
}

function Invoke-CodeSign {
    param(
        [string]$FilePath
    )

    if (-not $CertificatePath) {
        Write-Verbose "No certificate provided; skipping signing for $FilePath."
        return
    }

    if (-not (Get-Command $SigningTool -ErrorAction SilentlyContinue)) {
        Write-Warning "Signing tool '$SigningTool' not found. $FilePath will not be signed."
        return
    }

    $arguments = @('sign', '/fd', 'sha256', '/tr', $TimestampUrl, '/td', 'sha256', '/v')
    if ($CertificatePath) { $arguments += @('/f', $CertificatePath) }
    if ($CertificatePassword) { $arguments += @('/p', $CertificatePassword) }
    $arguments += $FilePath

    Write-Host "Signing artifact: $FilePath"
    & $SigningTool @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Signing tool exited with code $LASTEXITCODE for $FilePath."
    }
}

function Copy-WithHash {
    param(
        [IO.FileInfo]$File,
        [string]$Destination,
        [System.Collections.Generic.List[object]]$Manifest
    )

    $targetPath = Join-Path $Destination $File.Name
    Copy-Item -Path $File.FullName -Destination $targetPath -Force
    $hash = (Get-FileHash -Path $targetPath -Algorithm SHA256).Hash
    $Manifest.Add([pscustomobject]@{
        Name = $File.Name
        Sha256 = $hash
        RelativePath = [IO.Path]::GetRelativePath($repoRoot, $targetPath)
    })
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$releaseRoot = Join-Path $repoRoot "artifacts/release/$Channel/$timestamp"
New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null

$windowsOutput = Join-Path $repoRoot 'artifacts/windows'
$linuxOutput = Join-Path $repoRoot 'artifacts/linux'

if (-not $SkipPackaging) {
    Write-Host "Packaging Windows artifacts..."
    & pwsh (Join-Path $repoRoot 'tools/ci/package-windows.ps1') -Configuration $Configuration -OutputRoot $windowsOutput
    if ($LASTEXITCODE -ne 0) {
        throw "Windows packaging failed with exit code $LASTEXITCODE."
    }

    if (Get-Command bash -ErrorAction SilentlyContinue) {
        Write-Host "Packaging Raspberry Pi artifacts..."
        & bash (Join-Path $repoRoot 'tools/packaging/pi/build-deb.sh') --version $Version --configuration $Configuration --runtime linux-arm64 --output $linuxOutput
        if ($LASTEXITCODE -ne 0) {
            throw "Pi packaging failed with exit code $LASTEXITCODE."
        }
    }
    else {
        Write-Warning "bash not available; skipping Pi packaging."
    }
}

$manifestEntries = [System.Collections.Generic.List[object]]::new()

$windowsRelease = Join-Path $releaseRoot 'windows'
$linuxRelease = Join-Path $releaseRoot 'linux'
New-Item -ItemType Directory -Path $windowsRelease -Force | Out-Null
New-Item -ItemType Directory -Path $linuxRelease -Force | Out-Null

if (Test-Path $windowsOutput) {
    Get-ChildItem -Path $windowsOutput -Filter 'AgOpenGPS.Nexus-*' | ForEach-Object {
        if ($_.Extension -ieq '.exe') {
            Invoke-CodeSign -FilePath $_.FullName
        }
        Copy-WithHash -File $_ -Destination $windowsRelease -Manifest $manifestEntries
    }
}
else {
    Write-Warning "Windows packaging output not found at $windowsOutput."
}

if (Test-Path $linuxOutput) {
    Get-ChildItem -Path $linuxOutput -Filter 'nexus-pi_*.deb' | ForEach-Object {
        Copy-WithHash -File $_ -Destination $linuxRelease -Manifest $manifestEntries
    }
}

$manifest = [pscustomobject]@{
    Channel = $Channel
    Version = $Version
    Timestamp = $timestamp
    Artifacts = $manifestEntries
}

$manifestPath = Join-Path $releaseRoot 'manifest.json'
$manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host "Release artifacts staged under $releaseRoot"
Write-Host "Manifest: $manifestPath"
