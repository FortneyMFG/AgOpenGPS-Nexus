# Gauge ID Registry

See [Section 15 – Engine & Machine Gauges](../sections/15_Engine_Machine_Gauges.md) for requirements, transport framing, and UI behaviors that rely on this registry.
| gaugeId | Name | J1939 PGN / SPN | Units | Convert (raw → engineering) |
|---|---|---|---|---|
| 1 | EngineSpeed | 61444 / 190 | rpm | `raw * 0.125` |
| 2 | CoolantTemp | 65262 / 110 | °C | `raw - 40` |
| 3 | EngineOilPressure | 65263 / 100 | kPa | `raw * 4` |
| 4 | BatteryPotential | 65271 / 168 | V | `raw * 0.05` |
| 5 | FuelLevel1 | 65276 / 96 | % | `raw * 0.4` |

Gauge IDs from 240–255 are reserved for vendor-specific or experimental mappings. Document any additions alongside their PGN/SPN or ISOBUS DDI references and scaling so dashboards remain interoperable across rigs.
