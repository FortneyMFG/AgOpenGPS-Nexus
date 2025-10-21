# 63 — Layers Registry & Journal Contracts (Status: drafting)

## Problem statement
Define the canonical registries and journal formats that Core exposes so plugins, services, and UI shells share consistent layer definitions, provenance, and replay semantics.【F:docs/SRS/sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Contracts.md†L1-L92】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L12-L156】

## Requirements (from contributors)
- R-LAY-000 (MUST, registry governance): Maintain an authoritative Layer Registry describing IDs, schemas, and lifecycle hooks; updates require schema versioning and ADR sign-off.【F:docs/SRS/sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Contracts.md†L1-L92】【F:docs/SRS/sections/4X_Interprocess_Communications/41-O5%20-%20Versioned%20layer%20schemas%20and%20quality%20metadata.md†L32-L64】
- R-LAY-001 (MUST, provenance): Persist provenance entries (jobId, sessionId, source plugin, timestamps) with every journal append so analytics and replay flows remain traceable.【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L12-L92】【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L12-L92】
- R-LAY-002 (SHOULD, delta journals): Support delta-encoded journals with conflict resolution strategies (last-writer, merges) to reduce storage while preserving deterministic replay.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L94-L156】【F:docs/SRS/sections/9X_Frontends_Ops/96-O5%20-%20Replay-driven%20CI%20and%20rollout%20for%20layers.md†L7-L44】
- R-LAY-003 (MUST, API access): Expose gRPC APIs for registry lookup, journal append, and snapshot retrieval; clients must specify schema versions to avoid drift.【F:docs/SRS/sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Contracts.md†L52-L132】
- R-LAY-004 (SHOULD, subscription model): Provide streaming subscriptions with backpressure so UI shells and plugins can react to layer changes without polling.【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L94-L156】
- R-LAY-005 (MUST, integrity): Validate journal entries against schema, enforce ACLs per layer capability, and write to append-only logs with tamper detection to satisfy audit requirements.【F:docs/SRS/sections/9X_Frontends_Ops/95_Security_Permissions.md†L48-L156】【F:docs/SRS/sections/3X_Data_Storage/34_Backup_Retention_Archival.md†L13-L80】

## Current sentiment
Layer and journal governance underpins plugin interoperability; contributors want to lock down registry contracts before widening plugin access or enabling multi-machine editing.【F:docs/SRS/sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Contracts.md†L1-L132】【F:docs/SRS/sections/4X_Interprocess_Communications/41-O5%20-%20Versioned%20layer%20schemas%20and%20quality%20metadata.md†L32-L64】
