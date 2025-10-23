# Telemetry Logging Plugin

## Overview
The Telemetry Logging plugin records high-frequency telemetry streams (pose, steer, sections, rate, plugin events) into Parquet files for replay and analytics. It ensures every field session has a durable audit trail.

## Capabilities
- `telemetry.logging` exclusive capability for writing telemetry archives.
- `telemetry.manifest` shared capability providing session metadata to other services.
- Optional `replay.source` capability when exposing logs to the replay controller.

## Core Integration
- `TelemetryLoggingCoordinator.cs` subscribes to event bus topics and writes Parquet files using file names defined in `TelemetryLogFileNames.cs`.
- `TelemetryLogManifest.cs` records session metadata and file locations so downstream tooling can discover logs without scanning the file system.
- `TelemetryLoggingOptions.cs` configures storage paths, retention, and compression settings.
- Tests in `tests/Aog.Plugins.Tests/TelemetryLoggingCoordinatorTests.cs` verify log creation, manifest integrity, and retention rules.

## UI Integration
- Exposes status indicators summarising logging state (active job, file sizes, disk usage).
- Provides dialogs in the diagnostics menu for browsing sessions and initiating downloads.
- When packaged as a zip, register command handlers for CLI exports if required.

## Dependencies
- Integrates with Job Tasks to scope recordings per session.
- Works with Replay to stream logs back into the simulation bus.
- Cooperates with the Compatibility plugin to flag insufficient disk space or missing manifests.

## Packaging Notes
- Manifest should request file system permissions and declare the storage strategy (local disk, network share).
- Include retention policy defaults under `assets/settings/telemetry.json`.

## Related Resources
- `docs/Plugins/Replay.md` for playback workflow.
- `docs/Plugins/architecture.md` describes how telemetry manifests are consumed by the host.
- `docs/Plugins/official/AutoSteer.md`, `Sections.md`, etc., all rely on telemetry logging for diagnostics.

