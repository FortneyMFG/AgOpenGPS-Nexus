# Pumpkin Pi Plugin

Pumpkin Pi lets a CM5 run NAV and steer-ctrl loops without leaving the box. The plugin exposes a shared-memory (SHM) ring for setpoints, owns the local hardware abstraction layer (HAL), and still mirrors telemetry through the AgIO Bridge so external devices remain compatible.

## Capabilities
- SHM fastpath (`/dev/shm/aoglink_steer`) between NAV producer and steer-ctrl consumer with eventfd notification.
- gRPC service `pumpkinpi.v1.PumpkinPiService` exposing `SetSteerTarget`, `StreamSteerStatus`, and `ClaimAuthority`.
- HAL drivers for GPIO (libgpiod), PWM character devices, and SocketCAN actuators.
- MQTT loopback mirrors for `SteerTarget`, `SteerStatus`, health heartbeats, and authority tokens (QoS1 retained).
- Authority topic `aog/v1/ctrl/authority/steer` for takeover or external controller nomination.

## Configuration
```yaml
node_id: cm5
roles: [CTRL, SENSOR, ACTUATOR]
fastpath:
  shm_name: "/aoglink_steer"
  eventfd_name: "/pumpkin-pi-steer"
  setpoint_ttl_ms: 250
hal:
  backend: pwm          # pwm | gpio | can
  pwm:
    chip: 0
    channel: 0
    period_ns: 2000000
grpc:
  listen: "unix:///run/pumpkin-pi.sock"
mqtt:
  broker: "localhost:1883"
  client_id: pumpkin-pi
  publish_status: true
authority:
  topic: "aog/v1/ctrl/authority/steer"
  default_owner: cm5
  hold_interval: 150ms
```

- `fastpath.setpoint_ttl_ms` defines the maximum age NAV/steer-ctrl should honor before forcing neutral outputs.
- `hal.backend` selects which hardware driver to load; each backend injects additional configuration blocks.
- `mqtt.publish_status` controls whether Pumpkin Pi mirrors telemetry to loopback (recommended `true`).
- `authority.hold_interval` defines how long a takeover lasts before Pumpkin Pi automatically reclaims control.

## Interaction with AgIO Bridge
Pumpkin Pi bypasses AgIO for CM5-internal traffic but keeps the bridge online for UDP, serial, CAN-FD, and MQTT-SN transports. The bridge still reports authority state, mirrored steer targets, and health so UI shells and external modules can monitor activity.

```mermaid
flowchart TD
  subgraph CM5["CM5 (Host + Controller)"]
    Core[gRPC Bus / NAV / UI]
    subgraph Pumpkin["Pumpkin Pi (HAL + SHM)"]
      SHM[(SHM Fastpath)]
      STEER[steer-ctrl]
    end
    subgraph AgIO["AgIO Bridge (external only)"]
      LINK[AOG-Link v1]
      UDP[UDP]
      SERIAL[Serial]
      CAN[CAN-FD]
      MQTT[MQTT Adapter]
      V0[v0 Bridge]
      LINK --> UDP & SERIAL & CAN & MQTT & V0
    end
    Core -->|SetSteerTarget (gRPC)| Pumpkin
    Pumpkin --> SHM
    STEER -->|HAL| Hardware
    Pumpkin <--> MQTT
  end
```

## Safety & Authority
- Authority defaults to CM5 on boot; external controllers publish retained tokens to take control.
- Setpoints older than `fastpath.setpoint_ttl_ms` must be ignored and reset to neutral outputs.
- Pumpkin Pi should be scheduled with real-time priority (`CPUSchedulingPolicy=fifo`, priority ≥80) and lock memory (`mlockall`) to avoid jitter.

## Rollout Checklist
- Extract `org.agopengps.agio.pumpkinpi-<ver>-linux-arm64.zip` to
  `<install-roo../Plugins/org.agopengps.agio.pumpkinpi/<ver>/`.
- Install config at `/etc/aog/pumpkin.yaml`.
- Enable systemd service `pumpkin-pi`.
- Verify SHM ring version matches NAV/steer-ctrl build.
- Confirm MQTT loopback mirrors `SteerTarget` + `SteerStatus` retained messages.
