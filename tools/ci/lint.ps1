#!/usr/bin/env pwsh
<#!
.SYNOPSIS
    Performs lightweight repository lint checks for Markdown files.
.DESCRIPTION
    The Wave 0 skeleton has limited source code today, so linting focuses on
    keeping Markdown/docs tidy. The script fails the build when trailing
    whitespace is detected to keep diffs clean and reduce churn in future
    pipelines.
#>

[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..")
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $RepoRoot)) {
    throw "Repository root '$RepoRoot' was not found."
}

$allowedExtensions = @('*.md')
$excludedSegments = @('Legacy SourceCode -V6', '.git', '.github')

$files = Get-ChildItem -Path $RepoRoot -Recurse -File -Include $allowedExtensions |
    Where-Object {
        $fullPath = $_.FullName
        foreach ($segment in $excludedSegments) {
            if ($fullPath -like "*${segment}*") {
                return $false
            }
        }
        return $true
    }

$violations = @()

foreach ($file in $files) {
    $relative = Resolve-Path -Relative -Path $file.FullName
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        $lineNumber++
        if ($line -match '[\t ]+$') {
            $violations += [pscustomobject]@{
                File = $relative
                Line = $lineNumber
                Content = $line
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host 'Trailing whitespace detected:' -ForegroundColor Red
    foreach ($issue in $violations) {
        Write-Host "  $($issue.File):$($issue.Line)" -ForegroundColor Red
    }
    throw "Lint checks failed."
}

Write-Host 'Markdown lint checks passed.' -ForegroundColor Green
