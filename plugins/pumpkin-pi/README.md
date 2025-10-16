# Pumpkin Pi (CM5 HAL + SHM fastpath)

Pumpkin Pi lets a CM5 act as its own controller:
- SHM fastpath between NAV and steer-ctrl for sub-ms jitter
- HAL to touch real hardware (GPIO/PWM/CAN/I²C/SPI)
- Mirrors telemetry to MQTT loopback for UI/logging
- AgIO Bridge stays on for external devices (UDP/Serial/CAN/MQTT-SN)

## Configure
Copy `config/pumpkin.yaml.example` to `/etc/aog/pumpkin.yaml` and edit HAL backend.
Enable service:
```bash
sudo cp systemd/pumpkin-pi.service /etc/systemd/system/
sudo systemctl enable --now pumpkin-pi
```

## Expected flows

* NAV → SHM → steer-ctrl → HAL → hardware
* SteerTarget also published to `aog/v1/bus/nav/steer_target` (QoS1 retained)
* SteerStatus/Health → MQTT loopback
