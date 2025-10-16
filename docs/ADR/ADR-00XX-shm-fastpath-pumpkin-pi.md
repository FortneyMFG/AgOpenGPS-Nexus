# ADR-00XX: CM5 SHM Fastpath + HAL Plugin (“Pumpkin Pi”)

## Status
Proposed (target release: AOG-Link v1.0)

## Context
CM5 frequently runs both NAV and steer control. Routing setpoints via broker or sockets adds 1–5 ms jitter. We need a deterministic, low-overhead path while preserving AgIO for external MCUs and v0 coexistence.

## Decision
Implement a CM5-local plugin (“Pumpkin Pi”) that:
- provides a Shared-Memory fastpath between NAV and steer-ctrl,
- owns the HAL (GPIO/PWM/CAN/I²C/SPI) for final actuator I/O,
- mirrors setpoints/telemetry to MQTT loopback for fan-out,
- leaves AgIO Bridge enabled exclusively for external transports.

## Consequences
- ✅ Lower latency & jitter for steering (< 2–5 ms p95 end-to-end).
- ✅ External devices remain plug-and-play via AgIO.
- ⚠️ One more service (Pumpkin Pi) to supervise; SHM ABI must be versioned.
- ❌ SHM-only builds (with AgIO disabled) cannot accept external devices.

## Alternatives considered
- All-MQTT/MQTT-SN: simpler but higher jitter.
- AgIO-only: uniform path, but adds avoidable overhead for on-box control.

## Work items
- Define SHM ABI & versioning.
- Implement libhal backends.
- Wire NAV producer and steer-ctrl consumer.
- Update SRS and plugin docs; add tests and systemd units.
