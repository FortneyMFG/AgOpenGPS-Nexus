# 61-ADR-067 — Equipment Configuration and Axle-Centric Kinematics Runtime

*(Status: Proposed)*

**Authors:** Nexus Team (Codex)
**Reviewers:** Kinematics Runtime Working Group
**Created:** 2025-10-20
**Last Updated:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Supersedes:** —
**Superseded by:** —
**Related SRS:** `61_Kinematics_Pose_Fusion.md`
**Related Options:** `61-O2`, `61-O3`

---

## 1) Context

Operators need to configure articulated, multi-steer, and tracked machines without hand-editing
JSON while Core consumes a kinematic graph that respects axle geometry, hitch couplers, sensors,
and mode-dependent limits. The multi-steer configurator blueprint defines UX, schema, and sensor
catalog expectations, but an executable contract is required to bridge exports into Core’s
kinematics runtime, pose fusion, and planner guardrails without bespoke integration per rig.【F:docs/sections/6X_Core_Domain_Services/61-ADR-067 - Equipment configuration and axle-centric kinematics runtime.md†L9-L32】

```mermaid
flowchart TD
  Configurator --> ProfileExport
  ProfileExport --> CoreIngestion
  CoreIngestion --> KinematicsRuntime
  CoreIngestion --> Telemetry
```

---

## 2) Decision

Adopt the axle-centric runtime from the multi-steer configurator as the canonical equipment export.
Axles form primary nodes, drawbars encode joints, and wheels attach with steering geometry metadata
so kinematics compute curvature, slip, and Ackermann-corrected commands. Require exports to include
frames/units/timebase metadata, steering module definitions, hitch/joint dynamics, sensor attachments,
mode profiles, and deterministic content hashes. Define a Core ingestion API that validates the graph,
enforces compatibility guards, exposes deterministic ingestion, and surfaces timebase/late-measurement
policies. Publish telemetry and planner hand-off contracts (`/machine/health`, `/planner/limits`,
`/estimator/debug`, `/calibration/status`) alongside capability summaries and profile hashes.【F:docs/sections/6X_Core_Domain_Services/61-ADR-067 - Equipment configuration and axle-centric kinematics runtime.md†L32-L84】

### Decision Summary

* **Scope:** Configurator exports, Core ingestion service, telemetry and planner contracts.
* **Boundary:** Does not define hardware calibration tooling beyond schema requirements.
* **Implementation Level:** Design + runtime implementation with deterministic ingestion and telemetry surfaces.

---

## 3) Consequences

**Positive Impacts:**

* Guidance, section, and automation planners rely on uniform axle-centric models with explicit limits,
  reducing bespoke integrations and enabling deterministic simulation across articulated rigs.
* Operators gain guided configuration workflows with validation and content hashes, improving onboarding and support.
* Telemetry surfaces expose curvature limits, health, and calibration status for diagnostics and automation governance.【F:docs/sections/6X_Core_Domain_Services/61-ADR-067 - Equipment configuration and axle-centric kinematics runtime.md†L86-L130】

**Negative / Mitigated Impacts:**

* Core must ship ingestion, validation, and telemetry services, increasing engineering effort — mitigated by shared fixtures and automation.
* Legacy presets require migration helpers and coordination with dealers — mitigated via scripted conversions and documentation.

**Follow-up Actions:**

* Finalize JSON schema, validation rules, and export pipeline including content hashes and calibration stamps.
* Implement Core loader enforcing Definition of Done checks, compatibility guards, deterministic seeds, and error taxonomy.
* Integrate planners and controllers with curvature limits, drive-direction policies, and slip estimates.
* Deliver calibration workflows (Ackermann wizard, hitch zeroing, slip checks) and publish operator guides plus preset libraries.【F:docs/sections/6X_Core_Domain_Services/61-ADR-067 - Equipment configuration and axle-centric kinematics runtime.md†L130-L188】

---

## 4) Rationale

Axle-centric exports provide deterministic geometry for Core without bespoke adapters. Alternatives that
kept legacy presets or partial schemas failed to capture steering authority, latency, and slip metadata
needed for automation safety and planner guardrails.

---

## 5) Alternatives Considered

| Option | Summary | Reason Not Selected |
|--------|---------|---------------------|
| Legacy preset conversion | Continue manual JSON editing per rig. | Error-prone, lacks deterministic ingestion and telemetry. |
| Minimal profile export | Export limited geometry without axle focus. | Cannot derive curvature limits or slip budgets reliably. |
| Plugin-specific ingestion | Allow each planner to parse exports. | Duplicates logic and breaks determinism across services. |

---

## 6) Implementation Notes

