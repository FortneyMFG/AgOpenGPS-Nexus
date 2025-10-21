# 74 — Monitoring Systems
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Version:** 0.1.0
**Section ID:** 74
**Editors:** Monitoring & Telemetry Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 42 — Interprocess Communications, 64 — Telemetry & Health, 71 — Mapping Kernel Contracts, 72 — Mapping Layers Plugin
**Upstream Dependencies:** ADR-042-O7 Gauge Telemetry, ADR-010 Layer Registry, ADR-021 Automation Pipelines
**Downstream Impacts:** Dashboards, Mapping overlays, Harvest analytics, Telemetry recorder

---

## 74.1 Purpose & Scope

Define how Nexus captures, normalizes, and visualizes machine monitoring telemetry—including engine gauges and combine harvest data—using metadata-driven transports. The section ensures gauges render consistently across overlays, dashboards, and widgets while aligning with Layer Registry persistence and telemetry recording.

---

## 74.2 Context

- Gauges ingest J1939/ISOBUS PGNs and serial payloads via telemetry transports outlined in ADR-042-O7, reusing existing CAN/UDP framing.【F:docs/SRS/sections/4X_Interprocess_Communications/42-O7%20-%20Gauge%20telemetry%20PGNs%20for%20engine%20%26%20machine%20data.md†L1-L54】
- Mapping section 72 leverages monitoring overlays for operator dashboards; telemetry Section 64 governs health metrics, stale detection, and smoothing policies.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L24-L126】
- Yield telemetry integrates with analytics ADRs (049/050) to keep profit and agronomy calculations consistent with coverage overlays.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-049 - Yield & Analytics Plugin.md†L21-L52】【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L21-L52】

---

## 74.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Gauge Mapping | Hard-coded WinForms gauges with bespoke scaling tables. | Difficult to extend to new sensors or vendor PGNs. | JSON metadata with PGN/SPN definitions and shared registry. | Monitoring backlog |
| UI Consistency | Gauges rendered differently across overlays, panels, and widgets. | Operators confused by conflicting alarms/targets. | Standardize rendering rules with `targetBand`/`alarmBands`. | UI audit |
| Harvest Data | Combine telemetry ingested via custom scripts per vendor. | No deterministic provenance or calibration workflows. | Normalize to `YieldTelemetry` schema with calibration state machine. | Yield plugin proposal |

---

## 74.4 Definitions

| Term | Definition |
|------|-------------|
| Gauge Definition | JSON metadata describing source PGN/SPN, scaling, smoothing, targets, and alarms for a telemetry gauge. |
| Capability Heartbeat | Periodic message advertising supported gauge IDs and data freshness. |
| YieldTelemetry | Structured telemetry payload containing mass flow, moisture, elevator speed, lag, and calibration metadata. |
| Swath Summary | Aggregated harvest metrics (yield, moisture, bushels) per spatial segment exported via telemetry recorder. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory with verification.
> - **SHOULD / SHOULD NOT** = strong preference; require waiver for deviations.
> - **MAY** = optional, subject to telemetry policy review.

