# Variable Rate Layer Architecture Proposal

## Goals
- Preserve existing on/off section mapping while adding support for numeric rate layers (e.g., gallons-per-acre, seeds-per-acre).
- Support multiple independent feedback channels per section (e.g., actual rate, skips, doubles).
- Allow visualization rules (color ramps, thresholds) to be defined per layer, with accumulation across passes.
- Enable summary metrics (per-section totals, averages, min/max) for UI consumption.
- Keep UI widgets, graphs, and dashboards layer-agnostic by sourcing metadata from layer definitions so new layer types require configuration only.
- Provide a flexible layer registry so firmware (SK21 AOG_RC and future modules) can publish arbitrary analog/digital signals without code changes.

## Example Layer Catalog

The system should treat every feedback quantity as a first-class, configurable layer. Initial built-ins can ship with presets, but the data model should not assume a fixed list.

| Layer Type | Default Aggregation | Notes |
|------------|---------------------|-------|
| Section On/Off | Boolean (existing) | Continue to render with current triangle-strip pipeline for backwards compatibility. |
| Commanded Rate | Latest value + optional smoothing | Useful for comparing intended vs. actual application. |
| Actual Rate | Sum or weighted average | Relative 0–200% (for ratio layers) or absolute 0..max scaling; supports aggregation across overlapping passes. |
| Skips | Percentage | Drives both color layer and header summary metric. |
| Doubles | Percentage | Same treatment as skips but separate color palette. |
| Downforce / Downpressure | Average or min | Analog feed from sensors per row/section. |
| Hopper Pressure / Bin Level | Latest sample | Supports liquid/air carts; may need alert thresholds. |
| Yield / Moisture / Test Weight | Configurable (avg, sum) | Combine harvester data sets, multiple metrics per crop pass. |
| Generic Digital Input | Boolean / duty cycle | E.g., section clutch feedback, bin door status. |
| Generic Analog Input | Configurable scale | Users can define engineering units and threshold colors. |

Additional presets (e.g., singulation, ride quality) can be added by configuration files or plugins without touching rendering logic. UI components should treat these entries as pure metadata so examples shipped with the app never hard-code special cases.

### Units Registry

| unitsId | Symbol | Description |
|---------|--------|-------------|
| `state.onOff` | — | Binary working state / clutch feedback |
| `state.flowState` | — | Multi-level flow feedback (off/low/normal/high) |
| `ratio.percent` | % | Relative rate layers scaled 0–200 % |
| `pressure.psi` | PSI | Pounds per square inch (downforce, hydraulics) |
| `pressure.kpa` | kPa | Metric pressure alternative |
| `rate.gpa` | GPA | Gallons per acre commanded/applied |
| `mass.kg_ha` | kg/ha | Combine yield mass flow |
| `volume.bu_ac` | bu/ac | Grain yield volume |
| `moisture.percent` | % | Crop moisture sensors |

Projects can extend this registry, but UI components and dashboards should rely on the canonical IDs to ensure consistent symbols and unit conversions.

## Conceptual Model
- **Section / Row**: keeps existing binary state plus a dictionary of layer controllers keyed by layer ID. For planters, every row can be represented as a “section” to reuse the geometry pipeline.
- **Layer Controller**: owns runtime state for one feedback quantity on a section/row.
  - Maintains a rolling accumulator of applied value (e.g., GPA, skips %, double %, downforce PSI) and timestamp/position history for mapping.
  - Exposes normalized value (0–1), engineering value (units), and quality (0–1 weight) so rendering code can share color maps, UI can show true units, and dashboards can dim sparse data.
  - Tracks numerator/denominator/min/max counters internally so overlap math stays exact even after multiple passes or sensor cadences.
  - Supports optional derived layers (e.g., “Under Applied” vs. “Over Applied”) as virtual views of a base numeric signal.
- **Layer Definition**: configuration record stored in project/profile files.
  - Fields: `schemaVersion` (semver), `id`, `name`, `units`, `unitsId`, `dataType`, `valueMode`, `minValue`, `maxValue`, `rangePercent`, `displayRanges` (list of `{min,max,color,label}`), `aggregation`, `compositeRule`, optional `rateBump` metadata, UI grouping tags, and `storagePrecision` (`u8`|`u16`|`float`).
  - Provide defaults for “Actual Rate”, “Commanded Rate”, “Skips”, “Doubles”, “Downpressure”, “Hopper Pressure”, “Yield”, “Moisture”, “Test Weight”, etc., but allow users to add arbitrary definitions and reuse a shared units registry to avoid typos (e.g., PSI, kPa, GPA, bu/ac, kg/ha, %).
  - Include `sourceMappings` so multiple hardware inputs can feed the same logical layer (e.g., left/right hopper pressures combined into one layer or kept separate) and specify `deadbandPct` plus `emaAlpha` to smooth UI noise while staying responsive.
  - Record `bitIndex` for packed digital feeds when `dataType == "bitpacked"` and surface `tileResolutionMultiplier` so high-volume layers can throttle writes or use coarser tiles without reconfiguration.
  - Track `targetBand` (min/max good zone), `alarmBands` (list of `{min,max,label,color,hysteresis,holdMs}`), and `qualityRules` (e.g., `[{ "condition": "q < 0.5", "effect": "dim" }]`) so color ramps, alarms, and dashboards remain metadata-driven.
  - Define `emitCadenceHz`, `rateNAFlag`, and any derived layer bindings (e.g., “UnderApplied”/“OverApplied”) alongside the base layer so controllers expose consistent summaries.
