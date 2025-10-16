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
2. Keep the AgIO Bridge enabled for external adapters:
   ```bash
   sudo cp plugins/pumpkin-pi/config/bridge.yaml.example /etc/aog/bridge.yaml
   sudo systemctl enable --now agio-bridge
   ```
3. Deploy the systemd unit:
   ```bash
   sudo cp plugins/pumpkin-pi/systemd/pumpkin-pi.service /etc/systemd/system/
   sudo systemctl daemon-reload
   sudo systemctl enable --now pumpkin-pi
   ```
4. Grant real-time scheduling and memory lock:
   ```bash
   sudo setcap cap_sys_nice=+ep /usr/local/bin/pumpkin-pi
    sudo systemctl set-property pumpkin-pi.service MemoryMax=infinity
   ```

## Verify Fastpath
- Confirm `/dev/shm/aoglink_steer` exists and updates when NAV runs.
- Check `journalctl -u pumpkin-pi` for HAL initialization logs.
- Subscribe to `aog/v1/bus/nav/steer_target` on localhost MQTT and confirm retained updates.
- Call `grpcurl -unix /run/pumpkin-pi.sock pumpkinpi.v1.PumpkinPiService/StreamSteerStatus` to watch status streaming.

## Keep AgIO Bridge Enabled
- Ensure `AgOpenGPS.AgIOBridge` service remains active for UDP/serial/CAN adapters.
- Mirror telemetry from Pumpkin Pi to Bridge topics via MQTT to keep UI dashboards in sync.

## Authority Handoff
- Default authority token is retained by CM5. External controllers publish retained payloads to `aog/v1/ctrl/authority/steer` to request control. Pumpkin Pi must ACK or yield within 150 ms.
