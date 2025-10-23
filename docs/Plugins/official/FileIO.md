# File IO Plugin

## Overview
The File IO plugin provides import/export surfaces for agronomic data, enabling operators to exchange prescriptions, coverage, and telemetry with third-party systems. It centralises file format handlers and exposes them through both UI and CLI entry points.

## Capabilities
- `fileio.import` shared capability for ingesting external data.
- `fileio.export` shared capability for writing coverage, yield, and analytics datasets.
- Optional `cli.module` capability when contributing commands to the Nexus CLI.

## Core Integration
- `FileIoSurfaceService.cs` orchestrates import/export requests and tracks registered providers.
- `SurfaceImportProvider.cs` and `SurfaceExportProvider.cs` implement the extension points plugins use to add new formats.
- Operations are asynchronous and routed through the command bus to keep UI responsive.
- Tests should mock file system interactions and validate error handling around malformed inputs.

## UI Integration
- Supplies menu commands and dialogs for selecting files and mapping them to internal data structures.
- When packaged, the plugin registers window descriptors for import/export workflows and status indicators showing job progress.

## Dependencies
- Depends on Mapping, Variable Mapping, or other domain plugins to supply the actual serializers/deserializers for specific layers.
- Requires permissions for file system read/write; declare them in the manifest `permissions` section.

## Packaging Notes
- Bundle format icons or sample templates under `assets/formats/`.
- Provide manifest metadata describing supported MIME types so the Plugin Manager can surface capabilities to operators.

## Related Resources
- `docs/Plugins/official/VariableMapping.md` for prescription interchange.
- `docs/Plugins/tutorials/first-plugin.md` demonstrates registering CLI commands alongside UI surfaces.
- ADRs covering interoperability and data governance outline accepted formats.