- **Layer Map Geometry**: parallels `CPatches` but stores scalar values per vertex in addition to color.
  - Each vertex pair (`left`, `right`) carries `value`, `weight` (area or sample count), and optional min/max so later aggregation never recomputes from lossy averages. A zero weight marks “no data”.
  - Rendering shader/function reads value, selects color based on definition, and composes overlapping passes using the layer’s `compositeRule`. Area-weighted sums use `sumApplied += value * areaSlice`, `sumArea += areaSlice`, and derive `avg = sumApplied / sumArea` at render time.
  - Allow compositing rules: sum for rates, max for skips/doubles, logical OR for digital alarms, percent via numerator/denominator pairs.
  - Persist raw byte/bit samples alongside normalized values so alternate UI layers (e.g., density histograms, planter skip overlays) can reuse the same data without reprocessing.
  - Provide a per-layer write queue with reuseable buffers sized by section count to avoid per-frame heap churn and let the mapping thread consume immutable snapshots.

## Data Flow
1. **Input**
   - New CAN/MQTT/etc. message handlers push raw sensor data (`actualRate`, `skipPct`, `doublePct`, `downforce`, `hopperPressure`, custom analog inputs) into the relevant layer controllers.
   - Controllers update section-level accumulators and expose latest normalized value for UI widgets.
   - Binary feeds arrive via PGN `0xDE` as bit-packed bytes (u1/u2, up to eight layers per byte) while analog feeds use PGN `0xE1` (u8) or `0xE0` (u16); controllers convert both into normalized floats using the layer definition metadata.
   - For relative layers, when commanded ≤ ε (configurable per layer) controllers mark the sample `missing`, assert the layer’s `rateNAFlag`, and avoid divide-by-zero hacks downstream.
   - Allow multiple input packets to feed the same layer within a time slice (e.g., per-row sensors updating faster than GPS); controllers buffer and reconcile before geometry emission using the layer’s aggregation (mean for analog, OR for boolean, numerator/denominator for percentages).

2. **Mapping**
   - When `isMappingOn` transitions true, layer controllers start emitting geometry records similar to `TurnMappingOn`, but throttle by `emitCadenceHz` so high-rate feeds (skips/doubles, downforce) coalesce between GPS fixes. All sensor samples gathered between GNSS updates are collapsed into one write per section per layer.
   - For every position update, controllers call `AddMappingPoint` with the aggregated numerator/denominator, `areaSlice = groundSpeed * dt * sectionWidth * coverageFactor`, min/max, and quality weight (0–1). `coverageFactor` reflects partial overlap/turn compensation and defaults to 1.0 when the boom is fully engaged.
   - Overlapping passes: when writing into the spatial buffer (grid/patch), accumulate according to the layer’s aggregation mode. Sum-mode layers add to `sumApplied` and `sumArea` and surface `sumApplied` as the “stacked passes” summary; average-mode stores numerator/denominator and derives the mean lazily; percentage-mode keeps 32-bit `events`/`opportunities` counters so combined passes remain mathematically correct; min/max layers update running extrema directly instead of recomputing from averages.
   - A zero weight indicates “no data” and is ignored during color ramp lookup and summary stats. Byte feeds reserve `0xFF` as a missing sentinel (u16 uses `0xFFFF`); controllers convert these to weight zero and clear any per-block validity bit.
   - Suspend mapping when groundSpeed < `v_min` (e.g., 0.2 m/s) but keep the latest sensor sample for UI display and diagnostics.
   - Support optional per-layer resolution (some layers may only need coarse tiles) to minimize memory footprint for high-frequency signals like skips/doubles, and persist optional min/max alongside value+weight when the layer requests it.
   - Standardize sample quality as `q ∈ [0,1]`. Each controller outputs `value`, `weight`, and `quality`; UDP/CAN frames may include an optional validity bitmap per block, while the geometry writer stores the numeric `quality` to drive `qualityRules` (e.g., dim map if `q < 0.5`).
   - Composite rules are explicit: `avg` layers compute `avg = sumApplied / sumArea`, `sum` layers report `sumApplied` and expose a derived “stacked passes” summary, `percent` layers compute `percent = events / opportunities`, and state layers use logical `OR` or `MAX` over packed codes.

3. **Rendering**
   - Extend `oglMain_Paint` (and WPF equivalent) to iterate per-layer before per-section.
   - For each active layer:
     - Determine color per patch vertex via a shared color ramp utility that uses identical inclusive/exclusive rules and label generation in both OpenGL and WPF.
     - Allow toggling layer visibility via UI checkboxes and support opacity stacking so multiple layers can be composited (e.g., show on/off mask plus actual-rate heatmap simultaneously).
     - Provide legend UI showing colors/range labels and rate-bump info (e.g., “<12 GPA: +3% bump”).
     - Offer aggregation visualizations (e.g., mini bar charts beside each section for skips/doubles, harvest moisture histograms, planter double percentage trends) fed by the same controllers.
     - Keep rendering/UI widgets layer-agnostic by binding to shared interfaces so new layer IDs automatically populate checklists, dashboards, and graph selectors without code changes.
   - For overlap highlighting (e.g., 19 GPA), the aggregated value stored in the layer geometry drives the color selection (purple for > target) and the same logic applies to harvest overlays (e.g., moisture outside target band).

