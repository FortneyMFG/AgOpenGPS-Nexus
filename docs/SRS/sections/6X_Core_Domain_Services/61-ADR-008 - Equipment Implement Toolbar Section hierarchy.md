# ADR-008: Equipment → Implement → Toolbar → Section hierarchy

## Status
Drafting (target review window: 2025-10-31 week)

**Relevant Plugin(s):** Mapping, Autosteer, Section Control, Rate Control, Variable Mapping, Planter Monitor


## Context
Legacy AOG configurations model implements as flat lists of sections with limited grouping semantics. Nexus must handle multiple toolbars per implement, overlapping SectionGroups, and richer metadata to coordinate lookahead, overlap policies, and future kinematic links. A shared hierarchy ensures PoseStream, control arbitration, and UI editors reason about the same structure while providing placeholders for ADR-017 profile integration.

## Decision
- Define a canonical object model that nests equipment, implements, toolbars, and sections with stable IDs and offsets per node.
- Support overlapping SectionGroups, including master groups, and document arbitration order for conflicting commands.
- Capture toolbar-level lookahead/overlap metadata and expose configuration hooks for future kinematic integration.
- Provide schema and configuration editor updates so legacy implements can be migrated deterministically.

## Consequences
- Control semantics (ADR-015) can rely on consistent hierarchy relationships when applying overrides or resolving overlaps.
- PoseStream consumers can reference stable identifiers for toolbar/section mapping and telemetry.
- Migration tooling must normalize legacy configs into the new schema, surfacing validation errors when overlaps are invalid.

## Amendment — 2025 architecture refresh (NX-190)

- Equipment, implement, and toolbar profiles now reference session snapshots defined in [ADR-041 — Job Sessions](ADR-041_JobSessions.md). Each session capture stores the applied configuration so replay, profit, and genetics plugins can align telemetry with the hardware state used during that outing.
- [ADR-017 — Profiles & Kinematics](ADR-017-profiles-kinematics.md) inherits these session pointers to surface profile history in Device Manager and to seed future kinematic planners.
- The lifecycle bus publishes `onSessionStart`/`onSessionEnd` events with resolved implement IDs so controllers, analytics, and report builders can attach provenance without re-querying the equipment tree.

## Governance Updates
- **Migration playbook.** A scripted converter ingests V5/V6 configurations, outputs diff reports (group priority, offsets, dependencies), and highlights operator-visible changes. Upgrades require capturing these reports and attaching them to release notes.
- **Versioned schemas.** Equipment hierarchy definitions now carry semantic versions, with compatibility gates in the registry that reject edits lacking migration metadata or unit tests covering downgrade paths.
- **Operator acceptance.** UI editors surface validation warnings sourced from the same schema validators, and human QA signs off on representative rigs each release cycle.

## Validation
- JSON schema validation must round-trip at least 30 representative V5/V6 implement configurations without structural diffs beyond expected ID normalization.
- Overlapping SectionGroup arbitration tests must demonstrate deterministic master override order with ≤ 50 ms resolution latency under concurrent commands.
- Configuration editor UX must reject invalid overlap definitions and surface contextual guidance covered by automated UI tests.

## References
- [Interprocess API requirements](../SRS/sections/4X_Interprocess_Communications/41_Inter_Application_API.md)
- [Control & automation requirements](../SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md)
- [ADR-007: PoseStream and SectionState architecture](ADR-007-posestream-sectionstate-architecture.md)
- [ADR-015: Section control & grouping semantics](ADR-015-section-control-grouping-semantics.md)