## 74.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|-----------------|-----------------------------|
| R-MON-7400 | MUST | Transport | Gauge telemetry MUST use dedicated read-only PGNs (0xDA/0xD9/0xD8) while preserving existing layer/section PGNs. | ADR-042-O7 | CAN gateway integration tests validating PGN routing. |
| R-MON-7401 | MUST | Metadata | Gauges MUST be declared via JSON metadata including source PGN/SPN, scaling, smoothing, target/alarm bands, and TTL. | Monitoring proposal | Schema lint + configuration tests. |
| R-MON-7402 | MUST | UI Consistency | Dashboards, overlays, and widgets MUST honor shared rendering rules (targetBand, alarmBands, stale indicators). | UI parity backlog | UI regression tests verifying consistent thresholds. |
| R-MON-7403 | SHOULD | Smoothing | Gauges SHOULD support EMA smoothing, deadbands, and stale detection toggled via configuration. | Telemetry WG | Telemetry unit tests verifying smoothing options. |
| R-MON-7404 | MUST | Capability Discovery | Publishers MUST advertise supported gauges and validity heartbeats so clients detect outages gracefully. | ADR-042-O7 | Integration tests ensuring heartbeat drop triggers stale state. |
| R-MON-7405 | MUST | Yield Normalization | Combine telemetry MUST normalize to `YieldTelemetry` schema with calibration metadata, lag, and coverage alignment. | Monitoring proposal | Harvest simulator verifying schema + lag compensation. |
| R-MON-7406 | MUST | Calibration & QA | System MUST persist calibration coefficients, validation timestamps, and block heatmap rendering when calibration is stale. | Monitoring proposal | QA workflow tests ensuring stale calibration prevents overlays. |
| R-MON-7407 | SHOULD | Data Export | Telemetry recorder SHOULD emit swath summaries (yield, moisture, bushels, time span) with CSV/GeoJSON export paths. | Analytics backlog | Export integration tests verifying schema compliance. |

### 74.5.1 Authoritative Gauge Mapping

| Gauge | PGN | SPN | Units | Typical Rate | Notes |
|---|---|---|---|---|---|
| Engine Speed | 61444 (EEC1) | 190 | rpm (0.125 rpm/bit) | 20–50 ms | Widely supported; baseline gauge. |
| Coolant Temperature | 65262 (Engine Temp 1) | 110 | °C (1 °C/bit, −40 °C offset) | ~1 s | Byte 1 per J1939-71. |
| Engine Oil Pressure | 65263 (Engine Fluid Lvl/Press) | 100 | kPa (4 kPa/bit) | 100–1000 ms | Byte 4 standard scaling. |
| Battery Potential | 65271 | 168 | V (0.05 V/bit) | ~1 s | Tracks machine electrical health. |
| Fuel Level | 65276 (Dash Display 1) | 96 | % (0.4 %/bit) | ~2 s | Tank level gauge. |
| Hydraulic Pressure | Vendor-specific | — | psi/kPa | Varies | Map via generic analog gauge definitions. |

### 74.5.2 Gauge Definition Example

```json
{
  "id": "EngineSpeed",
  "gaugeId": 1,
  "source": { "pgn": 61444, "spn": 190 },
  "units": "rpm",
  "scale": 0.125,
  "targetBand": { "min": 600, "max": 2200 },
  "alarmBands": [
    { "min": 0, "max": 400, "severity": "warn" },
    { "min": 2600, "max": 10000, "severity": "crit" }
  ],
  "smoothing": { "emaAlpha": 0.3, "deadband": 5.0 },
  "ttlMs": 2000
}
```

### 74.5.3 Yield Telemetry Schema

```json
{
  "id": "YieldTelemetry",
  "source": { "pgn": 61443, "fallback": "serial:RS232" },
  "fields": [
    { "name": "massFlow", "units": "kg/s", "scale": 0.01 },
    { "name": "grainMoisture", "units": "%", "scale": 0.1 },
    { "name": "elevatorSpeed", "units": "rpm", "scale": 1.0 },
    { "name": "lagMeters", "units": "m", "scale": 0.1 }
  ],
  "calibration": {
    "cropType": "corn",
    "massFlowGain": 1.07,
    "moistureOffset": -0.4,
    "lastValidatedUtc": "2024-04-13T22:10:00Z"
  },
  "coverage": {
    "swathWidth": 9.14,
    "sampleRateHz": 5.0
  }
}
```

---

## 74.6 Acceptance Criteria & Verification

- CAN/UDP integration tests verify PGN routing, scaling, and TTL handling across gateway implementations.
- UI regression suites confirm overlays, panels, and widgets display identical target/alarm cues for each gauge.
- Harvest simulator replays validate lag compensation, calibration workflows, and coverage alignment for yield/moisture heatmaps.
- Export tests ensure swath summaries produce valid CSV and GeoJSON files compatible with downstream systems.

