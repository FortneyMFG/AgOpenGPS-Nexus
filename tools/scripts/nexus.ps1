#!/usr/bin/env pwsh
param(
    [Parameter(Position = 0)]
    [string]$Command,
    [Parameter(Position = 1)]
    [string]$Target,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

function Show-Usage {
    @'
Usage: ./nexus.ps1 <command> [options]

Commands:
  run <target> [-- <args>...]   Run a Nexus host (core, agio, ui) via dotnet run.
  sim [-- <args>...]            Launch the composite simulation host.

Environment overrides:
  NEXUS_CORE_PROJECT  Relative path to the Core host .csproj.
  NEXUS_AGIO_PROJECT  Relative path to the AgIO host .csproj.
  NEXUS_UI_PROJECT    Relative path to the UI .csproj.
  NEXUS_SIM_PROJECT   Relative path to the simulation entry .csproj.
  DOTNET              dotnet executable to invoke (default: dotnet).
'@
}

if (-not $Command) {
    Show-Usage
    exit 1
}

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
$repoRoot = Resolve-Path (Join-Path $scriptDir '..' '..')

$defaults = @{
    core = 'Nexus SourceCode/src/Aog.Core.Host/Aog.Core.Host.csproj'
    agio = 'Nexus SourceCode/src/Aog.Agio.Host/Aog.Agio.Host.csproj'
    ui   = 'Nexus SourceCode/src/Aog.UI.Avalonia/Aog.UI.Avalonia.csproj'
    sim  = 'Nexus SourceCode/src/Aog.Core.SimHost/Aog.Core.SimHost.csproj'
}

function Resolve-Project {
    param([string]$Target)
    $envVar = "NEXUS_{0}_PROJECT" -f $Target.ToUpperInvariant()
    $override = [Environment]::GetEnvironmentVariable($envVar)
    if (-not [string]::IsNullOrEmpty($override)) {
        return $override
    }
    if (-not $defaults.ContainsKey($Target)) {
        throw "Unknown target '$Target'."
    }
    return $defaults[$Target]
}

function Require-Project {
    param([string]$Target)
    $projectRel = Resolve-Project -Target $Target
    $projectPath = Join-Path $repoRoot $projectRel
    if (-not (Test-Path -Path $projectPath -PathType Leaf)) {
        throw "Expected project for '$Target' at $projectPath. Override via NEXUS_${($Target.ToUpperInvariant())}_PROJECT."
    }
    return $projectPath
}

function Ensure-Dotnet {
    $dotnetCmd = if ($env:DOTNET) { $env:DOTNET } else { 'dotnet' }
    try {
        Get-Command $dotnetCmd -ErrorAction Stop | Out-Null
    } catch {
        throw "dotnet CLI not found (DOTNET=$dotnetCmd)."
    }
    return $dotnetCmd
}

function Run-Target {
    param(
        [string]$Target,
        [string[]]$Args
    )
    $dotnetCmd = Ensure-Dotnet
    $projectPath = Require-Project -Target $Target
    if ($Args -and $Args.Count -gt 0) {
        & $dotnetCmd run --project $projectPath -- @Args
    } else {
        & $dotnetCmd run --project $projectPath
    }
    exit $LASTEXITCODE
}

switch ($Command.ToLowerInvariant()) {
    'run' {
        if (-not $Target) {
            Write-Error 'Missing target for run command.'
            Show-Usage
            exit 1
        }
        switch ($Target.ToLowerInvariant()) {
            'core' { Run-Target -Target 'core' -Args $RemainingArgs }
            'agio' { Run-Target -Target 'agio' -Args $RemainingArgs }
            'ui'   { Run-Target -Target 'ui'   -Args $RemainingArgs }
            default {
                Write-Error "Unknown run target '$Target'. Expected core, agio, or ui."
                exit 1
            }
        }
    }
    'sim' {
        Run-Target -Target 'sim' -Args $RemainingArgs
    }
    'help' { Show-Usage }
    default {
        Write-Error "Unknown command '$Command'."
        Show-Usage
        exit 1
    }
}
