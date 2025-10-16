# Pumpkin Pi (CM5 HAL + SHM fastpath)

Pumpkin Pi lets a CM5 act as its own controller:
- SHM fastpath between NAV and steer-ctrl for sub-ms jitter (ring + eventfd)
- HAL backends for PWM char devices, libgpiod GPIO, and SocketCAN steering actuators
- gRPC surface for NAV to publish `SetSteerTarget` and subscribe to streaming `SteerStatus`
- MQTT loopback mirrors steer targets, status, health, and authority ownership
- AgIO Bridge stays on for external devices (UDP/Serial/CAN/MQTT-SN)

## Configure
Copy `config/pumpkin.yaml.example` to `/etc/aog/pumpkin.yaml` and edit HAL backend.
Optional: copy `config/bridge.yaml.example` to `/etc/aog/bridge.yaml` to keep external adapters online.
Enable service:
```bash
sudo cp systemd/pumpkin-pi.service /etc/systemd/system/
sudo systemctl enable --now pumpkin-pi
```

## Expected flows

* NAV (gRPC `SetSteerTarget`) → SHM → steer-ctrl → HAL → hardware
* SteerTarget mirrored to `aog/v1/bus/nav/steer_target` (QoS1 retained)
* SteerStatus/Health/Authority → MQTT loopback for UI and AgIO Bridge