### 74.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-MON-7400 | Integration test | `tests/integration/can_gauge_transport.cs` | Gauge PGNs transmitted without colliding with control PGNs. |
| R-MON-7401 | Schema lint | `tools/telemetry/gauge_schema_lint.py` | 100% metadata files pass validation. |
| R-MON-7402 | UI regression | `tests/ui/gauge_rendering.spec` | Target/alarm cues match reference renders. |
| R-MON-7404 | Heartbeat test | `tests/integration/gauge_capability_heartbeat.cs` | Loss of heartbeat triggers stale indicator ≤ 3 s. |
| R-MON-7405 | Harvest sim | `sim/harvest/yield_pipeline.md` | Lag-compensated yield within ±2% of baseline. |
| R-MON-7407 | Export test | `tests/integration/harvest_export.cs` | CSV/GeoJSON exports validated against schema. |

---

## 74.7 Constraints

- Gauge telemetry remains read-only; commands or actuator control require separate authenticated channels.
- Embedded targets must process gauge updates within available CPU (≤ 20% core utilization) while streaming to dashboards.
- Calibration data must persist with encryption-at-rest to protect operator and agronomy data.

### 74.7.1 Non-Functional Requirement Classes

- **Performance:** PGN handling latency, widget render frequency.
- **Reliability:** Stale detection, heartbeat monitoring, failover to fallback sources.
- **Security:** Read-only transport enforcement, calibration data protection.
- **Usability:** Consistent gauge presentation, actionable alarms, calibration workflow clarity.
- **Operability:** Telemetry recorder diagnostics, export tooling, configuration validation.

---

## 74.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-74-1 | Vendor PGN deviations require custom scaling. | Medium | Allow vendor override tables in metadata with QA review. | @monitoring |
| RISK-74-2 | Telemetry bandwidth saturation during harvest. | High | Implement adaptive throttling + buffering; monitor via telemetry metrics. | @telemetry |
| ISSUE-74-1 | Determine retention policy for raw gauge samples vs. aggregates. | Medium | Pending storage ADR; coordinate with analytics. | @ops |
| ISSUE-74-2 | Clarify fallback behavior when elevator speed missing for lag compensation. | Low | Document GPS speed fallback; add quality flag. | @mapping |

---

## 74.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Metadata-Driven Gauges | JSON-driven definitions reduce code churn and keep scaling consistent. |
| C2 | UI Parity | Shared rendering rules avoid conflicting alarms across overlays vs. panels. |
| C3 | Calibration Integrity | Blocking overlays on stale calibration prevents misleading analytics. |
| C4 | Transport Isolation | Dedicated read-only PGNs protect section control traffic. |
| C5 | Export Compatibility | Swath summaries enable integration with FarmOS, OpenAg, and similar tools. |
| C6 | Bandwidth Management | Adaptive throttling ensures telemetry remains responsive during harvest peaks. |

### 74.9.1 Assumptions & Preconditions

- [A1] Gateways can publish capability heartbeats and gauge payloads at configured intervals.
- [A2] Operators maintain calibration data for each crop/implement combination.
- [A3] Dashboards consume JSON metadata to render gauges without custom logic.

---

## 74.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | All options consolidated as design considerations in §74.9. | — |

---

## 74.11 Comparison Matrix

| Attribute / Criteria | Metadata-Driven Monitoring | Legacy Gauge Handling |
|----------------------|-----------------------------|-----------------------|
| Extensibility | JSON-defined gauges deploy without code changes | Hard-coded sensors per release |
| UI Consistency | Shared rendering rules across overlays/panels/widgets | Divergent alarm behavior |
| Harvest Integration | Yield telemetry normalized with calibration + lag | Vendor-specific scripts |
| Telemetry Reliability | Heartbeats, stale detection, smoothing controls | Ad-hoc polling, no stale handling |
| Export Support | CSV/GeoJSON swath summaries | Manual CSV exports without metadata |
