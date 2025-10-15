#!/usr/bin/env pwsh
<#!
.SYNOPSIS
    Runs compatibility gate tests covering protobuf contracts and plugin manifests.
.DESCRIPTION
    Executes the focused contract compatibility test suites to ensure changes to
    protobuf definitions or plugin manifest schemas are validated against their
    recorded baselines. The script is invoked from CI to block breaking changes
    to the shared Aog.Abstractions package and manifest schema without explicit
    coordination.
#>

[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..")
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $RepoRoot)) {
    throw "Repository root '$RepoRoot' was not found."
}

$tests = @(
    @{ Path = 'Nexus SourceCode/tests/Aog.Abstractions.Tests/Aog.Abstractions.Tests.csproj'; Filter = 'FullyQualifiedName~ContractCompatibilityTests' },
    @{ Path = 'Nexus SourceCode/tests/Aog.Plugins.Tests/Aog.Plugins.Tests.csproj'; Filter = 'FullyQualifiedName~ContractCompatibilityTests' },
    @{ Path = 'Nexus SourceCode/tests/Aog.Core.Tests/Aog.Core.Tests.csproj'; Filter = 'FullyQualifiedName~CapabilityRegistryDocumentationTests' }
)

foreach ($test in $tests) {
    $projectPath = Join-Path $RepoRoot $test.Path
    if (-not (Test-Path $projectPath)) {
        throw "Test project '$($test.Path)' was not found relative to '$RepoRoot'."
    }

    Write-Host "Running contract compatibility tests for $($test.Path)" -ForegroundColor Cyan
    dotnet test $projectPath --configuration Release --filter $test.Filter --verbosity minimal
}
