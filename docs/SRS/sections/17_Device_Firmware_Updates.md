# Device Firmware Updates (Status: proposal)

## Problem statement
Operators run heterogeneous rate controllers, section modules, AIO boards, and steering controllers across AVR, ESP32, Teensy, STM32, and other MCUs. Each vendor ships ad-hoc firmware versioning and flashing flows, creating uncertainty about firmware provenance and leaving crews without timely fixes. We need a unified detection, notification, and update framework that spans AgIO transports (serial, UDP, CAN/SocketCAN) and works with the plugin system so new device families can ship their own updaters without forking core code.

## Requirements
- DFU-001 (MUST): Discover device identity (make, model, variant, MCU, current firmware version, capabilities) at runtime over every supported transport.
- DFU-002 (MUST): Present update notifications in the UI with changelog, compatibility, and safety pre-checks before any flashing begins.
- DFU-003 (MUST): Keep updates optional and explicitly user-confirmed; never auto-flash without consent.
- DFU-004 (MUST): Support OTA (UDP/Wi-Fi), serial/USB, CAN (via bootloaders), and DFU-style methods with a consistent orchestration surface.
- DFU-005 (MUST): Enforce safety controls—signature/checksum verification, power and link guardrails, bounded timeouts, and transactional flows.
- DFU-006 (SHOULD): Offer staged rollout channels (stable, beta) and allow pinning individual devices to a chosen channel/version.
- DFU-007 (SHOULD): Allow plugin backends to register device-specific updaters without modifying core logic.
- DFU-008 (COULD): Support offline update bundles (e.g., USB stick) that carry the same verification artifacts as the online catalog.

## Architecture overview
1. **Device identity & health broadcast**: Every compatible module emits a periodic (1–2 Hz) identity frame over its transport. On CAN this is PGN `0xD4`; on serial/UDP the same fields are sent in either the 8-byte binary frame or an extended JSON “Device Hello” once per boot. Fields: `vendorId`, `productId`, `variantId`, `mcuId`, firmware semantic version, capability bitfield (OTA, CAN bootloader, DFU, etc.), and basic health flags (temperature/voltage/uptime bands). Extended JSON can surface `fwVersion`, `bootloader`, `api`, `serial` strings.
2. **DFU catalog service**: Core fetches a signed JSON catalog (see [Appendix — DFU Catalog Schema](../appendices/DFU_Catalog_Schema.md)) that lists known vendor/product/variant combinations, the latest firmware per rollout channel, download assets (BIN/ELF/HEX), signatures, compatible hardware ranges, bootloader prerequisites, and instructions. Users can configure default URLs and fallback mirrors. Catalog signatures use Ed25519; firmware assets include SHA-256 hashes and optional signatures.
3. **Update orchestrator**: A core service (AgIO/AgOpenGPS) aggregates device identities, matches them to catalog entries, computes compatibility checks, and surfaces “Update available” badges. It builds an `UpdatePlan` that includes device identity, chosen channel, firmware asset, pre-check requirements, and a selected update method.
4. **Updater plugins**: Out-of-process plugins implement flashing workflows per MCU/bootloader (avrdude, esptool.py, teensy_loader_cli, dfu-util, CAN bootloaders, vendor OTA routines). Plugins expose a gRPC surface (`DfuManager`) that core invokes to download, flash, verify, reboot, and rollback. Plugins stream progress states (`erasing`, `flashing`, `verifying`, `rebooting`) and telemetry logs back to UI.
5. **UI & UX**: The device list shows each discovered module with firmware version, health status, and update badges. A detail drawer presents changelog notes, channel selection, compatibility gates, estimated flash time, and step-by-step guidance (especially for wired flows that require button presses). Notifications are non-blocking toasts plus an Updates tab with snooze/remind options.

## Identity frame details
- **Binary PGN 0xD4 layout**: byte0 `vendorId`, byte1 `productId`, byte2 `variantId`, byte3 `mcuId` (1=AVR, 2=ESP32, 3=Teensy/ARM, 4=STM32, etc.), byte4 upper nibble `fwMajor`, lower nibble `fwMinor`, byte5 `fwPatch`, byte6 capability bitfield (bit0=OTA, bit1=CAN boot, bit2=USB DFU, bit3=dual-bank, bit4=voltage telemetry, remaining reserved), byte7 health flags (voltage low, thermal warning, uptime bucket). Devices without CAN reuse the same packed frame over serial or UDP for passive discovery.
- **Extended Device Hello (optional)**: JSON payload (emitted at boot or on request) including human-readable `fwVersion`, `bootloader`, `hwVersion`, `api`, `serial`, `capabilities` array, and current power metrics for richer logging and diagnostics.

