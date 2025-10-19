# ISOBUS Bridge Plugin

## Overview
The ISOBUS plugin bridges Nexus with ISO 11783 task controllers, enabling section and rate commands to flow to compliant implements. It manages PGNs, performs handshakes, and routes messages between AgIO transports and the plugin bus.

## Capabilities
- `hardware.isobus` exclusive capability for driving ISOBUS task controllers.
- `telemetry.isobus` shared capability so other plugins can observe ISOBUS traffic.
- Optional `diagnostics` capability for surfacing bus health metrics.

## Core Integration
- `IsobusRouter.cs` routes PGNs between the host transport and plugin consumers.
- `IsobusHandshakeManager.cs` coordinates VT/TC handshakes and session setup.
- `IsobusMessage.cs` encapsulates message parsing/encoding; `IsobusPgns.cs` tracks PGN constants.
- `IsobusDiagnostics.cs` produces health snapshots for telemetry and UI.
- Tests must cover handshake failure modes and PGN encoding to prevent regressions.

## UI Integration
- Supplies diagnostics dialogs (e.g., `udp_status_dialog`) and status indicators for bus state.
- Provides toolbar/menu commands for reconnecting or toggling diagnostic traces.
- When packaged, ensure dialogs are registered via `IWindowProvider` and diagnostic blocks via `IBlockProvider`.

## Dependencies
- Depends on AgIO transports for physical communication channels.
- Integrates with Sections and Variable Rate plugins to translate agronomic commands into ISOBUS messages.

## Packaging Notes
- Manifest should request serial/network permissions depending on transport type.
- Include DBC/PGN metadata under `assets/profiles/` for OEM-specific mappings when required.

## Related Resources
- `docs/plugins/TelemetryLogging.md` for capturing ISOBUS telemetry.
- `docs/ADR/ADR-006-aog-link-mcu-communications.md` details the broader communications architecture.
- Existing ISOBUS bridge brief under `docs/plugins/IsobusBridge.md` provides historical context.

