#!/usr/bin/env pwsh
<#!
.SYNOPSIS
    Generates shell completion scripts for the Nexus CLI host.
.DESCRIPTION
    Invokes the `nx` host with System.CommandLine's completion directive to
    produce Bash, Zsh, and PowerShell completion files. The output is written to
    ./artifacts/nx-cli/completions/ by default.
#>

[CmdletBinding()]
param(
    [string]$OutputRoot = (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..' '..' 'artifacts/nx-cli/completions'),
    [string]$NxBinary = 'nx'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Get-Command $NxBinary -ErrorAction SilentlyContinue)) {
    throw "Unable to locate '$NxBinary'. Provide -NxBinary to point at a built host."
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

function Invoke-Completion {
    param([string]$Shell, [string]$Destination)
    $process = Start-Process -FilePath $NxBinary -ArgumentList "[suggest:$Shell]" -RedirectStandardOutput $Destination -NoNewWindow -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "nx suggestion command for $Shell failed with exit code $($process.ExitCode)"
    }
}

Invoke-Completion -Shell 'bash' -Destination (Join-Path $OutputRoot 'nx.bash')
Invoke-Completion -Shell 'zsh' -Destination (Join-Path $OutputRoot 'nx.zsh')
Invoke-Completion -Shell 'powershell' -Destination (Join-Path $OutputRoot 'nx.ps1')

Write-Host "Completions exported to $OutputRoot" -ForegroundColor Cyan