4. **UI Integration**
   - Update section status panel to show per-layer values (e.g., actual rate, skip %, double %, downforce) with configurable columns.
  - Add summary strip that aggregates multiple sections (sum or average) for the selected layer and supports “stacked pass” totals (e.g., 5 GPA + 14 GPA = 19 GPA, or combining yield across heads on a combine).
  - Publish built-in derived layers `UnderApplied` (`ratio < targetBand.min`) and `OverApplied` (`ratio > targetBand.max`) so dashboards stay consistent across controllers and firmware implementations.
  - Drive summary strips from layer metadata: averages for ratio layers, sums for volume/seeds, maxima for alarms, all using the same aggregation hints that power mapping.
  - Provide layer manager dialog for enabling/disabling layers, editing thresholds/colors, defining good/under/over zones, mapping UI labels, and adjusting opacity/resolution presets.
  - Surface quick toggles for planter-specific metrics (skips, doubles, singulation) and allow graphing per section/row using the same data feed; reuse the same plumbing for harvest metrics, downforce charts, hopper pressure history, etc.
  - Ensure dashboards, alarms, and graphs are fully layer-agnostic by sourcing labels, units, color ramps, target bands, and alarm thresholds from the layer definition metadata so examples (planter vs. combine vs. sprayer) require zero bespoke UI code.
  - Offer per-row strip views with sparklines or mini-bars driven by the controller’s accumulator history for planter diagnostics, plus quick pins for common metrics (skips, doubles, moisture) built entirely from metadata.
  - Ship a diagnostics “Inspector” panel that shows raw payload bytes, decoded engineering value, quality, weight, and the PGN/transport that produced them for fast field triage.

## Risk Mitigations & Operational Guidelines

- **Aggregation correctness across overlaps**: store numerator, denominator, and area weight per write, then derive averages lazily so slow/fast passes stay accurate. Percent layers retain events/opportunities instead of precomputed percentages; min/max layers update in-place rather than deriving from averages.
- **Temporal decimation & buffering**: respect each layer’s `emitCadenceHz`, coalesce high-rate samples on the controller thread, and move them via reusable queues to the mapping thread. Bench against 5–20 Hz GNSS with 48–64 rows to size buffers.
- **Deadband & smoothing**: apply per-layer `deadbandPct` (default 0.4–0.8 for u8) and controller-side `emaAlpha` before quantization so UI plots stay steady without hiding real shifts.
- **No-data semantics**: treat weight `0` (or validity bit cleared) as “missing”. Controllers convert `0xFF` sentinels to zero-weight entries so renderers and stats skip them automatically.
- **Color ramp parity**: centralize ramp definitions so OpenGL and WPF share the same bins, inclusive edges, and legend labels. Ship a tiny JSON fixture with edge-case bands (exact boundary hits) and unit tests that exercise both GL and WPF renderers against it.
- **Performance & memory**: write value+weight (and optional min/max) per vertex, compute colors on demand, reuse buffers, and allow coarse tile resolution for dense layers.
- **Threading & determinism**: ingest on IO threads, aggregate on a deterministic mapping thread, and hand immutable snapshots to render/UI threads via ring buffers. Sequence counters plus validity maps expose packet loss without freezing the UI.

## Transport & PGN Specification (UDP + CAN)

### Overview

All variable-rate and layer feedback messages use a common payload definition that can travel over both UDP and CAN.

UDP wraps the payload in AOG’s standard message envelope.

CAN uses an 8-byte data frame with the same contents.

PGN numbers and byte order are identical across both transports.

| PGN | Purpose | Payload Size | Transport Notes |
|-----|---------|--------------|-----------------|
| 0xDE (222) | Bit-packed state feedback (u1/u2) | CAN: 3 + (2–4) bytes · UDP: variable (8–64 sections) | Identical payload bytes for UDP/CAN |
| 0xE1 (225) | Per-section u8 block (relative or absolute) | CAN: 8 bytes · UDP: 2 + N | 6 samples (CAN) or 6–64 samples (UDP) |
| 0xE0 (224) | Per-section u16 block (relative or absolute) | CAN: 8 bytes · UDP: 2 + 2N | 3 samples (CAN) or 3–64 samples (UDP) |
| 0xE2 (226) | Layer definition / capability handshake | 8 bytes | Optional startup broadcast + on config change |

### UDP Frame Format

Byte0  0x80        // preamble
Byte1  0x81        // preamble
Byte2  Src         // source ID
Byte3  PGN         // message type
Byte4  Len         // payload length (Data bytes)
Byte5-(4+Len) Data // payload (see below)
Last   CRC         // sum of bytes 2..(n-2)

