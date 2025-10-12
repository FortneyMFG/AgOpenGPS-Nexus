# Interface Specifications

## Gauge block PGNs (read-only telemetry)
| PGN | Name | Direction | Transport(s) | Payload summary |
|---|---|---|---|---|
| 0xDA | Gauge Block (u16, three signals) | Controller → AOG | CAN (fixed 8-byte), UDP (variable-length) | Packs up to three gauge IDs with 16-bit raw values. |
| 0xD9 | Gauge Block (u8, six signals) | Controller → AOG | CAN (fixed 8-byte), UDP (variable-length) | Packs up to six gauge IDs with 8-bit raw values. |
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

### 0xD9 – Gauge Block (u8, six signals)
- **Layout (CAN):**
  - Bytes 0–5: `gaugeId0..gaugeId5`
  - Byte 6: `value0`
  - Byte 7: `value1`
- **UDP extension:** For UDP, extend the payload to `[gId0..gIdN][value0..valueN]` with contiguous ID/value arrays. Use this variant for inherently 8-bit gauges (e.g., coolant temp).
- **Scaling:** Determined by gauge metadata. Missing values use `0xFF`.
- **Guidance:** Prefer 0xDA unless the signal natively fits in 8 bits.

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
