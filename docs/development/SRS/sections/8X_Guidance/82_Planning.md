# 82 — Planning
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Version:** 0.1.0
**Section ID:** 82
**Editors:** Guidance Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 81 — Guidance Orchestrator, 83 — Autosteer Target Models
**Upstream Dependencies:** ADR-033, ADR-069, 2X — System Architecture
**Downstream Impacts:** Autosteer plugins, Coverage Writer, Telemetry pipelines

---

## 82.1 Purpose & Scope

Define the deterministic planning stack that converts boundary, keep-out, implement, and orientation inputs into steerable catalogs without interrupting Autosteer.
This section captures planner responsibilities, caching strategies, fallback behavior, and telemetry expectations that uphold operator trust during refreshes.

---

## 82.2 Context

- Guidance Orchestrator supplies live geometry, implement width, and operator intents requiring rapid planner turnaround.
- Fields2Cover acts as the baseline solver with deterministic caching; AB-line fallback must remain available within tight latency budgets.
- Operators expect Quick Refresh interactions to avoid steering gaps while still validating complex headlands and partial replans.
- Telemetry dashboards rely on rich planner metrics (latency, jitter, fallback rates) to govern releases.

---

## 82.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Planner Hosting | Embedded logic tightly coupled to UI. | Refresh pauses Autosteer; poor isolation. | Orchestrated planner pipeline with caching + fallback. | Guidance pilot notes |
| Cache Strategy | Minimal reuse of prior plans. | Redundant recomputation; long latency. | Deterministic cache keyed by boundary + settings. | Contributor backlog |
| Fallback Policy | Manual AB-line takeover. | No telemetry; delayed recovery. | Automatic AB fallback tagged in telemetry. | Legacy workflow |
| Persistence | Plans lost on restart. | Operators forced to re-drive or wait for recompute. | Persist last three catalogs per field. | Nexus planning RFC |

---

## 82.4 Definitions

| Term | Definition |
|------|-------------|
| Planner Catalog | Structured headland/swath dataset ready for execution. |
| Quick Refresh | Command that recomputes planning inputs while Autosteer continues along prior targets. |
| Hysteresis Threshold | Debounce configuration that avoids unnecessary replans for minor geometry/width changes. |
| Fallback Mode | Deterministic AB-line offset planner invoked on timeout or solver failure. |
| Settings Hash | Canonical digest of planner configuration influencing cache keys. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory planner requirements.
> - **SHOULD / SHOULD NOT** = preferred behaviors with documented waivers.
> - **MAY / COULD** = optional enhancements.

## 82.5 Requirements

| ID | Priority | Category | Summary | Verification |
|----|----------|----------|---------|--------------|
| R-PLAN-000 | MUST | Planner API | Package requests with `{boundary_rev, keepouts_rev, effective_width, implement_profile, headland_spec, orientation}`; respond ≤ 200 ms median for ≤ 65 ha fields; expose error codes and fallback reasons. | Solver latency telemetry; API contract tests. |
| R-PLAN-001 | MUST | Caching | Cache planner outputs keyed by `field_rev + settings hash`; respect hysteresis debounces (boundary 500 ms, keep-out 250 ms, width 10%). | Cache hit analysis; debounced trigger tests. |
| R-PLAN-002 | MUST | Fallback | Provide deterministic AB fallback on timeouts (> 500 ms) or solver failures; tag `plan_source=fallback`. | Failure-injection scenarios. |
| R-PLAN-003 | SHOULD | Partial Replans | Preserve unworked paths when new keep-outs occur ≥ 30 m away while maintaining sequencing integrity. | Scenario regression suite. |
| R-PLAN-004 | MUST | Persistence | Persist last three plan catalogs per field and restore most recent on restart. | Restart smoke tests. |
| R-PLAN-005 | MUST | Execution Stream | Publish `SteerTargets` at 25 Hz with ≤ 5 ms jitter; continue streaming previous catalog until new plan committed; include optional `speed_cap_mps` and `row_bias_m`. | Autosteer telemetry; jitter analysis. |
| R-PLAN-006 | SHOULD | Equivalence Policy | Define Hausdorff + heading tolerances for hot swaps to maintain engagement. | Equivalence validation tests. |
| R-PLAN-007 | MUST | Telemetry | Record planner latency, jitter, plan source, and fallback metadata for QA dashboards. | Telemetry completeness review. |