Example (PGN 0xE1):
80 81 32 E1 08 0C 00 7F 82 7C FF 76 8C A5

### CAN Frame Format

CAN ID:  29-bit J1939 style (PGN in bits 8-23)
DLC:     8
DATA:    identical to UDP Data payload

Example (PGN 0xE1):
DATA = 0C 00 7F 82 7C FF 76 8C

### PGN Data Layouts

0xDE – Bit-Packed State Feedback

Use for u1 (on/off) and u2 (off/low/normal/high).

Byte  Field          Description
0     layerId        Which layer this is
1     bitsPerSample  1 = u1, 2 = u2
2     startSection   First section index
3…    Packed samples for the next 16 sections (LSB-first)
       • u1 → 2 bytes (16 bits)
       • u2 → 4 bytes (32 × 2-bit fields)

Missing samples are represented by clearing the corresponding quality/validity bit (if present) or by writing zero weight in the controller.

0xE1 – u8 Per-Section Block

Use for relative or absolute layers, 6 samples per frame.

Byte  Field          Description
0     layerId        Which layer
1     startSection   Starting section index
2–7   six × u8 samples  0–254 = value, 255 = missing

Semantics:

Relative: 0–254 → 0–200 % of commanded; 255 = missing.

Absolute: linear map [minValue,maxValue]; 255 = missing.

Optional trailer (UDP or CAN Fast-Packet): a 1-byte validity bitmap for the six samples (bit set = valid). Use when the producer needs to flag sparse data without sending `missingCode` placeholders.

0xE0 – u16 Per-Section Block

Use for higher-resolution absolute or relative layers, 3 samples per frame.

Byte  Field          Description
0     layerId        Which layer
1     startSection   Starting section index
2–3   sample 0       (LSB, MSB)
4–5   sample 1       (LSB, MSB)
6–7   sample 2       (LSB, MSB)

Semantics identical to 0xE1 except with 16-bit precision.
0xFFFF = missing.

Optional trailer (UDP or CAN Fast-Packet): a 1-byte validity bitmap for the three samples (bit set = valid). Producers may omit when using `0xFFFF` for missing.

### UDP Multi-Section Frames and Block Sizing

While CAN frames are limited to 8 data bytes, UDP has no such restriction.
All UDP transports use the same PGN payload structure as CAN but allow variable-length frames that include more sections per message.

#### General Rules

- Keep startSection in every frame. It allows multi-module tools, partial updates, and recovery from dropped packets.
- Payload order remains identical to CAN: [layerId][startSection][samples...]
- Len in the UDP header defines how many samples are included.
- Receivers derive the number of sections automatically:

```
sections = (Len - headerBytes) / bytesPerSample
```

(headerBytes = 2 for numeric; 3 for bit-packed)

#### Recommended section counts

| Data Type | Bits/Sample | Typical Sections per Frame (UDP) | Notes |
|-----------|-------------|-----------------------------------|-------|
| u1 (bit-packed) | 1 | 8, 16, 32, 64 | 8 sections = 1 byte; byte-aligned only |
| u2 (bit-packed) | 2 | 8, 16, 32, 64 | 4 sections = 1 byte; byte-aligned only |
| u8 | 8 | 6 (CAN), 12, 16, 24, 32, 48, 64 | 6 used for CAN, larger for UDP |
| u16 | 16 | 3 (CAN), 6, 12, 16, 24, 32, 48, 64 | 3 used for CAN, larger for UDP |

These alignments keep packets byte-aligned and eliminate padding math.

#### Examples

**u8, 64 sections**

PGN: 0xE1
Len = 2 + 64 = 66
[0] layerId
[1] startSection (usually 0)
[2..65] = 64 × u8 samples

**u1, 64 sections**

PGN: 0xDE
Len = 3 + (64/8) = 11
[0] layerId
[1] bitsPerSample = 1
[2] startSection
[3..10] = 8 bytes of packed bits

**u2, 16 sections**

PGN: 0xDE
Len = 3 + (16/4) = 7
[0] layerId
[1] bitsPerSample = 2
[2] startSection
[3..6] = 4 bytes of packed 2-bit codes

#### Recommendations

- CAN: remains fixed at 6 (u8) / 3 (u16) samples per frame.
- UDP: may send 8–64 sections per frame depending on latency and network conditions.
- Full-frame updates (64 sections) are efficient on LAN/Wi-Fi; smaller (8–16) blocks are safer for lossy links.
- One decoder for all: startSection stays mandatory; Len defines how many samples to read.
- Bit-packed alignment: always multiples of 8 sections for u1, 4 for u2.

#### Summary

UDP can transmit any multiple-of-byte block size while preserving the same PGN layout.
This keeps the specification backward-compatible with CAN and ensures identical decoding logic for both transports.

### Optional Sequence Counters & Diagnostics

- Producers MAY append a one-byte sequence counter to UDP frames (and CAN fast-packet streams) to expose packet loss metrics in AgDiag and similar tooling. Consumers ignore the byte when absent.
- Sequence counters should roll over at 255 and only increment when new sensor data is published (not for retransmissions).
- When combined with the validity bitmap trailer, place the sequence byte last.

0xE2 – Layer Definition / Capability Broadcast

