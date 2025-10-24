# Avalonia Run Modes (CompanionRemote, LocalInProc, LocalOutOfProc)

[13-ADR-001](../development/SRS/sections/1X_Platform_Foundations/13-ADR-001%20-%20Use%20Avalonia%20for%20the%20Nexus%20Desktop%20UI%20Shell.md) requires the Avalonia shell to switch between three run modes so the same UI serves as a remote companion or a full desktop host. This guide explains the configuration surface, transport wiring, and smoke tests for each mode.

## Configuration Surface

- **App settings.** [`AvaloniaShellOptions`](../../Nexus%20SourceCode/src/Aog.UI.Avalonia/Hosting/AvaloniaShellOptions.cs) binds the `Avalonia:RunMode` value from whichever `appsettings.json` ships with the UI (for example the [Core host template](../../Nexus%20SourceCode/src/Aog.Core.Host/appsettings.json)), letting operators pin defaults per deployment target.
- **Command-line override.** The [`--runMode` switch](../../Nexus%20SourceCode/src/Aog.UI.Avalonia/Program.cs) forces a mode at startup so smoke tests and CI harnesses can exercise every configuration without editing files.
- **UI toggle.** The [connection settings panel](../../Nexus%20SourceCode/src/Aog.UI.Avalonia/ViewModels/ConnectionSettingsViewModel.cs) surfaces the current mode with a dropdown. Switching prompts a restart when crossing in-proc/out-of-proc boundaries to keep transports aligned.

## Mode Behaviour

### CompanionRemote

- UI runs on a tablet/phone or desktop without Core/AgIO locally.
- Connects to remote Core and AgIO hosts over gRPC (Android/desktop) or gRPC-Web via Envoy (iOS/restricted networks).
- Discovery centre offers mDNS broadcast and manual host entry. TLS + token auth enforced.

### LocalInProc

- Core and AgIO run inside the Avalonia process.
- [`IAvaloniaRunModeService`](../../Nexus%20SourceCode/src/Aog.UI.Avalonia/Hosting/IAvaloniaRunModeService.cs) resolves transports to in-memory channels while AgIO backends attach hardware devices directly for local rigs.
- Used for desktop/laptop rigs where UI and control share resources.

### LocalOutOfProc

- UI spawns Core + AgIO as separate processes via the [launcher wiring](../../Nexus%20SourceCode/src/Aog.UI.Avalonia/Hosting/AvaloniaRunModeService.cs).
- IPC uses gRPC over Unix domain sockets/named pipes.
- Crash recovery restarts child processes and rebinds transports without closing the UI.

## Smoke Tests

1. **Mode switch regression.** Cycle through all three modes on Windows and Linux, verifying persisted settings and reconnect logic with [`nexus.ps1`](../../tools/scripts/nexus.ps1).
2. **Remote transport validation.** Run the Companion emulator against a simulated Core/AgIO pair (see [Pi/CM5 quick start](pi-sim.md)), confirming gRPC-Web fallback for iOS builds.
3. **In-proc hardware probe.** With simulated devices from the [Windows no-hardware quick start](windows-no-hw.md), ensure LocalInProc exposes GNSS/IMU feeds to the UI charts.
4. **Out-of-proc restart.** Kill the spawned Core process and confirm the UI restarts it and resubscribes to telemetry feeds.

Following this checklist keeps the Avalonia client aligned with 13-ADR-001 expectations and provides clear validation steps for release sign-off.
