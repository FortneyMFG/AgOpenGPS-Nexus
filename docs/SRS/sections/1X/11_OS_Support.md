# 11 — OS Support (Status: collecting proposals)

## Problem statement
Define which operating systems we target for development and field runtime, including headless, kiosk, and multi-monitor needs.

## Requirements (from contributors)
- R-OS-000 (MUST, current-AgOpenGPS): Maintain Windows desktop runtimes for AgOpenGPS executables that ship as WinExe targets with Windows desktop tooling enabled.【F:SourceCode/AgOpenGPS.WpfApp/AgOpenGPS.WpfApp.csproj†L1-L15】【F:SourceCode/GPS/AgOpenGPS.csproj†L1-L48】
- R-OS-001 (MUST, current-AgIO): Keep AgIO’s Windows Forms host viable for serial, UDP, and CAN management on Windows hardware.【F:SourceCode/AgIO/Source/AgIO.csproj†L1-L33】
- R-OS-002 (SHOULD, current-SK21-ROC): Preserve the Windows-based deployment flow relied on by external controllers such as the SK21 rate-control stack linked from the project docs.【F:README.md†L70-L76】
- R-OS-003 (SHOULD, current-AgOpenGPS): Continue providing multi-monitor aware window placement so dashboards stay visible across displays.【F:SourceCode/GPS/Helpers/ScreenHelper.cs†L1-L30】
- R-OS-004 (SHOULD, proposed-LinuxCore): Package a Linux headless “AOG Core” service for Ubuntu/Debian with a `systemd` unit, standard file layout, and dependency management while retaining Windows builds.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L6-L20】
- R-OS-005 (COULD, proposed-LinuxCore): Offer container images and optional AppImage bundles so power users can deploy the Core or combined UI without bespoke installers.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L6-L20】
- R-OS-006 (SHOULD, cross-platform pilots): Document and validate baseline hardware capable of sustaining 60 FPS rendering (quad-core 2.0 GHz CPU or better, 8 GB RAM, GPU with 2 GB VRAM/OpenGL 3.3 support) alongside supported architectures (Windows x64, Linux x86_64, Linux ARM64 SBCs) so contributors know when to move a slice from proposal to pilot.
- R-OS-007 (SHOULD, mobile companions): Plan for Android and iOS targets that reuse the Avalonia UI with minimal conditional code by relying on gRPC (Android) and gRPC-Web (iOS) transports, plus a shared configuration surface for CompanionRemote/LocalInProc/LocalOutOfProc run modes.【F:docs/ADR/ADR-003-avalonia-ui.md†L24-L42】
- R-OS-008 (COULD, mobile AgIO): Map USB-OTG serial, Bluetooth SPP, and BLE integrations onto the same AgIO abstraction surface so Android builds can eventually host Core + AgIO locally while iOS companions stay remote-first.【F:docs/ADR/ADR-003-avalonia-ui.md†L36-L44】

## Options
- O-OS-0: Status quo — Windows 10/11 x64 primary with optional experimentation elsewhere.
- O-OS-1: Windows 10/11 (x64) primary, Linux optional.
- O-OS-2: Linux first (Ubuntu/Debian), Windows optional.
- O-OS-3: Dual-first: Windows + Linux, shared UI toolkit.
- O-OS-4: Add Android “display client” for remote UI only.
- O-OS-5: Linux headless Core on Ubuntu/Debian plus Windows/Linux frontends via APIs.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L1-L44】
- O-OS-6: .NET 8 dual-first stack with Avalonia UI and AgIO backends for Windows x64 and Linux ARM64.【F:docs/SRS/options/1X/O-STACK-1_DotNet8Avalonia.md†L1-L47】

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-OS-0 | Keeps current installers, drivers, and tooling intact | Continues Windows dependency | Deferred cross-platform progress | Current WinForms/WPF build scripts |
| O-OS-1 | Driver breadth; current user base | Locks us into Win-only APIs unless careful | Driver changes; kiosk hardening | AgOpenGPS build chain |
| O-OS-2 | Headless friendly; containerization | Driver pain (USB GNSS on some distros) | Support burden for users | AgIO Linux ports |
| O-OS-3 | Max portability | Higher CI matrix | Split testing | Keep protocol pure |
| O-OS-4 | Cheap displays | Fragmentation | Input latency | Use WebSocket telemetry |
| O-OS-5 | Decouples OS/UI choices, supports headless rigs | Requires Linux packaging expertise + service hardening | API/bridge regressions can strand Windows rigs | Linux Core packaging plan |
| O-OS-6 | Shared runtime, consistent UI, simplified plugin story | Needs Windows + Linux CI, Avalonia expertise | Backend regressions could impact hardware access on both OSes | .NET 8 + Avalonia stack |

## Evaluation criteria
Driver support, latency, deployability, developer velocity, end-user setup complexity.

## Current sentiment
- Community leans to dual-first with a strict abstraction for hardware I/O, but nobody wants to drop Windows today.
- The .NET 8 + Avalonia stack is emerging as the preferred path because it keeps Windows-first quick starts while unlocking Linux ARM64 deployments through shared AgIO backends.【F:docs/SRS/options/1X/O-STACK-1_DotNet8Avalonia.md†L1-L79】
- Interest is growing in piloting the Linux Core packaging while validating PGN compatibility before committing to a broader migration.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L21-L44】【F:docs/SRS/options/4X/O-COMM-6_PGNCompatibilityBridge.md†L1-L35】
<<<<<<<< HEAD:docs/SRS/sections/1X_Platform_Foundations/11_OS_Support.md
- The same Avalonia stack gives us a straight path to Android/iOS companions while delaying hardware integration until the run-mode strategy proves itself, keeping Windows/Linux rigs stable during the rollout.【F:docs/ADR/ADR-003-avalonia-ui.md†L24-L44】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L26-L72】
========
- The same Avalonia stack gives us a straight path to Android/iOS companions while delaying hardware integration until the run-mode strategy proves itself, keeping Windows/Linux rigs stable during the rollout.【F:docs/ADR/ADR-003-avalonia-ui.md†L24-L44】【F:docs/SRS/sections/9X/91_UI_Shell_Layout.md†L26-L72】
>>>>>>>> origin/develop:docs/SRS/sections/1X/11_OS_Support.md

## Open questions
- Which distros to support officially if we invest in Linux parity?
- How do we certify the baseline hardware targets on ARM64 devices with varying GPU stacks?
