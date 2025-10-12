# Engine & Machine Gauges (Status: collecting proposals)

## Problem statement
Capture read-only engine and machine telemetry (RPM, temperatures, pressures, voltages, fuel levels, etc.) using J1939/ISOBUS semantics while keeping AgOpenGPS transports and dashboards metadata-driven. Gauges must surface consistently across overlays, standalone panels, and compact widgets without introducing bespoke code paths per sensor.

## Requirements (from contributors)
- R-GA-000 (MUST, J1939 alignment): Map core engine gauges to authoritative PGN/SPN sources so third-party controllers and gateways interoperate without custom scaling tables.【F:docs/SRS/sections/15_Engine_Machine_Gauges.md†L15-L31】
- R-GA-001 (MUST, transport): Deliver gauge telemetry through dedicated read-only PGNs (0xDA/0xD9/0xD8) that reuse the existing AOG CAN/UDP framing and preserve current layer/section PGNs.【F:docs/SRS/options/O-COMM-7_GaugeTelemetryPGNs.md†L1-L54】
- R-GA-002 (MUST, configuration): Define gauges through JSON metadata—ID, PGN/SPN source, scale/offset, smoothing, target/alarm bands—so operators can add/remove telemetry without recompiling clients.【F:docs/SRS/sections/15_Engine_Machine_Gauges.md†L33-L92】
- R-GA-003 (MUST, UI parity): Provide overlay, standalone panel, and mini widget presentations that honor `targetBand`, `alarmBands`, TTL, and quality gating to keep annunciation consistent across layouts.【F:docs/SRS/sections/15_Engine_Machine_Gauges.md†L94-L128】
- R-GA-004 (SHOULD, smoothing & stale handling): Support optional EMA smoothing, deadbands, and stale indicators driven by configuration so noisy sensors remain usable without hiding real faults.【F:docs/SRS/sections/15_Engine_Machine_Gauges.md†L82-L128】
- R-GA-005 (COULD, capability discovery): Advertise supported gauges via capability bits and validity heartbeats so dashboards can pre-provision tiles and detect publisher outages without bespoke logic.【F:docs/SRS/sections/15_Engine_Machine_Gauges.md†L130-L156】【F:docs/SRS/options/O-COMM-7_GaugeTelemetryPGNs.md†L23-L48】

## Context and scope
Gauges focus on machine-level telemetry that dashboards consume read-only. They complement, but do not replace, layer-specific rate or steering PGNs.

### Authoritative mappings
| Gauge | J1939 PGN | SPN | Units | Rate (typ.) | Notes / Sources |
|---|---|---|---|---|---|
| Engine Speed (RPM) | 61444 (EEC1) | 190 | rpm (0.125 rpm/bit) | 20–50 ms | Standard mapping; widely implemented. Sources: Colorado State University – Engineering; ICP DAS; forum.iqan.se. |
| Coolant Temp | 65262 (Engine Temp 1) | 110 | °C (1 °C/bit, −40 °C offset) | ~1 s | Byte 1 per J1939-71. Sources: JCOM1939 Monitor Pro; Copperhill Technologies. |
| Engine Oil Pressure | 65263 (Engine Fluid Lvl/Press) | 100 | kPa (4 kPa/bit) | ~100–1000 ms | Byte 4; standard scaling. Source: Cattron. |
| Battery Potential / Power Input 1 | 65271 | 168 | V (0.05 V/bit) | ~1000 ms | Bytes 5–6; typical battery bus voltage. Source: Cattron. |
| Fuel Level 1 | 65276 (Dash Display 1) | 96 | % (0.4 %/bit) | ~2 s | Commonly used for tank level. Sources: Cattron; maximatecc. |

> **Note:** Hydraulic pressure is implement-specific in ISOBUS rather than a single engine PGN. Treat these as generic analog gauges mapped through LayerDefinitions with explicit units, thresholds, and optional vendor PGN overrides.

