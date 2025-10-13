param(
    [Parameter(Mandatory = $true)]
    [string] $Output,

    [string[]] $Packages = @(),

    [string] $Profile = "",

    [switch] $Force
)

$ErrorActionPreference = 'Stop'

$resolvedOutput = [System.IO.Path]::GetFullPath($Output)
if (Test-Path $resolvedOutput) {
    if (-not $Force) {
        throw "Output directory '$resolvedOutput' already exists. Pass -Force to reuse it."
    }
} else {
    New-Item -ItemType Directory -Path $resolvedOutput | Out-Null
}

$packageDir = Join-Path $resolvedOutput 'packages'
$configDir = Join-Path $resolvedOutput 'config'

New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
New-Item -ItemType Directory -Path $configDir -Force | Out-Null

$copiedPackages = @()
foreach ($package in $Packages) {
    if (-not (Test-Path $package)) {
        throw "Package '$package' not found."
    }

    $destination = Join-Path $packageDir ([System.IO.Path]::GetFileName($package))
    Copy-Item -Path $package -Destination $destination -Force
    $copiedPackages += $destination
}

if ($Profile -and (Test-Path $Profile)) {
    Copy-Item -Path $Profile -Destination $configDir -Recurse -Force
}

$checksums = @()
foreach ($file in Get-ChildItem -Path $packageDir -File) {
    $hash = Get-FileHash -Path $file.FullName -Algorithm SHA256
    $checksums += "${($hash.Hash)}  ${($file.Name)}"
}

$checksumPath = Join-Path $resolvedOutput 'checksums.txt'
Set-Content -Path $checksumPath -Value ($checksums -join [Environment]::NewLine)

$checklist = @(
    "# Dealer Deployment Checklist",
    "",
    "1. Copy the contents of this folder to the dealer USB stick.",
    "2. Verify package checksums with `Get-FileHash` or `sha256sum`.",
    "3. Review the generated `checksums.txt` before release handoff.",
    "4. Import translated machine profiles with `legacy-tool translate`.",
    "5. Run `legacy-tool soak --seconds 30` on a bench rig to confirm transport stability.",
    "6. Archive the completed checklist for your regional coordinator."
)

Set-Content -Path (Join-Path $resolvedOutput 'dealer-checklist.md') -Value ($checklist -join [Environment]::NewLine)

Write-Host "Dealer deployment bundle created at $resolvedOutput"
if ($copiedPackages.Count -gt 0) {
    Write-Host "Copied packages:" -ForegroundColor Cyan
    $copiedPackages | ForEach-Object { Write-Host "  $_" }
}