* Ingestion validates single rooted tree, dependency completeness, schema compatibility, and `meta.compat.guard` requirements.
* Deterministic ingestion provides `{deterministic, seed}` parameters and publishes profile hashes for regression tracking.
* Telemetry topics mirror configurator blueprint and include capability summaries for planners and diagnostics.
* Definition of Done gates demand lossless round-trips, latency/overshoot budgets, and slip/accuracy metrics across fixtures.

### 6.1 Operational Playbook

The runtime ships with an end-to-end workflow that operators and engineers follow when onboarding axle-centric profiles:

1. **Profile ingestion.** `AxleCentricProfileLoader` canonicalises JSON exports, enforces schema and compatibility guards, injects deterministic seeds, and emits `KIN-###` errors with health telemetry so support can confirm the active configuration without collecting files manually.【F:docs/development/SRS/appendices/samples/examples/articulated-tractor.v1.json†L1-L32】
2. **Automation integration.** `AxleAutomationIntegrator` pushes curvature limits, slip budgets, drive-direction policies, and deterministic seeds into planners and controllers via `AutomationModeSnapshot`, avoiding duplicated Ackermann maths in downstream modules.【F:docs/development/SRS/sections/8X_Guidance/81-ADR-033 - Guidance planner and autosteer orchestration.md†L19-L68】
3. **Calibration workflows.** `CalibrationWorkflows` implements Ackermann validation, hitch zeroing, slip sanity checks, and transport lock verification, returning typed records that UI/CLI tooling can surface consistently. Acceptance budgets mirror the verification gates captured in §7.【F:docs/development/SRS/sections/6X_Core_Domain_Services/61-ADR-067 - Equipment configuration and axle-centric kinematics runtime.md†L188-L210】
4. **Documentation & presets.** Preset bundles under `artifacts/presets/axle-centric/` capture canonical hashes, deterministic seeds, and per-mode limits so simulation fixtures and fleet rollouts stay aligned. Support references `/machine/health`, `/planner/limits`, and `/calibration/status` telemetry topics when troubleshooting rigs.

#### 6.1.1 Sample Configuration Snippets

Profiles exported from the configurator include vehicle geometry, implement metadata, and controller settings that map directly to runtime services.

```json
{
  "vehicle": {
    "name": "John Deere 6R",
    "type": "tractor",
    "wheelbase": 2.8,
    "turnRadius": 4.5,
    "antennaOffset": { "y": 1.5 }
  }
}
```

```json
{
  "implement": {
    "type": "planter",
    "width": 12.0,
    "sections": [
      { "id": 1, "width": 3.0, "offset": -4.5 },
      { "id": 2, "width": 3.0, "offset": -1.5 },
      { "id": 3, "width": 3.0, "offset": 1.5 },
      { "id": 4, "width": 3.0, "offset": 4.5 }
    ]
  }
}
```

```json
{
  "guidance": {
    "controller": { "p_gain": 2.5, "i_gain": 0.1, "d_gain": 0.5, "lookahead": 2.8 },
    "limits": { "max_steer_angle": 35.0, "max_steer_rate": 25.0, "min_speed": 0.5, "max_speed": 20.0 }
  }
}
```

```json
{
  "sections": {
    "overlap": 0.15,
    "lookAhead": 2.0,
    "coverage": { "minimum": 0.98, "target": 1.0, "maximum": 1.02 }
  }
}
```

These snippets align with the articulated tractor example in Appendix samples and provide a quick reference when validating ingestion against calibration requirements.

---

## 7) Verification

* Regression fixtures confirm ≤ 5 cm RMS toolpoint cross-track error on flat ground and ≤ 10 cm on 8% sidehills without crab steering.
* Mode profile transitions (road ↔ field ↔ fail_safe) complete in < 150 ms with < 1° transient on dependent joints.
* Export/import round-trips preserve content hashes (modulo calibration stamps) and reject cycles, missing sensors, or incompatible schemas.
* Ackermann wizard CSV loopback yields < 0.2° RMS residual; sidehill slip sanity produces 0.08–0.16 m/s with κ_max derate ≥ 15%; road→field flips stay < 150 ms and < 1° transient.【F:docs/sections/6X_Core_Domain_Services/61-ADR-067 - Equipment configuration and axle-centric kinematics runtime.md†L188-L210】

---

## 8) References

* [Multi-steer equipment configurator blueprint](../6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md#6192-multi-steer-configurator-detail)
* [ADR-017 — Equipment profiles and kinematics](61-ADR-017%20-%20Equipment%20profiles%20and%20kinematics.md)
* [ADR-033 — Guidance planner and autosteer orchestration](../6X_Core_Domain_Services/61-ADR-033%20-%20Guidance%20planner%20and%20autosteer%20orchestration.md)
* [ADR-028 — Stack boundaries](../6X_Core_Domain_Services/61-ADR-028%20-%20Stack%20boundaries.md)

---

## Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-20 | Initial draft | Nexus Team (Codex) |  |

