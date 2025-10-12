# Variable Rate Layer Architecture Proposal

This document has been decomposed into the living SRS. Use it as a map for where the variable-rate work now resides:

- **Goals & Conceptual Model** → [Backend Services](SRS/sections/04_Backend_Services.md) requirement set with option [O-BE-5](SRS/options/O-BACKEND-4_LayerControllers.md) covering layer controllers and dependency injection.
- **Example Layer Catalog & Units Registry** → [Data Model & Storage](SRS/sections/08_Data_Model_Storage.md) with option [O-DATA-5](SRS/options/O-DATA-5_MetadataDrivenLayers.md) detailing catalog schema, units, and persistence.
- **PGN & Transport Expectations** → [Communications & Transports](SRS/sections/03_Comm_Transports.md) via option [O-COMM-5](SRS/options/O-COMM-5_VariableRatePGNs.md) and [Interprocess API](SRS/sections/07_Interprocess_API.md) via option [O-API-5](SRS/options/O-API-5_VersionedLayerSchemas.md).
- **Hardware & Firmware Setup** → [Hardware I/O](SRS/sections/06_Hardware_IO.md) with option [O-HW-5](SRS/options/O-HW-5_ModularLayerFirmware.md) describing discovery, presets, and throttling.
- **UI/Dashboard Behavior** → [Frontends](SRS/sections/05_Frontends.md) through option [O-FE-5](SRS/options/O-UI-5_MetadataDrivenDashboards.md) for metadata-driven visualization.
- **Diagnostics & Monitoring** → [Telemetry & Health](SRS/sections/10_Telemetry_Health.md) with option [O-TH-5](SRS/options/O-TELE-4_LayerDiagnostics.md) capturing inspector enhancements.
- **Testing & Rollout** → [Testing & CI/CD Pipelines](SRS/sections/11_Testing_CI_CDPipelines.md) via option [O-CI-5](SRS/options/O-TEST-4_LayerReplayCI.md) outlining replay suites and feature flags.
- **Plugin Hooks & Registries** → [Extensibility & Plugins](SRS/sections/12_Extensibility_Plugins.md) referencing dependency-injection hooks and layer ID governance.

See those sections for the authoritative, option-first breakdown. Future edits should happen in the SRS so requirements and comparisons stay in sync.