## DFU catalog expectations
- Catalog JSON is versioned (`version: 1`), timestamped, and signed. It lists device definitions with rollout channels (`stable`, `beta`, etc.), each providing firmware assets, signatures, instructions, minimum core API requirements, and release notes.
- Compatibility rules include hardware version ranges (`hwMin`, `hwMax`), minimum bootloader, and optional dependency checks (e.g., requires identity capability bit for OTA).
- Catalogs can be hosted by the community or by vendors; users can add custom catalog URLs or import offline bundles. Catalog signature validation failures must block updates with a clear UI message.

## Supported update methods
| Method | Typical MCU | Transport | Tooling | Notes |
|---|---|---|---|---|
| Serial AVR | ATmega328p/ATmega2560 | USB CDC | `avrdude` (stk500/Caterina) | Requires reset/boot button guidance. |
| Teensy HID | MK20/i.MX RT | USB HID | `teensy_loader_cli` | Prefers external power, supports verify after flash. |
| ESP32 OTA | ESP32 | Wi-Fi/UDP/TCP | ESP-IDF OTA | Auto-select when device reports OTA capability and external power. |
| ESP32 Serial | ESP32 | USB UART | `esptool.py` | Used when OTA unavailable or fails verification. |
| STM32 DFU | STM32 | USB DFU | `dfu-util` | Handles DFU detach/attach cycles, verifies CRC. |
| CAN Bootloader | STM32/other CAN MCUs | CAN / ISO-TP / UDS | Vendor bootloader tools or SocketCAN helpers | Requires deterministic CAN session management. |

The orchestrator always prefers the safest available method (OTA when supported and stable power) and falls back to wired flows with detailed guidance.

## Safety & transactionality
- **Pre-checks**: Confirm power conditions (voltage telemetry, external power connected), validate stable transport link, and ensure device identity matches the selected firmware (vendor/product/variant/serial).
- **Guardrails**: Prompt operators to maintain power, block updates on low-voltage alarms, and warn when only battery power is detected.
- **Verification**: Mandatory checksum/signature verification after flash. Catalog-supplied signatures are validated before flashing; on-device verification (dual-bank or CRC) runs before reporting success.
- **Rollback**: Prefer dual-bank/OTA rollback when supported (e.g., ESP32 A/B partitions). Otherwise retain last-known-good firmware package and provide recovery instructions (wired bootloader flow). UI exposes a rollback action when the device advertises support.
- **Timeouts & retries**: Bounded retries per flashing phase with descriptive error codes (timeout, signature mismatch, transport disconnect). Logs capture each step for diagnostics.

## Offline & field mode
Operators can import an offline update bundle (ZIP) that contains a trimmed catalog, firmware assets, checksums, signatures, and flashing instructions. Once imported, the orchestrator treats it as a read-only catalog source and performs the same verification flow without internet access.

## API sketch (core ↔ updater plugin)
```
service DfuManager {
  rpc ListDevices (Empty) returns (DeviceList);
  rpc CheckUpdates (DeviceSelector) returns (UpdatePlan);
  rpc Download (UpdatePlan) returns (DownloadResult);
  rpc Flash (UpdatePlan) returns (stream FlashProgress);
  rpc Verify (DeviceSelector) returns (VerifyResult);
  rpc Reboot (DeviceSelector) returns (Ack);
}

message DeviceIdentity {
  string vendor = 1;
  string product = 2;
  string variant = 3;
  string mcu = 4;
  string fwVersion = 5;
  string hwVersion = 6;
  string serial = 7;
  uint32 capabilities = 8;
}
```
Core services discover devices via transports, the catalog resolver computes update plans, and updater plugins execute flashing flows while streaming progress logs back to the UI and history logs.

## Related specifications
- Transport discovery and PGN framing: see [Section 03 — Communications & Transports](03_Comm_Transports.md).
- Plugin surface and updater extensibility: see [Section 12 — Extensibility & Plugins](12_Extensibility_Plugins.md).
- Packaging for catalogs and offline bundles: see Section 14 `Offline-First Updates` and Section 16 `Plugin Packaging, Updates, and Catalog` for shared distribution policies.
- JSON schema definitions for catalogs and bundles: see [Appendix — DFU Catalog Schema](../appendices/DFU_Catalog_Schema.md).

## Related ADRs

- [ADR-015 — Section Control Grouping Semantics](../../ADR/ADR-015-section-control-grouping-semantics.md)
- [ADR-016 — Firmware Transport Variable Rate PGNs](../../ADR/ADR-016-firmware-transport-variable-rate-pgns.md)
- [ADR-018 — Plugin API](../../ADR/ADR-018-plugin-api.md)
- [ADR-048 — RadioBridge](../../ADR/ADR-048_RadioBridge.md)