---

## 82.6 Architecture Overview

1. **Input Assembly:** Guidance Orchestrator bundles geometry, effective width, implement profile, and operator settings into normalized planner requests.
2. **Solver Invocation:** Fields2Cover solves primary plans; deterministic cache keys ensure identical inputs reuse previous catalogs.
3. **Fallback Engine:** AB-offset fallback engages on timeout or solver error while tagging telemetry and preserving operator awareness.
4. **Catalog Persistence:** The last three successful catalogs per field persist to disk (configurable root) and reload during startup.
5. **Execution Streaming:** Planner commits feed Autosteer via `SteerTargets` while respecting equivalence policy before switching paths.

---

## 82.7 Performance & Timing

- Primary solver p50 latency ≤ 120 ms and p95 ≤ 200 ms for ≤ 65 ha fields with ≤ 2 holes.
- Timeout threshold fixed at 500 ms before fallback engagement; fallback publishes new targets ≤ 20 ms after trigger.
- Planner loops evaluate refresh triggers at ≥ 4 Hz in sync with Orchestrator.
- Cache rebuild or invalidation operations complete within 100 ms to avoid UI stalls.

---

## 82.8 Verification & Validation

- Regression suite exercises boundary edits, keep-out additions, and width changes to confirm hysteresis and partial replan logic.
- Hardware-in-loop testing validates Quick Refresh workflow without target gaps and measures fallback activation latency.
- Replay harness reuses persisted catalogs to confirm deterministic hashing and restoration.
- Telemetry dashboards monitor latency distribution, fallback frequency, and cache hit rate per release.

---

## 82.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Embedded solver path | Keep Fields2Cover embedded in Guidance plugin for lowest latency and reuse of DI context. |
| C2 | Remote service isolation | Evaluate gRPC-based planner services for environments requiring independent scaling or language diversity. |
| C3 | Hybrid burst capacity | Combine embedded planner with optional remote solver when local latency budgets are exceeded. |
| C4 | Cache governance | Decide where caches live (plugin vs. service) and how to synchronize across deployments. |
| C5 | Fallback parity | Ensure fallback paths share telemetry and sequencing semantics regardless of hosting choice. |

### 82.9.1 Decision Inputs

- Family DS-PLAN-RUNTIME weighs embedded vs. remote execution prior to fallback strategy decisions.
- Family DS-PLAN-FALLBACK evaluates whether fallbacks share code with primary planner or run independently.

---

## 82.10 Evaluation Criteria

- Deterministic latency ≤ 200 ms for ≤ 65 ha polygons with ≤ 2 holes.
- Hot-swap equivalency maintains Autosteer engagement without jitter.
- Planner caches survive restarts and configuration reloads.
- Telemetry coverage powers regression dashboards (latency, fallback rate, jitter).
- Deployments remain supportable offline; default workflows avoid cloud dependencies.

---

## 82.11 Current Sentiment

- Embedded Fields2Cover (C1) remains default due to minimal latency and shared contracts with ADR-033.
- Hybrid strategies (C3) gain interest for large fields if cache coherence and governance costs stay manageable.

---

## 82.12 Open Questions

- Q-PLAN-001: Which planner artifacts (catalog JSON, solver logs) must sync to support remote debugging?
- Q-PLAN-002: How should hysteresis thresholds adapt for multi-implement rigs with variable width sensors?
- Q-PLAN-003: What telemetry sampling strategy balances 25 Hz steer target logs with storage constraints during long jobs?

---

## 82.13 References

- [81 — Guidance Orchestrator](81_Guidance_Orchestrator.md)
- [ADR-033 — Guidance planner and autosteer orchestration](81-ADR-033%20-%20Guidance%20planner%20and%20autosteer%20orchestration.md)
- [ADR-069 — Guidance Orchestrator plugin](81-ADR-069%20-%20Guidance%20Orchestrator%20plugin.md)
- [Fields2Cover documentation](https://fields2cover.github.io/)