### Gauge definitions (JSON examples)
```json
{
  "id": "EngineSpeed",
  "gaugeId": 1,
  "source": { "pgn": 61444, "spn": 190 },
  "units": "rpm",
  "dataType": "u16",
  "scale": 0.125,
  "offset": 0,
  "targetBand": { "min": 600, "max": 2200 },
  "alarmBands": [
    { "min": 0, "max": 400, "severity": "warn" },
    { "min": 2600, "max": 10000, "severity": "crit" }
  ],
  "smoothing": { "emaAlpha": 0.3, "deadband": 5.0 }
}

{
  "id": "CoolantTemp",
  "gaugeId": 2,
  "source": { "pgn": 65262, "spn": 110 },
  "units": "°C",
  "dataType": "u8",
  "scale": 1.0,
  "offset": -40.0,
  "targetBand": { "min": 75, "max": 95 },
  "alarmBands": [
    { "max": 65, "severity": "warn" },
    { "min": 105, "severity": "crit" }
  ],
  "smoothing": { "emaAlpha": 0.25 }
}

{
  "id": "EngineOilPressure",
  "gaugeId": 3,
  "source": { "pgn": 65263, "spn": 100 },
  "units": "kPa",
  "dataType": "u8",
  "scale": 4.0,
  "offset": 0.0,
  "targetBand": { "min": 150, "max": 600 },
  "alarmBands": [
    { "max": 100, "severity": "crit" }
  ],
  "smoothing": { "emaAlpha": 0.3 }
}

{
  "id": "BatteryPotential",
  "gaugeId": 4,
  "source": { "pgn": 65271, "spn": 168 },
  "units": "V",
  "dataType": "u16",
  "scale": 0.05,
  "offset": 0.0,
  "targetBand": { "min": 12.0, "max": 14.7 },
  "alarmBands": [
    { "max": 11.0, "severity": "crit" },
    { "min": 15.0, "severity": "warn" }
  ]
}

{
  "id": "FuelLevel1",
  "gaugeId": 5,
  "source": { "pgn": 65276, "spn": 96 },
  "units": "%",
  "dataType": "u8",
  "scale": 0.4,
  "offset": 0.0,
  "targetBand": { "min": 15.0, "max": 100.0 },
  "alarmBands": [
    { "max": 10.0, "severity": "warn" }
  ],
  "smoothing": { "emaAlpha": 0.4 }
}
```

The shared [Gauge ID Registry](../appendices/GaugeId_Registry.md) assigns stable `gaugeId` values and raw-to-engineering unit conversions.

#### Transport & scaling rules
- Missing codes: `0xFF` (u8) and `0xFFFF` (u16) signal no data. Mark gauges stale when repeated values exceed their TTL.
- Units and scaling originate from JSON metadata. Transports stay raw; clients compute `value = raw * scale + offset`.
- EMA smoothing and optional `deadband` dampen noise without masking real transitions. Omit `smoothing` to disable.
- UI widgets share `targetBand` / `alarmBands` semantics to drive color, blink, and annunciators consistently across layouts.

### Gauge UI behaviors
- **Placement:** Dockable overlay strip on the map, standalone gauge panel, and compact header/footer widgets.
- **Layout:** Configurable rows/columns, drag-to-reorder, and per-gauge size presets (S/M/L).
- **Color & motion:** Neutral defaults, warn/crit palette derived from `alarmBands`, optional blink with `holdMs` hysteresis to avoid flicker.
- **Visibility:** Render only when quality ≥ threshold and sample age ≤ TTL; otherwise gray out and optionally flag a stale indicator.
- **Interactions:** Tap/long-press opens a detail modal with sparkline, min/max/avg, raw SPN bytes, source PGN, and last timestamp.
- **Multi-source arbitration:** Prefer the highest quality publisher; tie-break on `schemaVersion` and newest timestamp.

### Transport notes
- Controllers may forward native J1939 frames to an AOG gateway that emits Gauge Blocks or publish the PGNs directly.
- Gauge traffic is unacknowledged; rely on the Gauge Heartbeat (0xD8) plus optional UDP sequence trailers (`capabilities.seq = 1`) for liveness and loss detection.
- Follow J1939 pacing where practical: RPM @ 20–50 ms, temperatures/pressures @ 0.5–1 s, battery/fuel @ 1–2 s.

### Open items
- Define vendor-neutral gauge IDs for hydraulic and implement-specific pressure sensors with configurable ISOBUS DDI/PGN mapping hooks.
- Extend the unit registry (kPa, bar, psi, etc.) and deliver automatic conversions in the UI.
- Advertise supported gauges through PGN 0xE2 capability bits so dashboards can pre-provision tiles.

### Verification
- Gauges render correctly in overlay strip and standalone panels.
- Needles/tiles recolor based on `alarmBands` warn/crit thresholds.
- Engine RPM and coolant temperature scale accurately against replay logs with known raw bytes.
- Packet loss simulations trigger stale indicators after TTL expiration.
- Reloading JSON configuration adds/removes gauges without code changes.
