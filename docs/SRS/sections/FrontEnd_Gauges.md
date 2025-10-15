# Engine & Machine Gauges (J1939/ISOBUS-aligned)

## Overview
Gauges are read-only telemetry values surfaced as layer-aware overlays and UI widgets. They pull their semantics from SAE J1939 Suspect Parameter Numbers (SPNs) and, where applicable, ISOBUS Device Descriptor Identifiers (DDIs). Transport uses the existing AgOpenGPS (AOG) UDP/CAN envelope, byte order, and capability flags so gauge traffic coexists cleanly with existing layer, rate, and steering PGNs. Values are always transmitted and displayed in absolute engineering units with explicit scaling metadata.

## Authoritative mappings
| Gauge | J1939 PGN | SPN | Units | Rate (typ.) | Notes / Sources |
|---|---|---|---|---|---|
| Engine Speed (RPM) | 61444 (EEC1) | 190 | rpm (0.125 rpm/bit) | 20–50 ms | Standard mapping; widely implemented. Sources: Colorado State University – Engineering; ICP DAS; forum.iqan.se. |
| Coolant Temp | 65262 (Engine Temp 1) | 110 | °C (1 °C/bit, −40 °C offset) | ~1 s | Byte 1 per J1939-71. Sources: JCOM1939 Monitor Pro; Copperhill Technologies. |
| Engine Oil Pressure | 65263 (Engine Fluid Lvl/Press) | 100 | kPa (4 kPa/bit) | ~100–1000 ms | Byte 4; standard scaling. Source: Cattron. |
| Battery Potential / Power Input 1 | 65271 | 168 | V (0.05 V/bit) | ~1000 ms | Bytes 5–6; typical battery bus voltage. Source: Cattron. |
| Fuel Level 1 | 65276 (Dash Display 1) | 96 | % (0.4 %/bit) | ~2 s | Commonly used for tank level. Sources: Cattron; maximatecc. |

> **Note:** Hydraulic pressure is implement-specific in ISOBUS and not exposed through a single J1939 engine PGN. Treat hydraulic channels as generic analog gauges mapped through LayerDefinitions with explicit units, thresholds, and optional vendor PGN overrides.

## Gauge definitions (JSON schema examples)
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

### Transport and scaling rules
- Missing codes: `0xFF` (u8) and `0xFFFF` (u16) represent “no data available.” Clients must mark gauges as stale when these values repeat past the configured TTL.
- Units and scaling originate from the JSON definition. Transports carry raw integers; UI components convert using `value = raw * scale + offset`.
- Exponential moving average (EMA) smoothing is optional per gauge. Provide `emaAlpha` and optional `deadband` in configuration; omit the object to disable smoothing.
- UI widgets interpret `targetBand` and `alarmBands` to color needles/tiles and drive warn/crit annunciators consistently across overlay, standalone, and compact widgets.

## Gauge UI behaviors
- **Placement:** Gauges appear as (a) a dockable overlay strip on the main map, (b) a standalone gauge panel, and (c) mini widgets embedable in header/footer regions.
- **Layout:** Operators can configure rows/columns, drag-to-reorder, and choose per-gauge sizes (S/M/L) for overlay and standalone containers.
- **Color & motion:** Gauges default to neutral colors. Warn/crit alarm bands recolor the gauge and may blink with configurable `holdMs` hysteresis so transient crossings do not flicker excessively.
- **Visibility:** A gauge renders only when the most recent sample age is within the configured TTL and quality score meets or exceeds the threshold. Stale gauges gray out and optionally show a warning icon.
- **Interactions:** Tap or long-press opens a detail modal featuring a recent sparkline, min/max/avg values, raw SPN bytes, source PGN, and last timestamp.
- **Multi-source arbitration:** When multiple publishers emit the same `gaugeId`, prefer the highest quality feed. If quality scores tie, choose the latest `schemaVersion` and newest timestamp.

## Transport notes
- Controllers may forward native J1939 frames to the AOG gateway for translation into Gauge Blocks or emit the Gauge Block PGNs directly over UDP/CAN.
- Gauges use no acknowledgements; rely on Gauge Heartbeat (0xD8) plus optional UDP sequence trailers (when `capabilities.seq = 1`) for liveness and packet-loss detection.
- Recommended update rates follow J1939 guidance: Engine RPM at 20–50 ms; coolant temperature and pressure ranges at 0.5–1 s; battery and fuel level at 1–2 s to balance fidelity and bandwidth.

## Open items
- Define vendor-neutral gauge IDs for hydraulic and implement-specific pressure sensors (e.g., `HydraulicPressureA` in kPa) with configuration hooks for ISOBUS DDIs/PGNs.
- Extend the unit registry (kPa, bar, psi, etc.) and provide automatic conversions in the UI.
- Advertise supported gauges via a capability bit in PGN `0xE2` so subscribers can pre-provision dashboards.

## Verification
- Gauges render correctly in both the overlay strip and standalone panels.
- Needle/tiles shift colors based on `alarmBands` warn/crit thresholds.
- Engine RPM and coolant temperature track known replay logs after applying the documented scaling and offsets.
- Simulated packet loss triggers the stale indicator when the TTL expires.
- Reloading JSON configuration adds/removes gauges without requiring code changes.

## Related ADRs

- [ADR-016 — Firmware Transport Variable Rate PGNs](../../ADR/ADR-016-firmware-transport-variable-rate-pgns.md)
- [ADR-034 — Metadata-Driven Dashboards](../../ADR/ADR-034-metadata-driven-dashboards.md)
- [ADR-052 — Field Health Plugin](../../ADR/ADR-052_FieldHealthPlugin.md)
