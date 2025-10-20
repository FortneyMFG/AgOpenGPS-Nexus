# O-COMM-7: Gauge telemetry PGNs for engine & machine data

## Summary
Define a read-only PGN suite (0xDA/0xD9/0xD8) that carries engine and machine gauges over CAN and UDP. The transport keeps the
existing AgOpenGPS framing so firmware can publish telemetry without blocking current section or steering PGNs. Dashboards
interpret gauge metadata (IDs, scaling, validity) supplied via JSON definitions.

## Details
- **Gauge block PGNs (read-only telemetry)**
  | PGN | Name | Direction | Transport(s) | Payload summary |
  |---|---|---|---|---|
  | 0xDA | Gauge Block (u16, three signals) | Controller → AOG | CAN (fixed 8-byte), UDP (variable-length) | Packs up to three gauge IDs with 16-bit raw values. |
  | 0xD9 | Gauge Block (u8, up to six signals) | Controller → AOG | CAN (fixed 8-byte), UDP (variable-length) | Compact transport for gauges that naturally fit in one byte. |
  | 0xD8 | Gauge Heartbeat / Summary | Controller → AOG | CAN/UDP | Publishes uptime, sequence, and validity bitmap for diagnostics. |
- **0xDA – Gauge Block (u16, three signals)**
  - Byte 0: `gaugeId0`
  - Byte 1: `gaugeId1`
  - Byte 2: `gaugeId2`
  - Byte 3: Reserved (set to 0)
  - Bytes 4–5: `value0` (LSB, MSB)
  - Bytes 6–7: `value1` (LSB, MSB)
  - Value 2 handling: Send a follow-up 0xDA frame containing the same `gaugeId2` in byte 0 and `value2` in bytes 4–5, or (for UDP) append additional bytes following the base structure to deliver `value2`.
  - Scaling: Determined by the gauge definition (`scale`, `offset`, `dataType`).
  - Missing value: `0xFFFF` denotes unavailable data.
- **0xD9 – Gauge Block (u8, up to six signals)**
  - Bytes 0–5: `segmentPayload`
    - When `segment` (byte 6) is `0`, the payload contains `gaugeId0..gaugeId5`.
    - When `segment` is `1`, the payload contains `value0..value5` aligned with the previously delivered IDs.
  - Byte 6: `segment`
    - `0`: Gauge ID segment (announces up to six IDs).
    - `1`: Value segment (delivers the matching byte values).
  - Byte 7: `count` (number of gauges in this block, 1–6).
  - CAN sequencing: Send an ID segment first (segment = 0) followed immediately by a value segment (segment = 1) using the same `count`. Receivers cache the most recent ID segment per source and apply subsequent value segments until a new ID segment arrives. Missing values use `0xFF`.
  - UDP extension: Continue to support the variable-length `[gId0..gIdN][value0..valueN]` envelope inside a single datagram; the CAN segmentation rule does not apply to UDP payloads.
  - Guidance: Use 0xD9 for gauges that natively fit in 8 bits and reserve 0xDA for wider ranges.
- **0xD8 – Gauge Heartbeat / Summary**
  - Byte 0: `uptimeSeconds` (mod 256)
  - Byte 1: `sequence`
  - Bytes 2–3: `sourceId`
  - Bytes 4–7: Validity bitfield (`gaugeId` / 32 index)
  - UDP extension: Append additional validity words as needed and optionally terminate with a sequence trailer when `capabilities.seq = 1`.
  - Purpose: Allows dashboards to confirm publisher liveness, correlate packet-loss, and mark gauges stale without per-gauge ACKs.
- **UDP framing rules**
  - Maintain the existing AOG UDP variable-length contract: gauge blocks may extend beyond 8 bytes so long as IDs appear contiguously before their raw values.
  - Optional 1-byte sequence trailers follow the payload when gauge publishers advertise sequencing support.
- **Coexistence with existing PGNs**
  - Gauge PGNs do not replace or modify existing layer, rate, or steering PGNs. Controllers emit them alongside current telemetry without reusing section-specific PGN IDs (e.g., `0xE1/0xE0`).
  - Receivers map incoming `gaugeId` values to JSON-defined gauges. If a gauge is unrecognized, the payload is ignored but logged for diagnostics.

## Pros
- Reuses proven AgIO CAN/UDP framing so firmware and dashboards can adopt the feature incrementally.
- Supports both byte-wide and 16-bit gauges without exploding PGN IDs or message sizes.
- Heartbeat semantics give dashboards a consistent way to mark sensors stale or offline.

## Cons
- Adds new PGNs that must be rolled out across every firmware image and dashboard before gauges become useful.
- Still depends on out-of-band JSON metadata for scaling and presentation, so misconfigured definitions can yield wrong values.

## Risks & mitigations
- **Transport drift:** Stick to existing frame sizes and sequencing rules to avoid breaking current AgIO tooling.
- **Gauge ID collisions:** Maintain a shared registry and validation tooling to prevent duplicate `gaugeId` assignments.
- **Partial adoption:** Provide feature flags and backward-compatible fallbacks so rigs without the new PGNs continue working.

## Borrowables
- AgIO UDP monitor and PGN designer for testing payloads.
- Existing JSON layer definition tooling for schema validation.

## Rough effort
- **Medium:** Requires firmware updates (new PGNs + heartbeats), dashboard parsing/rendering, and registry/tooling support.

## References
- J1939/ISOBUS mappings in [Section 15 – Engine & Machine Gauges](../sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md).
- Current PGN framing documented in [AgIO PGN baseline](../references/AgIO_PGN_Baseline.md).

## Related ADRs

- [ADR-016 — Firmware Transport Variable Rate PGNs](../../ADR/ADR-016-firmware-transport-variable-rate-pgns.md)
- [ADR-047 — Live Telemetry Mesh](../../ADR/ADR-047_LiveTelemetryMesh.md)
- [ADR-017 — Profiles & Kinematics](../../ADR/ADR-017-profiles-kinematics.md)
