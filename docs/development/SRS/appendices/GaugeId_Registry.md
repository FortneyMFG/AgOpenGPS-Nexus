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

- [ADR-016 — Firmware Transport Variable Rate PGNs](../sections/4X_Interprocess_Communications/42-ADR-016 - Firmware and transport for variable-rate layer PGNs.md)
- [ADR-017 — Profiles & Kinematics](../sections/6X_Core_Domain_Services/61-ADR-017 - Equipment profiles and kinematics.md)
- [ADR-047 — Live Telemetry Mesh](../sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md)
