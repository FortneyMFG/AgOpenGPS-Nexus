# O-COMM-5: Versioned variable-rate PGN suite

## Summary
Defines a set of CAN/UDP PGNs and sequencing rules that stream variable-rate layers, acknowledge layer definitions, and guard against packet loss while remaining backward compatible with existing AgIO transports.

## Details
- Raw sensor handlers push analog/digital samples (`actualRate`, `skipPct`, `doublePct`, `downforce`, `hopperPressure`, custom inputs) into layer controllers using PGNs `0xE1` (u8) and `0xE0` (u16) plus packed binary feeds on `0xDE` (up to eight bit-indexed signals per byte).
- Introduce `0xE4`/`0xE3` commands, a definition handshake `0xE2`, aggregated summaries `0xDF`, and sequence counters on payload blocks so controllers detect drops and request retransmission/replay if needed.
- When commanded rate ≤ ε (per-layer tolerance), mark samples as missing, assert `rateNAFlag`, and avoid divide-by-zero scenarios for relative layers.
- Permit multiple packets within a time slice to feed the same layer; controllers buffer and reconcile according to configured aggregation (mean for analog, OR for boolean, numerator/denominator for percentages).
- Provide monotonic millisecond timestamps alongside optional GNSS time, ensuring modules that lack GNSS still produce ordered samples.
- Publish bounds guidance: drop samples outside `[min,max] * 1.5` (absolute) or `[rangePercent.min, rangePercent.max] * 1.5` (relative) and increment `badSample` counters for diagnostics.
- Reserve layer IDs (1=Working, 2=Flow State, 10=Actual/Commanded, 20=Downforce, 30=Yield, 40=Moisture, 240–255=third-party) so firmware and UI share a registry.

## Pros
- Backwards compatible with today’s UDP/CAN framing while adding sequencing, schema negotiation, and error tracking.
- Supports high-rate sensors without overwhelming the UI by delegating smoothing and aggregation to controllers.
- Clear layer ID registry simplifies cross-team collaboration and plugin development.

## Cons
- Adds firmware complexity to implement new PGNs, sequence counters, and definition handshakes.
- Requires updated diagnostics tooling to decode new payloads and registry metadata.
- Increases bandwidth needs if many layers stream simultaneously without downsampling.

## Risks & mitigations
- **Risk:** Firmware/app version skew leads to rejected samples. **Mitigation:** Include schema hash exchange during handshake and provide logging/UI indicators for mismatches.
- **Risk:** Bandwidth pressure on low-speed links. **Mitigation:** Allow per-layer cadence throttling and packed binary feeds for digital-only signals.
- **Risk:** Debug difficulty when multiple PGNs interact. **Mitigation:** Extend inspector tools to surface raw payload bytes, decoded engineering values, quality, and source PGN metadata.

## Borrowables
- Current AgIO UDP monitor and AgDiag utilities already parse PGNs and can be extended for the new IDs.
- Existing heartbeat/watchdog logic can be reused to supervise the new sequencing flows.
- The legacy section-control PGNs provide patterns for acknowledgement and retry behavior.

## Rough effort
M — Requires firmware updates, AgIO decoding changes, schema negotiation logic, and documentation but reuses the established transport stack.

## References
- [Section 51 — Sensor & Actuator Abstractions](../sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md)
- [Section 61 — Kinematics & Pose Fusion](../sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md)
- [AgIO PGN baseline](../references/AgIO_PGN_Baseline.md)

## Related ADRs
- [ADR-015 — Section Control Grouping Semantics](../../ADR/ADR-015-section-control-grouping-semantics.md)
- [ADR-016 — Firmware Transport Variable Rate PGNs](../../ADR/ADR-016-firmware-transport-variable-rate-pgns.md)
- [ADR-047 — Live Telemetry Mesh](../../ADR/ADR-047_LiveTelemetryMesh.md)

