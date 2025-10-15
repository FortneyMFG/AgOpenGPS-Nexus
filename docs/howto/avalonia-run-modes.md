# Avalonia Run Modes (CompanionRemote, LocalInProc, LocalOutOfProc)

ADR-003 requires the Avalonia shell to switch between three run modes so the same UI serves as a remote companion or a full desktop host. This guide explains the configuration surface, transport wiring, and smoke tests for each mode.

## Configuration Surface

- **App settings.** `appsettings.json` exposes `RunMode` with values `CompanionRemote`, `LocalInProc`, and `LocalOutOfProc`. Builds default to `CompanionRemote` on Android/iOS and `LocalInProc` on desktop.
- **Command-line override.** `--runMode=<Mode>` flag forces a mode at startup. Useful for QA automation.
- **UI toggle.** The connection/settings panel surfaces the current mode with a dropdown. Switching prompts a restart when crossing in-proc/out-of-proc boundaries.

## Mode Behaviour

### CompanionRemote

- UI runs on a tablet/phone or desktop without Core/AgIO locally.
- Connects to remote Core and AgIO hosts over gRPC (Android/desktop) or gRPC-Web via Envoy (iOS/restricted networks).
- Discovery centre offers mDNS broadcast and manual host entry. TLS + token auth enforced.

### LocalInProc

- Core and AgIO run inside the Avalonia process.
- DI container resolves `ICoreTransport` to an in-memory channel while AgIO backends attach hardware devices directly.
- Used for desktop/laptop rigs where UI and control share resources.

### LocalOutOfProc

- UI spawns Core + AgIO as separate processes via the Launcher service.
- IPC uses gRPC over Unix domain sockets/named pipes.
- Crash recovery restarts child processes and rebinds transports without closing the UI.

## Smoke Tests

1. **Mode switch regression.** Cycle through all three modes on Windows and Linux, verifying persisted settings and reconnect logic.
2. **Remote transport validation.** Run the Companion emulator against a simulated Core/AgIO pair, confirming gRPC-Web fallback for iOS builds.
3. **In-proc hardware probe.** With simulated devices, ensure LocalInProc exposes GNSS/IMU feeds to the UI charts.
4. **Out-of-proc restart.** Kill the spawned Core process and confirm the UI restarts it and resubscribes to telemetry feeds.

Following this checklist keeps the Avalonia client aligned with ADR-003 expectations and provides clear validation steps for release sign-off.
