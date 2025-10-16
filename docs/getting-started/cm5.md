# CM5 Integrated Controller Setup

This guide prepares a CM5 that runs NAV, steer-ctrl, Pumpkin Pi, and the AgIO Bridge on the same device.

## Prerequisites
- Nexus runtime images for CM5 with NAV and steer-ctrl services installed.
- Mosquitto or compatible MQTT broker running locally.
- Real-time kernel options enabled (ensure `cpuset`/`rtprio` limits allow FIFO priority 80+).

## Install Pumpkin Pi
1. Copy the example config and adjust HAL settings:
   ```bash
   sudo install -d /etc/aog
   sudo cp plugins/pumpkin-pi/config/pumpkin.yaml.example /etc/aog/pumpkin.yaml
   sudo editor /etc/aog/pumpkin.yaml
   ```
2. Deploy the systemd unit:
   ```bash
   sudo cp plugins/pumpkin-pi/systemd/pumpkin-pi.service /etc/systemd/system/
   sudo systemctl daemon-reload
   sudo systemctl enable --now pumpkin-pi
   ```
3. Grant real-time scheduling:
   ```bash
   sudo setcap cap_sys_nice=+ep /usr/local/bin/pumpkin-pi
   ```

## Verify Fastpath
- Confirm `/dev/shm/aoglink_steer` exists and updates when NAV runs.
- Check `journalctl -u pumpkin-pi` for HAL initialization logs.
- Subscribe to `aog/v1/bus/nav/steer_target` on localhost MQTT and confirm retained updates.

## Keep AgIO Bridge Enabled
- Ensure `AgOpenGPS.AgIOBridge` service remains active for UDP/serial/CAN adapters.
- Mirror telemetry from Pumpkin Pi to Bridge topics via MQTT to keep UI dashboards in sync.

## Authority Handoff
- Default authority token is retained by CM5. External controllers publish retained payloads to `aog/v1/ctrl/authority/steer` to request control. Pumpkin Pi must ACK or yield within 150 ms.
