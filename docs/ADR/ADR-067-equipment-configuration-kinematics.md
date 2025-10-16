# ADR-067: Equipment configuration and axle-centric kinematics runtime

## Status
Drafting (target review window: 2025-05-16 week)

**Relevant Plugin(s):** Autosteer, Guidance Planner, Section Control, Mapping, Calibration Toolkit

## Context
Operators need to configure articulated, multi-steer, and tracked machines without hand-editing JSON while Core consumes a kinematic graph that respects axle geometry, hitch couplers, sensors, and mode-dependent limits. [O-HW-7](../SRS/options/O-HW-7_MultiSteerConfigurator.md) now specifies the multi-steer configurator UX, schema, and sensor catalog, including axle-first topology, steering module behaviors, calibration workflows, and validation criteria for Ackermann, slip, drift, and fail-safe profiles.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L1-L152】【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L203-L333】【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L357-L446】  [ADR-017](ADR-017-profiles-kinematics.md) established the expectation that guidance and control engines ingest richer profiles, but it stops short of defining how configuration exports feed the runtime solver or how acceptance is measured across automation modes.【F:docs/ADR/ADR-017-profiles-kinematics.md†L1-L53】  We need an executable contract that bridges the configurator exports to Core’s kinematics, pose fusion, and planner guardrails without requiring bespoke integration work per rig.

## Decision
- Adopt the axle-centric runtime described in [O-HW-7](../SRS/options/O-HW-7_MultiSteerConfigurator.md) as the canonical equipment configuration export: axles are primary nodes, drawbars encode rigid or articulated joints, and wheels attach with steering geometry metadata (including Ackermann mapping sourced from linkage sensors where present) so [ADR-017](ADR-017-profiles-kinematics.md) can compute curvature, slip, and Ackermann-corrected commands directly.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L127-L219】
- Require every exported profile to include frames/units/timebase metadata, steering module definitions with authority and latency policies, hitch/joint dynamics (including float and transport locks), sensor attachments with redundancy policies, and mode profiles with interlocks/fail-safe fallbacks as outlined in the blueprint.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L19-L126】【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L203-L333】【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L334-L446】
- Define an ingestion API in Core that validates the exported graph (single rooted tree, dependency completeness, schema/version compatibility), enforces `meta.compat.guard` requirements, exposes deterministic ingestion (`{deterministic, seed}`), and surfaces timebase/late-measurement policies to the runtime. Validation failures return namespaced error codes with severities so automation blocks are explicit.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L220-L255】
- Publish telemetry, calibration, and planner hand-off contracts that mirror the blueprint: `/machine/health`, `/planner/limits`, `/estimator/debug`, `/calibration/status`, plus capability summaries (turn radius, curvature limits, drive direction policy) and profile hashes for regression tracking.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L352-L533】
- Lock export determinism: `contentHash = SHA256(canonicalJson(profile \ calibrationBundle))` must remain stable across import/export cycles; any kinematic or sensor change requires bumping `schemaVersion` or `profileId` before ingestion will accept the profile.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L29-L35】【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L236-L244】
- Core computes per-axle capacity from per-wheel slip and publishes `κ_max` each cycle; planners must honor the advertised curvature limit when generating headland or crab trajectories.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L410-L432】
- Establish Definition of Done gates for profile-driven rigs that align with NX-414: configuration round-trips must be lossless, mode switches must meet latency/overshoot budgets, and slip/accuracy metrics must hold across representative fixtures before declaring a rig supported.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L479-L533】

## Consequences
- Guidance, section, and automation planners can rely on a uniform axle-centric model with explicit limits, reducing bespoke rig integrations and enabling deterministic simulation across articulated tractors, tracked drives, and steerable implements.
- Operators gain a guided configuration workflow with wizard + graph views that exports validated profiles, cutting onboarding time and reducing field errors from ambiguous geometry or missing sensors.
- Core must ship ingestion, validation, and telemetry surfaces alongside regression fixtures, increasing upfront engineering effort but lowering long-term support costs as rigs share the same contracts.
- Legacy presets remain compatible through migration helpers that seed axle/drawbar definitions, but they now carry schema versioning and content hashes that require coordinated updates with dealers and support teams.

## Implementation Plan
1. **Configurator export & schema tooling (NX-414).** Finalize JSON schema, validation rules, and export pipeline from the multi-steer configurator, including content hashes, calibration stamps, and compatibility guards.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L19-L126】【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L357-L446】
2. **Core ingestion service (NX-452).** Implement a loader that parses profiles, enforces Definition of Done checks, maps axle/drawbar structures into [ADR-017](ADR-017-profiles-kinematics.md) runtime types, and exposes health/limits telemetry topics. The loader must honor compatibility guards, support deterministic seeds, expose `lateMeasurementPolicy`, and emit the `KIN-###` error taxonomy. Provide deterministic fixtures that simulate articulated, tracked, and steer-cart rigs.
3. **Automation integration (NX-453).** Wire guidance planner, section arbiter, and autosteer controllers to consume curvature limits, drive-direction policies, and slip estimates from the ingestion service. Ensure mode profile toggles propagate within the required latency budgets.
4. **Calibration & validation workflows (NX-454).** Deliver the Ackermann wizard, hitch zeroing, slip sanity fixtures, and transport lock checks described in the blueprint so operators can close the loop before field deployment.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L203-L333】【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L447-L533】
5. **Documentation & presets (NX-455).** Publish operator guides, preset libraries, and support checklists that map legacy rigs into the new schema, including hardware hints and telemetry expectations for redundancy and fallback modes.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L334-L446】

## Validation
- Automated regression fixtures must confirm ≤5 cm RMS toolpoint cross-track error on flat ground and ≤10 cm RMS on 8 % sidehills without crab steering, matching the blueprint acceptance gates over ≥3 km mixed-maneuver datasets.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L489-L533】
- Mode profile transitions (road ↔ field ↔ fail_safe) must complete in <150 ms with <1° transient on dependent joints, verified across simulated articulated and steer-cart rigs.【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L505-L533】
- Profile export/import round-trips must preserve content hashes (modulo calibration stamps), and ingestion validators must reject graphs with cycles, missing sensors for enabled modules, or schema incompatibilities. Acceptance vectors include Ackermann wizard CSV loopback (<0.2° RMS residual), sidehill slip sanity (0.08–0.16 m/s with κ_max derate ≥15 %), and road→field mode flips (<150 ms, <1° transient).【F:docs/SRS/options/O-HW-7_MultiSteerConfigurator.md†L365-L380】

## References
- [O-HW-7 — Multi-steer equipment configurator primitives](../SRS/options/O-HW-7_MultiSteerConfigurator.md)
- [ADR-017 — Equipment profiles and kinematics](ADR-017-profiles-kinematics.md)
- [ADR-033 — Guidance planner and autosteer orchestration](ADR-033-guidance-planner-autosteer.md)
- [ADR-028 — Stack boundaries](ADR-028-stack-boundaries.md)
