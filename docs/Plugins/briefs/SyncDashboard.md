# Sync Dashboard & Web Companion Plugin (Planned)

The Sync Dashboard plugin provides read-only visibility into Nexus seasons, jobs, and analytics without requiring operators to expose control surfaces.

## Deployment model

- Watches mirrored `/Seasons/` folders synchronized via Google Drive, Syncthing, or other file replication tools.
- Runs as a lightweight web server (ASP.NET/Avalonia WebView) that can be hosted on-farm or in the cloud, serving dashboards to browsers and mobile devices.
- Operates in monitor-only mode; no control commands are emitted back to Core or hardware.

## Features

- Season overview dashboard with progress charts, active work orders, recent sessions, and weather snapshots.
- Field detail pages showing coverage, yield, profit, soil, and advisor recommendation overlays rendered using pre-tiled layers.
- Inventory and maintenance widgets summarizing stock levels, upcoming service tasks, and outstanding discrepancies.
- Permission filters that hide sensitive cost data unless the viewer is authorized via signed share profiles.

## Integration

- Consumes plugin-generated summaries (`season.extensions`, `job.extensions`, `inventory.rollups`, `equipment.health`) to populate charts.
- Uses signed manifests from the Plugin Catalog to ensure the web dashboard binaries align with governance policies.
- Generates shareable PDF snapshots by invoking Map Composer templates server-side for landlord or stakeholder updates.

## Security & offline behavior

- Supports read-only API tokens and optional IP allowlists to keep farm data private.
- Caches latest sync data locally so dashboards remain available when connectivity to the cab is interrupted; refresh indicators show staleness.
- Logs access events for compliance reporting when regulatory exports are shared remotely.