Sent once at startup or on config change.

Byte  Field          Description
0     layerId        Unique layer ID (0–239 reserved for core catalog)
1     versionHi      Upper nibble = `transportVersion`, lower nibble = `schemaMajor`
2     schemaMinor    LayerDefinition schema minor version
3     schemaPatch    LayerDefinition schema patch version
4     dataInfo       Bits 0-1 `dataType` (0=bitpacked,1=u8,2=u16), bit2 `valueMode` (0=absolute,1=relative), bits3-4 `bitsPerSample` (for bitpacked), bits5-7 reserved
5     capabilities   Bitfield: 0=quality support, 1=validity bitmap, 2=area-weighted averages, 3=stacked sum summary, 4=derived layers advertised, 5=rateNAFlag, 6-7 reserved
6     minHint (u8)   Optional normalized hint (0–254) for UI ramps
7     maxHint (u8)   Optional normalized hint (0–254) for UI ramps

Notes

Identical payloads → same decoder for both CAN and UDP.

CRC only exists in UDP; CAN uses its built-in CRC.

Aggregation rules: numeric layers use area-weighted averages; bit-packed layers use logical OR/max.

Update cadence: 10 Hz typical (adjustable via emitCadenceHz).

Backwards-compatible: existing section bitmaps remain valid.

`transportVersion` increments whenever payload semantics change; decoders should warn (and optionally fall back) if they receive an unsupported version.

Producers SHOULD rebroadcast after any schema revision or capability change so controllers and UI remain in sync.

### Rate Limit Planning

| Rig / Layer Mix | Sections | Data Type | Cadence | Frames per Second (UDP) | Notes |
|-----------------|----------|-----------|---------|-------------------------|-------|
| 24-row planter, Actual vs Commanded | 24 | u8 relative | 10 Hz | 2 (12 samples per frame) | Leaves headroom for skips/doubles on same link |
| 48-row planter, Actual + Skips + Doubles | 48 | u8 + u8 + u8 | 10 Hz | 6 (three frames, two layers multiplexed) | Keep per-frame payload ≤ 512 bytes on Wi-Fi |
| 64-section sprayer, Working + Flow State + GPA | 64 | u1 + u2 + u8 | 10 Hz | 4 (one frame each for u1/u2, two for u8) | Maintain <40 packets/sec on UDP |
| Combine, Yield + Moisture | 16 | u16 + u8 | 5 Hz | 2 (one per layer) | Favor lower cadence for long-range radios |

Guideline: keep total transport load below ~50 packets/sec on hobby-grade networks and monitor AgDiag loss counters when adding layers.

## LayerDefinition Examples

Each layer in the system is defined by a LayerDefinition record that provides metadata for scaling, visualization, and aggregation.
The following examples illustrate typical configurations for the main data types and value modes used by AgOpenGPS variable-rate layers.

### Example 1 — u1 Bitpacked “Working” State

Simple on/off feedback (8 sections per byte).
Transmitted via PGN 0xDE with bitsPerSample = 1.

```
{
  "schemaVersion": "1.0.0",
  "id": 1,
  "name": "Working State",
  "units": "state",
  "unitsId": "state.onOff",
  "dataType": "bitpacked",
  "bitsPerSample": 1,
  "states": [
    { "code": 0, "label": "Off", "color": "#555555" },
    { "code": 1, "label": "On",  "color": "#00C853" }
  ],
  "aggregation": "logical",
  "compositeRule": "or",
  "storagePrecision": "u8",
  "deadbandPct": 0.0,
  "emaAlpha": 1.0,
  "tileResolutionMultiplier": 1,
  "emitCadenceHz": 10
}
```

### Example 2 — u2 Bitpacked “Flow State”

4 sections per byte. Useful for spray flow visualization.
Transmitted via PGN 0xDE with bitsPerSample = 2.

```
{
  "schemaVersion": "1.0.0",
  "id": 2,
  "name": "Flow State",
  "units": "state",
  "unitsId": "state.flowState",
  "dataType": "bitpacked",
  "bitsPerSample": 2,
  "states": [
    { "code": 0, "label": "Off",    "color": "#444444" },
    { "code": 1, "label": "Low",    "color": "#FFD600" },
    { "code": 2, "label": "Normal", "color": "#00C853" },
    { "code": 3, "label": "High",   "color": "#D50000" }
  ],
  "aggregation": "logical",
  "compositeRule": "max",
  "storagePrecision": "u8",
  "deadbandPct": 0.0,
  "emaAlpha": 0.9,
  "tileResolutionMultiplier": 1,
  "emitCadenceHz": 10
}
```

### Example 3 — u8 Relative “Actual vs Commanded”

6 sections per frame. Linear 0–200 % scaling, 255 = missing.
Transmitted via PGN 0xE1.

