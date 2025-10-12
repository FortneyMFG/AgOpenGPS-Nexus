# Interface Specifications

## Gauge block PGNs (read-only telemetry)
| PGN | Name | Direction | Transport(s) | Payload summary |
|---|---|---|---|---|
| 0xDA | Gauge Block (u16, three signals) | Controller → AOG | CAN (fixed 8-byte), UDP (variable-length) | Packs up to three gauge IDs with 16-bit raw values. |
| 0xD9 | Gauge Block (u8, up to six signals) | Controller → AOG | CAN (fixed 8-byte), UDP (variable-length) | Compact transport for gauges that naturally fit in one byte. |
| 0xD8 | Gauge Heartbeat / Summary | Controller → AOG | CAN/UDP | Publishes uptime, sequence, and validity bitmap for diagnostics. |

### 0xDA – Gauge Block (u16, three signals)
- **Layout (CAN):**
  - Byte 0: `gaugeId0`
  - Byte 1: `gaugeId1`
  - Byte 2: `gaugeId2`
  - Byte 3: Reserved (set to 0)
  - Bytes 4–5: `value0` (LSB, MSB)
  - Bytes 6–7: `value1` (LSB, MSB)
- **Value 2 handling:** Send a follow-up 0xDA frame containing the same `gaugeId2` in byte 0 and `value2` in bytes 4–5, or (for UDP) append additional bytes following the base structure to deliver `value2`.
- **Scaling:** Determined by the gauge definition (`scale`, `offset`, `dataType`).
- **Missing value:** `0xFFFF` denotes unavailable data.

### 0xD9 – Gauge Block (u8, up to six signals)
- **Layout (CAN):**
  - Bytes 0–5: `segmentPayload`
    - When `segment` (byte 6) is `0`, the payload contains `gaugeId0..gaugeId5`.
    - When `segment` is `1`, the payload contains `value0..value5` aligned with the previously delivered IDs.
  - Byte 6: `segment`
    - `0`: Gauge ID segment (announces up to six IDs).
    - `1`: Value segment (delivers the matching byte values).
  - Byte 7: `count` (number of gauges in this block, 1–6).
- **CAN sequencing:** Send an ID segment first (segment = 0) followed immediately by a value segment (segment = 1) using the same `count`. Receivers cache the most recent ID segment per source and apply subsequent value segments until a new ID segment arrives. Missing values use `0xFF`.
- **UDP extension:** Continue to support the variable-length `[gId0..gIdN][value0..valueN]` envelope inside a single datagram; the CAN segmentation rule does not apply to UDP payloads.
- **Guidance:** Use 0xD9 for gauges that natively fit in 8 bits and reserve 0xDA for wider ranges.

### 0xD8 – Gauge Heartbeat / Summary
- **Layout (CAN):**
  - Byte 0: `uptimeSeconds` (mod 256)
  - Byte 1: `sequence`
  - Bytes 2–3: `sourceId`
  - Bytes 4–7: Validity bitfield (`gaugeId` / 32 index)
- **UDP extension:** Append additional validity words as needed and optionally terminate with a sequence trailer when `capabilities.seq = 1`.
- **Purpose:** Allows dashboards to confirm publisher liveness, correlate packet-loss, and mark gauges stale without per-gauge ACKs.

### UDP framing rules
- Maintain the existing AOG UDP variable-length contract: gauge blocks may extend beyond 8 bytes so long as IDs appear contiguously before their raw values.
- Optional 1-byte sequence trailers follow the payload when gauge publishers advertise sequencing support.

### Coexistence with existing PGNs
- Gauge PGNs do not replace or modify existing layer, rate, or steering PGNs. Controllers emit them alongside current telemetry without reusing section-specific PGN IDs (e.g., `0xE1/0xE0`).
- Receivers map incoming `gaugeId` values to JSON-defined gauges. If a gauge is unrecognized, the payload is ignored but logged for diagnostics.
