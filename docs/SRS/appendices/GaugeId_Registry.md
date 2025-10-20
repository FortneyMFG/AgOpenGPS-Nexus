# Gauge ID Registry

See [Section 15 – Engine & Machine Gauges](../sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md) for requirements, transport framing, and UI behaviors that rely on this registry.
| gaugeId | Name | J1939 PGN / SPN | Units | Convert (raw → engineering) |
|---|---|---|---|---|
| 1 | EngineSpeed | 61444 / 190 | rpm | `raw * 0.125` |
| 2 | CoolantTemp | 65262 / 110 | °C | `raw - 40` |
| 3 | EngineOilPressure | 65263 / 100 | kPa | `raw * 4` |
| 4 | BatteryPotential | 65271 / 168 | V | `raw * 0.05` |
| 5 | FuelLevel1 | 65276 / 96 | % | `raw * 0.4` |

Gauge IDs from 240–255 are reserved for vendor-specific or experimental mappings. Document any additions alongside their PGN/SPN or ISOBUS DDI references and scaling so dashboards remain interoperable across rigs.

## Related ADRs

- [ADR-016 — Firmware Transport Variable Rate PGNs](../../ADR/ADR-016-firmware-transport-variable-rate-pgns.md)
- [ADR-017 — Profiles & Kinematics](../../ADR/ADR-017-profiles-kinematics.md)
- [ADR-047 — Live Telemetry Mesh](../../ADR/ADR-047_LiveTelemetryMesh.md)