```
{
  "schemaVersion": "1.0.0",
  "id": 10,
  "name": "Actual vs Commanded",
  "units": "%",
  "unitsId": "ratio.percent",
  "dataType": "u8",
  "valueMode": "relative",
  "rangePercent": { "min": 0, "max": 200 },
  "missingCode": 255,
  "targetBand": { "min": 95, "max": 105 },
  "displayRanges": [
    { "min": 0, "max": 95,  "label": "Under", "color": "#2196F3" },
    { "min": 95, "max": 105,"label": "On",    "color": "#43A047" },
    { "min": 105,"max": 200,"label": "Over",  "color": "#E53935" }
  ],
  "alarmBands": [
    { "min": 0, "max": 90, "label": "Severe Under", "color": "#1565C0", "hysteresis": 2, "holdMs": 3000 },
    { "min": 110, "max": 200, "label": "Severe Over", "color": "#B71C1C", "hysteresis": 2, "holdMs": 3000 }
  ],
  "aggregation": "avg",
  "compositeRule": "avg",
  "storagePrecision": "u8",
  "deadbandPct": 0.6,
  "emaAlpha": 0.25,
  "tileResolutionMultiplier": 1,
  "rateNAFlag": "actualRateNA",
  "derivedLayers": ["UnderApplied", "OverApplied"],
  "qualityRules": [
    { "condition": "q < 0.5", "effect": "dim" }
  ],
  "emitCadenceHz": 10
}
```

### Example 4 — u8 Absolute “Downforce Pressure”

6 sections per frame. 0–300 PSI mapped to 0–254.
Transmitted via PGN 0xE1.

```
{
  "schemaVersion": "1.0.0",
  "id": 20,
  "name": "Downforce Pressure",
  "units": "PSI",
  "unitsId": "pressure.psi",
  "dataType": "u8",
  "valueMode": "absolute",
  "minValue": 0,
  "maxValue": 300,
  "missingCode": 255,
  "targetBand": { "min": 80, "max": 120 },
  "displayRanges": [
    { "min": 0,   "max": 80,  "label": "Low",  "color": "#2196F3" },
    { "min": 80,  "max": 120, "label": "Good", "color": "#43A047" },
    { "min": 120, "max": 300, "label": "High", "color": "#E53935" }
  ],
  "alarmBands": [
    { "min": 0, "max": 70, "label": "Critical Low", "color": "#0D47A1", "hysteresis": 5, "holdMs": 2000 },
    { "min": 130, "max": 300, "label": "Critical High", "color": "#C62828", "hysteresis": 5, "holdMs": 2000 }
  ],
  "aggregation": "avg",
  "compositeRule": "avg",
  "storagePrecision": "u8",
  "deadbandPct": 0.5,
  "emaAlpha": 0.3,
  "tileResolutionMultiplier": 2,
  "qualityRules": [
    { "condition": "q < 0.4", "effect": "desaturate" }
  ],
  "emitCadenceHz": 10
}
```

### Example 5 — u16 Absolute “Yield”

High-resolution analog layer with fine precision.
Transmitted via PGN 0xE0.

```
{
  "schemaVersion": "1.0.0",
  "id": 30,
  "name": "Yield Mass Flow",
  "units": "kg/ha",
  "unitsId": "mass.kg_ha",
  "dataType": "u16",
  "valueMode": "absolute",
  "minValue": 0,
  "maxValue": 20000,
  "missingCode": 65535,
  "targetBand": { "min": 9000, "max": 12000 },
  "displayRanges": [
    { "min": 0,     "max": 9000,  "label": "Low",  "color": "#42A5F5" },
    { "min": 9000,  "max": 12000, "label": "Good", "color": "#66BB6A" },
    { "min": 12000, "max": 20000, "label": "High", "color": "#EF5350" }
  ],
  "alarmBands": [
    { "min": 0, "max": 8500, "label": "Under Target", "color": "#1E88E5", "hysteresis": 200, "holdMs": 5000 },
    { "min": 12500, "max": 20000, "label": "Above Target", "color": "#D32F2F", "hysteresis": 200, "holdMs": 5000 }
  ],
  "aggregation": "avg",
  "compositeRule": "avg",
  "storagePrecision": "u16",
  "deadbandPct": 0.4,
  "emaAlpha": 0.2,
  "tileResolutionMultiplier": 2,
  "qualityRules": [
    { "condition": "q < 0.6", "effect": "dim" }
  ],
  "emitCadenceHz": 5
}
```

### Usage Notes

- `schemaVersion` aligns with the `0xE2` broadcast; mismatches trigger controller warnings.
- `dataType` + `valueMode` dictate how each PGN’s payload is interpreted, while `storagePrecision` selects the transport block.
- `states[]` only applies to bitpacked layers.
- `minValue` / `maxValue` apply to absolute; `rangePercent` to relative; `missingCode` is locked to 255 (`u8`) and 65535 (`u16`).
- `deadbandPct` and `emaAlpha` are applied controller-side before quantization to stabilize UI readouts.
- `qualityRules` evaluate the stored `q` weight (0–1) to dim/desaturate sparse data without altering numeric summaries.
- `alarmBands` support `hysteresis` and `holdMs` to prevent chatter; dashboards reuse the same metadata for alerts.
- `rateNAFlag` fires when the commanded rate is effectively zero so firmware and UI avoid divide-by-zero cases.
- `derivedLayers` advertise canonical virtual overlays (e.g., UnderApplied/OverApplied) that the UI may auto-enable.
- `unitsId` references the shared registry so dashboards render consistent symbols and conversions.
- `tileResolutionMultiplier` hints to persistence and rendering about acceptable downsampling.
- UI color ramps, legends, and dashboards remain fully metadata-driven from these definitions.

