# O-HW-5: Modular firmware publishing variable-rate layers

## Summary
Expands hardware IO expectations so CAN/UDP modules (e.g., SK21 AOG_RC and future controllers) can publish multiple variable-rate layers per section, advertise firmware capabilities, and map physical channels to logical layers without core code changes.

## Details
- Firmware exposes discovery data listing supported PGNs, firmware revisions, and available channels so the app can map hardware inputs to logical layers during setup.
- Layer Definition Manager links hardware channels to layer IDs, including `startSection` offsets for multi-module rigs and UDP block sizing when several controllers share a bus.
- Support per-layer smoothing parameters, cadence throttling, and EMA/deadband tuning that can be flashed or adjusted through the same configuration UI.
- Allow firmware to publish arbitrary analog/digital signals via the flexible layer registry, keeping shipped dashboards declarative rather than hard-coded to specific sensors.
- Provide guidance for scaling raw sensor data into normalized ranges before sending PGNs and for flagging missing data using reserved sentinels (`0xFF` for u8, `0xFFFF` for u16) that controllers convert into zero-weight samples.
- Reserve configuration fields for `tileResolutionMultiplier`, `rateNAFlag`, and derived layer bindings so firmware can negotiate how frequently to emit data for high-frequency sensors.

## Pros
- Gives firmware teams clear contracts for adding new sensors without requiring application rebuilds.
- Centralizes hardware mapping inside configuration flows, reducing custom forks for unique rigs.
- Enables shared tooling for flashing, tuning, and verifying modules through AgIO.

## Cons
- Requires firmware updates and additional testing hardware to validate new mappings.
- Configuration dialogs become more complex, demanding strong UX to avoid misconfiguration.
- Increased metadata exchange may expose compatibility issues with legacy controllers.

## Risks & mitigations
- **Risk:** Operators mis-map hardware channels. **Mitigation:** Provide presets (e.g., 48-row planter) and validation that flags missing required layers like skips/doubles/downforce.
- **Risk:** Firmware without layer support becomes unusable. **Mitigation:** Keep legacy binary section control flows available and allow a “Legacy-only” toggle.
- **Risk:** UDP block congestion in multi-module rigs. **Mitigation:** Allow configuration of block sizes, cadence, and prioritization per module.

## Borrowables
- Existing AgIO module discovery and firmware flashing pathways can be extended with new metadata fields.
- Current section configuration dialogs supply geometry data for `startSection` offsets and coverage factors.
- AgDiag simulators can be adapted to emulate the new layer-capable modules for testing.

## Rough effort
M — Firmware, configuration UI, and discovery protocol changes but largely additive to existing module infrastructure.

