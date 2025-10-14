# Nexus Glossary

| Term | Definition |
| --- | --- |
| **Pose** | A timestamped position/orientation sample for a physical or virtual node (tractor, implement, toolbar, sensor) within the canonical coordinate reference system. |
| **PoseStream** | The ordered, canonical time series of poses and associated state deltas that all Nexus services, plugins, and replays consume. |
| **Equipment** | A configured machine profile representing a tractor, combine, sprayer, or other power unit that may host one or more implements. |
| **Implement** | A functional attachment (planter, toolbar, sprayer) mounted to equipment and containing toolbars, sections, and sensors. |
| **Toolbar** | A physical or logical boom/bar spanning multiple sections with shared lookahead, overlap, and control metadata. |
| **SectionNode** | The smallest controllable output element (row unit, nozzle, valve) with an addressable on/auto/off state. |
| **SectionGroup** | A named grouping of SectionNodes (or nested groups) that can receive aggregate commands, overlaps, or rates. |
| **Master Group** | The highest-priority SectionGroup controlling a toolbar or implement; manual overrides or safety interlocks apply here first. |
| **Layer** | A time- or session-bounded spatial dataset (coverage, rate, yield, diagnostics) registered with units, precision, and visualization metadata. |
| **Prescription** | A target layer that prescribes rates or setpoints to controllers (e.g., VR fertilizer, seeding) and feeds automation decisions. |
| **Event** | A discrete control or telemetry record representing an observed action (e.g., section on/off, obstruction detected). |
| **Opportunity** | The expected number of controllable actions in an interval (e.g., rows that should fire) used to evaluate misses/doubles. |
| **Tile** | A chunk of gridded spatial data persisted in the TileStore with shared codec/precision metadata. |
| **Cell** | The individual sample within a tile storing a value, weight, min/max, and quality metadata. |
| **Vector Log** | The append-only PoseStream/SectionState record used for deterministic replay and audit trails. |
| **Fusion** | The process of combining multiple PoseStreams or layers (sessions, implements, seasons) into a unified dataset with provenance. |
| **Provenance** | Recorded lineage describing how data was produced, transformed, and validated across plugins, sessions, and exports. |
| **Registry Hash** | A stable hash computed over layer definitions and schemas to detect mismatches between plugins, firmware, and stored data. |