## UI & UX Plan

### Existing Layout Touchpoints

- The WPF shell hosts a header, side button strips, and central viewport (`SourceCode/AgOpenGPS.WpfApp/MainWindow.xaml`).
- The header bar is currently an empty grid ready to receive summary widgets (`SourceCode/AgOpenGPS.WpfApp/MainViews/HeaderBar.xaml`).
- Left/right button strips provide vertical stacks of commands; the left strip already exposes configuration and field-selection entries (`SourceCode/AgOpenGPS.WpfApp/MainViews/LeftButtonStrip.xaml`).
- Legacy WinForms dialogs such as `ConfigVehicle` still handle section width and controller assignment workflows (`SourceCode/GPS/Forms/Settings/ConfigVehicle.Designer.cs`).

### Run Screen Enhancements

1. **Summary Metric Row (Header Bar)**
   - Populate the header grid with responsive metric tiles (Avg Downforce, Avg Skips, Working %, etc.) that display value, unit, quality indicator, and recent trend sparkline.
   - Tiles are metadata-driven—layer definitions flag which summaries appear and what aggregation to show (avg/sum/max).
   - Clicking a tile opens the associated drill-down chart and highlights the layer in the legend.

2. **Layer Legend & Toggles (Right Strip)**
   - Repurpose the right button stack as a collapsible legend showing active layers, color swatches, opacity sliders, derived-layer toggles (Under/Over Applied), and alarm acknowledgements.
   - Include quick filters to jump between planter, sprayer, and combine presets sourced from layer grouping tags.

3. **Chart Drawer (Bottom Strip + Viewport Overlay)**
   - The bottom strip opens a slide-up drawer anchored to the viewport overlay for bar charts and histograms.
   - Charts respect relative vs. absolute scaling, offer compare-all-rows vs. selected-row modes, and reuse aggregation hints (avg/percent/sum) from layer metadata.
   - Provide presets: per-row bar chart, stacked-pass volume chart, and time-series trendline—all configured without hard-coded layer IDs.

4. **Per-Row Inspector (Viewport Overlay)**
   - Extend `ViewportOverlay` to show hover tooltips with decoded value, unit, quality, weight, and `rateNA` flags for the closest section.
   - Allow users to “pin” an inspector card that also displays the raw PGN bytes and transport info for diagnostics.

### Configuration Flows

1. **Layer Definition Manager**
   - Accessible from the existing Configuration button in the left strip, opening a WPF dialog with tabs for *Layer Catalog*, *Derived Layers*, and *Transport*.
   - Supports import/export of JSON, editing schemaVersion, smoothing, alarm hysteresis, quality rules, and unit selection backed by the shared registry.
   - Validates missing codes, `rateNAFlag` behavior, and warns if a u16 layer is assigned to an 8-bit-only controller.

2. **Firmware & Module Setup**
   - Adds a “Modules & Firmware” tab to the tool/vehicle configuration sequence (bridging to the existing WinForms dialog until the WPF migration completes).
   - Lists discovered CAN/UDP modules, firmware revisions, supported PGNs, and allows flashing or tuning controller parameters (emit cadence, deadband, EMA alpha).
   - Provides mapping between hardware channels and logical layers, including startSection offsets and UDP block sizing for multi-module rigs.

3. **Section & Coverage Settings**
   - Augment the current section configuration with per-layer defaults, coverage factors, and preview of how overlapping passes aggregate (area-weighted avg vs. stacked sum).
   - Enable quick duplication from presets (e.g., 48-row planter) and highlight sections missing required layers (skips, doubles, downforce).

### Dashboard & Drill-Down Behavior

- Metric tiles drive drill-down panels that can dock to the right strip or float over the viewport.
- Charts expose a scale toggle—relative interprets stored numerator/denominator as %, absolute displays engineering units using `minValue`/`maxValue`.
- Users can bookmark chart layouts per layer so planter or harvester operators recall favorite dashboards instantly.
- Summary strip totals follow aggregation hints: averages for ratio layers, sums for volume/seeds, maxima for alarm layers, plus optional “stacked pass” totals for overlaps.

### Diagnostics & Monitoring

- The inspector panel surfaces raw payload bytes, decoded engineering value, quality, weight, and source PGN, mirroring AgDiag to simplify field triage.
- Add a legend parity test shortcut that renders the shared color-ramp fixture in both OpenGL and WPF to confirm matching bin edges.
- Provide a packet-rate monitor overlay (reuse AgDiag graphs) so operators can see when UDP/CAN utilization nears the rate-limit guidance.

