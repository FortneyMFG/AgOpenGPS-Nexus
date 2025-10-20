# ADR-016: Firmware and transport for variable-rate layer PGNs

## Status
Drafting (target review window: 2025-11-18 week)

**Relevant Plugin(s):** Rate Control, Section Control, Variable Mapping, AgIO Host Services, Planter Monitor, ISOBUS Bridge


## Context
Delivering layer definitions and feedback between Core, AgIO, and implement firmware requires deterministic CAN/UDP messages aligned with the new layer registry and section control semantics. Legacy PGNs do not cover registry hashes or degraded mode signaling. ADR-016 specifies the transport contracts so firmware, simulators, and the AgIO bridge can exchange variable-rate information with bounded latency.

## Decision
- Define CAN and UDP message suites for layer definitions and feedback (E2/E1/E0/DF/E3/E4) including node IDs, sequencing, timeout, and heartbeat semantics.
- Integrate registry hash handshakes (ADR-010) to ensure firmware and Core operate on matching layer catalogs with fail-safe fallbacks.
- Specify degraded-mode and heartbeat policies that keep sections fail-safe when transport errors occur, including telemetry diagnostics.
- Provide reference firmware stubs, simulators, and conformance tests to validate interoperability across transports.

## Consequences
- Firmware and AgIO bridge gain clear expectations for variable-rate messaging but must implement additional handshake logic and telemetry.
- Transport specifications improve safety by ensuring mismatched hashes fail-safe quickly, though they increase implementation complexity.
- CI and simulation infrastructure must expand to cover latency, jitter, and error injection scenarios for both CAN and UDP paths.

## Governance Updates
- **Timing reference implementations.** Shared firmware examples include jitter injectors and watchdog tunables. Vendors must certify against the reference suite before distributing updates.
- **Shared conformance lab.** Nexus QA operates a lab with CAN/UDP harnesses, publishing monthly health summaries and escalating regressions within 48 hours.
- **Configurable safety thresholds.** Specifications now expose parameterized watchdog sensitivity with documented safe ranges, allowing deployments to adjust without forking firmware.

## Validation
- Firmware simulators must demonstrate end-to-end PGN exchange with ≤ 15 ms jitter at 20 Hz over CAN and ≤ 25 ms over UDP.
- Registry hash mismatches must trigger degraded mode within 200 ms and log actionable error codes for diagnostics.
- Heartbeat watchdog tests must prove sections fail closed after 300 ms of missed heartbeats and recover automatically when communication resumes.

## References
<<<<<<< HEAD
- [Communications & transports requirements](../SRS/sections/4X_Interprocess_Communications/42_Transports.md)
- [Hardware I/O requirements](../SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md)
=======
- [Communications & transports requirements](../SRS/sections/4X/42_Transports.md)
- [Hardware I/O requirements](../SRS/sections/5X/51_Sensor_Actuator_Abstractions.md)
>>>>>>> origin/develop
- [ADR-010: Layer registry and variable-rate framework](ADR-010-layer-registry-variable-rate.md)
- [ADR-015: Section control and grouping semantics](ADR-015-section-control-grouping-semantics.md)
- [ADR-031: Official plugin bundle governance](ADR-031-official-plugin-bundle.md)
