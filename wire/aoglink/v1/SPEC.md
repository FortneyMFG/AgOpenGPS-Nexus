# AOG-Link v1 Specification (Excerpt)

## 6.4 Local MQTT Mirroring
AgIO Bridge instances that operate on the same CM5 as NAV and steer-ctrl nodes must continue mirroring `SteerTarget` and `SteerStatus` topics on the local MQTT broker so UI shells, loggers, and optional external controllers can subscribe without binding to the SHM fastpath.

## 8. Messaging patterns
AOG-Link transports cover UDP, RS-485/serial, and CAN/CAN-FD networks for MCU devices. Host-local integrations normally traverse the bridge to preserve consistent arbitration, telemetry, and security policy.

## 8A. CM5 Integrated Controller (AgIO-bypass)
When CM5 acts as both host and controller, NAV⇄CTRL setpoints use a local Shared-Memory (SHM) fastpath and do not traverse AgIO or the network stack. AgIO remains enabled for external transports (UDP/Serial/CAN/MQTT-SN) and observability.

- **Fastpath:** `/dev/shm/aoglink_steer` ring + eventfd. Producer: NAV. Consumer: steer-ctrl.
- **HAL:** CM5 hardware I/O via libgpiod (GPIO), PWM char devices, SocketCAN, I²C/SPI.
- **Mirroring:** `SteerTarget` mirrored to MQTT loopback (QoS1 retained) for UI/logger and optional external controllers.
- **Authority:** `aog/v1/ctrl/authority/steer` (QoS1 retained) nominates controller; default holder is CM5 when single-box.
- **Setpoint TTL:** Controllers must ignore retained setpoints older than default 250 ms (configurable via `HelloAck.setpoint_ttl_ms`).

Performance targets: SHM write→apply ≤2 ms p50 / ≤5 ms p95; authority handoff ≤150 ms on-device.