## Persistence
- Extend project profile JSON/INI to include layer definitions and per-field overrides.
- Store historical layer maps alongside existing section map files (e.g., `*.agl`), using a chunked binary format: header (`magic`, version, CRS, cell size, quantization bits, storagePrecision`) followed by chunks keyed by `(layerId, zoomTile)` compressed with LZ4/Zstd. Export to GeoTIFF as a post-process tool when needed.
- Support optional downsampling-on-write per layer using `tileResolutionMultiplier` so high-frequency feeds reduce footprint without losing coverage.
- Include quantization metadata (min/max, missingCode, scale) in each chunk so replay/export tools can reconstruct engineering values exactly.
- Provide CSV export utilities for per-section and per-pass summaries driven by the same accumulators powering dashboards.
- Hash only schema-critical fields (`schemaVersion`, `id`, `unitsId`, `dataType`, `storagePrecision`, `targetBand`) when persisting controller bindings so cosmetic changes (legend labels) do not invalidate firmware handshakes mid-field.

## Implementation Phases
1. **Core Types & Tests**: introduce `LayerDefinition`/`LayerController` classes, update section model to own controllers, and add unit tests for overlap math (area-weighted rates, percent numerator/denominator, min/max updates, boolean OR).
2. **Rendering Path**: refactor mapping pipeline to support per-layer geometry while preserving legacy binary strips. Extract shared color ramp utility with test coverage for bin boundaries and legend labels.
3. **Aggregation & Buffering**: implement throttled emission (`emitCadenceHz`), ring buffers per layer, and accumulators carrying numerator/denominator/min/max/weight. Benchmark 48–64 rows @ 10 Hz ingest and ensure 30–60 FPS rendering.
4. **UI Enhancements**: add layer visibility toggles, opacity controls, legends, per-row strip view, summary widgets, and planter/harvest dashboards driven entirely by metadata.
5. **IO & Interop**: wire `0xE4`/`0xE3` commands, `0xE2` definition handshake, `0xE1`/`0xE0` feedback blocks (with sequence counters), and `0xDF` aggregates. Provide AgDiag simulators for deterministic record/replay and ship a “Variable-Rate PGNs” feature flag (on by default for UDP, optional for CAN, with a legacy-only fallback).
6. **Persistence**: implement the chunked storage format with write-behind throttling, persist layer registries with schema hashes, and supply export tooling (GeoTIFF/CSV) as optional utilities.

## Recommendations
- Keep existing section binary mapping untouched for backwards compatibility; layer controllers can be optional.
- Use dependency injection (where available) so new layers can be registered by plugins (e.g., planter monitor modules) and auto-discovered by the UI.
- Consider adopting a common color ramp utility so both OpenGL and WPF views interpret display ranges identically, sharing ramp definitions, legend text, and inclusive/exclusive edge rules.
- Create unit tests for aggregation math using representative multi-pass scenarios (e.g., 5 GPA + 14 GPA = 19 GPA => “Over Applied”) and planter-specific metrics (skip % + double %).
- Document the layer API so firmware teams (like SK21) can publish data without tight coupling to UI internals. Include guidance on scaling raw sensor data into the 0–100 normalization band and handling missing samples (0xFF → weight 0).
- Provide example configurations for planters, sprayers, and fertilizer rigs so users can see how multiple analog/digital feeds become layers.
- Keep shipped dashboards/examples declarative; UI plugins should consume layer metadata (name, units, aggregation, color ramps) instead of branching on specific IDs so new combines/planters add layers without code.
- Ingest on an IO thread, aggregate on a dedicated mapping thread, render on the UI thread, and pass immutable snapshots between them to avoid tearing. Use per-layer ring buffers to isolate firmware bursts from render cadence.
- Maintain a published layer ID registry (core catalog IDs fixed, 240–255 reserved for third-party namespaces) and expose it in docs and firmware headers.
- Enforce bounds checks on decoded samples: drop and count any value outside `[minValue,maxValue] * 1.5` (or `rangePercent`) or if parsing yields NaN/Inf.

## Interop & Safety

- **Layer ID registry**: recommend fixed IDs for common feeds (e.g., 1=Working, 2=Flow State, 10=Actual vs Commanded, 20=Downforce, 30=Yield, 40=Moisture). Reserve 240–255 for third-party/experimental namespaces.
- **Time base**: when including timestamps, use a monotonic millisecond counter since controller boot. GNSS time may supplement but should not replace the monotonic clock.
- **Bounds & counters**: increment a `badSample` counter whenever a decoded value is NaN/Inf or outside `[min,max] * 1.5` (absolute) / `[rangePercent.min, rangePercent.max] * 1.5` (relative), and drop the sample.
- **Safety fallbacks**: if a layer definition or transport version is unknown, log the issue once, disable mapping for that layer, and leave legacy section control unaffected.

## Testing & Rollout

- **Golden replays**: capture AgDiag recordings for a 64-row planter, a 5-section sprayer, and a combine. CI should replay them to produce deterministic PNGs, summary tables, and `badSample` counts.
- **Performance acceptance**: target ≤3% CPU and ≤150 MB RAM after a 1-hour session with 64 rows at 10 Hz ingest while sustaining 30–60 FPS rendering.
- **Regression protection**: compare aggregator outputs (sum/avg/min/max) across releases using the replay suite and fail CI on deviations.
- **Feature flags**: ship “Variable-Rate PGNs” enabled for UDP, optional for CAN, and provide a “Legacy-only” toggle to disable new layers on older rigs.
- **Ops readiness**: document inspector workflow, rate-limit guidelines, and troubleshooting steps in release notes so dealers can triage in the field.

