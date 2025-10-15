# Option O-STACK-1 — .NET 8 + Avalonia Cross-Platform Stack (Status: draft)

## Summary
Adopt a unified C#/.NET 8 stack that runs identically on Windows x64 and Linux ARM64 (Raspberry Pi/Compute Module 5). All core services, plugins, simulation, and UI components share the same language and runtime. Platform-specific hardware access is isolated inside swappable AgIO backends that surface a consistent gRPC API to the rest of the system. Avalonia delivers a single desktop UI codebase for both operating systems, with the option to host the same gRPC client inside a WinUI or WPF shell for Windows-first polish.

## Architecture highlights
- **Language/runtime**: C# across Core, AgIO, plugins, simulation, and UI targeting .NET 8 for cross-platform execution.
- **Process boundaries**: AgIO becomes a gRPC host exposing protobuf contracts published through a shared `Aog.Abstractions` NuGet package. Core services, plugins, and the UI consume only these contracts and never access serial, SocketCAN, or GPIO APIs directly.
- **Service layout**:
  - `Aog.Abstractions` — protobuf definitions and shared C# interfaces for GNSS, IMU, CAN, sections, and configuration.
  - `Aog.Core` — headless guidance engine that talks to AgIO via gRPC streams.
  - `Aog.Agio` — host process that loads one backend (simulation, Windows, or Linux) and exposes unified device services.
  - `Aog.Plugins` — managed plugin catalog (auto-steer, sections, simulation, replay) loaded through manifests and `AssemblyLoadContext`.
  - `Aog.UI.Avalonia` — desktop UI consuming the same gRPC contracts on Windows and Linux.
- **Simulation**: `Agio.Sim` backend injects virtual GNSS/IMU/CAN feeds so Core and UI behave identically in replay or training scenarios. A replay plugin rehydrates Parquet/CSV logs on any platform.
- **Composite sim fabric**: Core hosts the SimClock (fixed-step, rate/seekable), SimBus (typed pub/sub + last value), and SourceRouter (hardware vs. sim vs. replay priorities) so every plugin simulator and hardware input shares one authoritative timeline.
- **GPS abstraction**: A single `IPositionSource` interface with providers for NMEA/UBX serial ports, gpsd, Windows Location, and TCP/UDP streams. AgIO policy selects the best available provider and exposes a unified GNSS stream over gRPC.
- **Backends**:
  - `Agio.Windows` — wraps COM ports, Windows Location API, and optional vendor CAN SDKs (PCAN, Kvaser).
  - `Agio.Linux` — uses SocketCAN, gpsd, serial NMEA/UBX, libgpiod, and System.Device bindings for SPI/I²C/GPIO.
  - `Agio.Sim` — software-only backend for simulation and replay.
- **Packaging**: Ship Windows-first installers that auto-scan GPS via COM ports or Windows Location. Provide a matching Pi/CM5 image that bundles the Linux backend and Avalonia UI, sharing the same configuration profile.

## NuGet and tooling
Leverage `Grpc.Net.Client`, `Grpc.AspNetCore`, `Google.Protobuf`, `Microsoft.Extensions.Hosting`, `Serilog`, `System.IO.Ports`/`RJCP.SerialPortStream`, `SocketCANSharp`, `System.Device.Gpio`, `Iot.Device.Bindings`, and Avalonia-compatible rendering libraries such as Mapsui or SkiaSharp.

## Benefits
- Reuses existing C# community expertise while enabling rapid plugin development.
- Keeps Core logic, simulation, plugins, and UI identical across Windows and Linux deployments.
- Contains platform-specific complexity inside small AgIO backends, simplifying testing and packaging.
- Provides a clear migration path: operators start on Windows with auto-detected GPS, then move to Pi/CM5 without rewriting workflows.
- Enables deterministic multi-plugin simulation (including hardware-in-the-loop overrides) without forking Core logic or duplicating routing rules.

## Considerations
- Requires disciplined API versioning across gRPC contracts and plugin manifests.
- Demands CI coverage for both Windows and Linux (including ARM64) to ensure identical behavior.
- Windows-native UI polish may still require optional host shells (WinUI/WPF) for advanced integrations.
- Hardware vendors must provide .NET-friendly SDKs or gRPC shims to integrate with the AgIO abstraction layer.

## References
- [Section 01 — OS Support](../sections/01_OS_Support.md)
- [Section 02 — Framework & UI](../sections/02_Framework_UI.md)
- [Section 07 — Interprocess API](../sections/07_Interprocess_API.md)

## Related ADRs
- [ADR-001 — Adopt .NET 8 C# Stack](../../ADR/ADR-001-dotnet8-runtime.md)
- [ADR-003 — Avalonia UI](../../ADR/ADR-003-avalonia-ui.md)
- [ADR-028 — Stack Boundaries](../../ADR/ADR-028-stack-boundaries.md)

## Validation hooks
- Simulation plugin exercises GNSS/IMU/section flows without hardware.
- Replay plugin reuses unified gRPC contracts to validate new telemetry features against historical logs.
- Shared NuGet package enables contract linting and compatibility testing before releases.
- Composite simulation clock/bus APIs guarantee replay parity across Windows/Linux and keep plugin-provided fake data consistent with hardware override priorities.
