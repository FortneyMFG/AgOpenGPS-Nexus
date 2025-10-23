# AgOpenGPS Hardware Platform Reference

This reference captures representative hardware stacks that operators deploy today when running AgOpenGPS. It is scoped to end-user compute platforms and assumes guidance controllers, implement control ECUs, and GNSS receivers follow the PGN baselines captured elsewhere in the SRS. Each section summarizes total system costs (device, power, mounting, and required accessories), standardization considerations, ease of setup, performance headroom, ruggedness, and mounting implications. Values are indicative ranges in USD as of 2024 and may vary regionally.

## Windows tablets and PCs (primary field deployment)

| Platform | Typical configuration | Cost (new) | Cost (used/refurb) | Standardization & lifecycle | Setup & usability | Performance headroom | Ruggedness & environmental tolerance | Mounting & I/O notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Panasonic Toughpad FZ-G1 / FZ-G2 | 10" daylight-readable rugged tablet, Intel i5, 8–16 GB RAM, 256 GB SSD, vehicle dock with CLA power | $2,400–$3,200 (tablet + dock + power) | $600–$1,200 (ex-lease bundles) | 5–7 year availability per generation, hot-swappable batteries, Windows LTSC images common | Preload AgOpenGPS installer or portable folder; drivers stable; GPS/IMU via USB or RS-232 dock | Handles full AgOpenGPS stack, RTK corrections, and remote support concurrently | MIL-STD-810G/IP65, outdoor readable 800+ nit display, glove use | Dedicated docks clamp to RAM Mount ball bases; numerous serial/USB breakouts on dock |
| Trimble / CNH rugged Windows tablets (Trimble T10/T7, CNH XCN-1050) | OEM 10" tablets sold with ag kits; Intel Atom/i5; GNSS-ready power harness | $3,500–$5,500 (bundle with harness) | $1,500–$3,000 (surplus dealer) | Locked-down images; long-term firmware support but limited user customization | Install AgOpenGPS alongside vendor tools; may require admin unlock | Adequate for mapping, light analytics; limited for heavy post-processing | Designed for cab vibration, sealed, integrated CAN/power connectors | Proprietary mounting plates; integration with existing 12 V harness reduces wiring |
| Microsoft Surface Go 3/4 with rugged frame | 10.5" consumer tablet, Intel Pentium/ i3, rugged frame (MobileDemand) + RAM mount + USB-C hub + hardwire charger | $850–$1,200 | $450–$700 | High availability; consumer lifecycle ~3 years; relies on Windows Update | Clean Windows install then AgOpenGPS; requires disabling sleep/updates | Enough for guidance + light section control; limited in extreme heat | Frame adds drop protection; IP54 with case; sunlight readability moderate | Requires USB-C hub for GNSS/IMU; RAM Tab-Tite cradle fits standard 1.5" ball |
| Intel NUC / Minisforum UM series (12 V PC) + external display | x86 mini PC (i5/i7), 16 GB RAM, 512 GB SSD, 12 V DC-DC regulator, 12–15" 12 V monitor, USB touch overlay | $1,100–$1,600 (PC + display + regulator + mount) | $700–$1,000 | Commodity supply, easy imaging with MDT/WDS; replaceable components | Requires enclosure, fan filters; Windows Pro image, install AgOpenGPS, remote desktop | High headroom for multi-monitor, data logging, VMs; can run analytics offline | Not sealed; needs fan filters and shock damping; better in climate-controlled cabs | Monitor mounts via VESA-to-RAM; PC secures in junction box; ample USB/serial expansion |

### Windows considerations

* **Why it leads today** – AgOpenGPS is primarily developed/tested on Windows, most third-party GNSS drivers and USB tools ship Windows binaries, and farms often have Windows support expertise.
* **Standard images** – Many operators freeze on Windows 10/11 Pro with offline user accounts and minimal services to ensure deterministic startup.
* **Peripheral ecosystems** – Wide availability of CAN-to-USB, RS-232, and Ethernet adapters with signed drivers simplifies multi-module deployments.

## Linux SBCs and industrial PCs

| Platform | Typical configuration | Cost (new) | Cost (used/refurb) | Standardization & lifecycle | Setup & usability | Performance headroom | Ruggedness & environmental tolerance | Mounting & I/O notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Raspberry Pi 5 + AgOpenGPS HAT (concept) | Pi 5 (8 GB), passive case with DIN rail mount, 10.1" HDMI touchscreen, PoE HAT or GPIO machine-control HAT, 12 V to 5 V regulator | $350–$450 (Pi + screen + HAT + power) | $250–$320 (Pi used is rare) | Community-controlled stack; long-term availability via CM4 variants; single SD/EMMC image | Flash Linux distro image (Ubuntu Core/Raspberry Pi OS), install AgOpenGPS build, configure systemd services | Enough for Qt UI + machine control; CPU margin for CAN and RTK streaming | Requires sealed enclosure; wide temp requires heat spreaders; fanless but limited ingress protection | Touchscreen mounts on RAM plate; Pi/HAT stack fits DIN-rail near cab harness; GPIO enables direct section/machine HATs |
| Raspberry Pi CM4 + Waveshare Industrial carrier | CM4 (4–8 GB), carrier with isolated CAN/RS-485, eMMC, 7"-10" touchscreen, IP65 enclosure | $500–$650 | $350–$450 | Better BOM control; carriers sold 5+ years; Yocto/Linux images version-controlled | Prebuilt images reduce setup; AgOpenGPS can run via Qt/Mono builds; remote SSH | Moderate; hardware acceleration limited but adequate for guidance UI | Carrier-rated -20 ° to 70 °C; sealed enclosure reduces dust ingress | Panel-mount display screws into dash; I/O terminals exposed inside enclosure |
| LattePanda 3 Delta (SBC + embedded microcontroller) | Intel N5105 SBC with built-in Arduino co-processor, 8 GB RAM, 128 GB SSD, 10" HDMI display, 12 V regulator | $600–$800 | $400–$550 | Combines x86 Windows/Linux with Arduino; board supply 3–5 years | Windows 11 or Ubuntu preloads; Arduino handles real-time IO; requires cooling fan | Comparable to low-power laptops; handles AgOpenGPS + light data logging | Open-frame board; needs vibration damping and dust-proof enclosure | Board mounts to plate; display via RAM mount; Arduino headers expose GPIO for section control |
| OnLogic/Advantech fanless industrial PCs | Fanless Intel i5/i7 IPC, 8–16 GB RAM, 256 GB SSD, wide 9–36 V input, CAN/serial IO, 12" sunlight-readable panel PC | $1,800–$2,600 | $1,000–$1,600 | 7–10 year lifecycle, configurable SKUs; OEM support for Ubuntu/Debian | Pre-imaged Linux; AgOpenGPS via Mono/Qt; remote fleet management via Ansible | High headroom; supports multi-display, VM, RTK processing | Fanless, sealed IP65/IP66 front, wide temp -20 ° to 60 °C | Panel PCs mount flush to dash; VESA mounting for box PCs; integrated serial/CAN reduces adapters |

### Linux considerations

* **AIO HAT potential** – Pi GPIO exposes SPI/I²C/UART needed for steer/machine hats, enabling single-board “all-in-one” controllers without USB hubs.
* **Real-time control** – Pairing Linux SBCs with co-processors (Arduino, RP2040) or PREEMPT_RT kernels enables deterministic machine control loops.
* **Field maintainability** – SD/EMMC images can be replicated; spare boards are inexpensive to keep on hand.

## Android rugged tablets and handhelds

| Platform | Typical configuration | Cost (new) | Cost (used/refurb) | Standardization & lifecycle | Setup & usability | Performance headroom | Ruggedness & environmental tolerance | Mounting & I/O notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Samsung Galaxy Tab Active4 Pro | 10.1" rugged tablet, Snapdragon 778G, 6 GB RAM, 128 GB, pogo-pin vehicle dock with 12 V adapter | $900–$1,200 | $500–$750 | 3–4 year availability; Knox Manage aids fleet standardization | Install AgOpenGPS Android build/remote client; limited access to native USB drivers | Sufficient for remote UI, mapping, telemetry dashboards; limited for full desktop features | MIL-STD-810H/IP68, replaceable battery, glove/touch modes | Docks bolt to RAM bases; pogo pins provide power + USB passthrough |
| Juniper Mesa 3 Rugged Android | 7" sunlight-readable tablet, Snapdragon 626, 6 GB RAM, GNSS options, vehicle dock | $1,600–$2,100 | $900–$1,200 | Niche vendor with long-term service contracts; Android updates slower | Sideload AgOpenGPS client; integrate with GNSS over Bluetooth/serial | Adequate for remote monitor role; CPU modest for heavy UI | IP68, drop-rated, extended temp range | Smaller screen eases mounting; limited USB (Micro-B); rely on Bluetooth/RS-232 |
| Getac ZX10 | 10" rugged Android tablet, Snapdragon 660, 8 GB RAM, optional keyboard dock, 12 V charger | $1,400–$1,900 | $800–$1,100 | Enterprise supply chain; supports Android Enterprise configurations | Requires sideloading; integrate with AgIO via Wi-Fi/UDP; vendor peripherals available | Enough for remote UIs, CAN dashboards; not ideal for native Windows binaries | Fully rugged (IP66, MIL-STD-810H), glove touch, sunlight readable | Vehicle docks with HDMI-out and dual USB allow external sensors |

### Android considerations

* **Role today** – Android devices are typically remote clients mirroring AgOpenGPS via VNC/WebRTC or running lightweight dashboard apps; full native port would require additional platform investment.
* **Cost vs. capability** – Hardware is cheaper than rugged Windows tablets but lacks drivers for USB-based steer controllers, so they complement rather than replace PC-class hosts.
* **Mounting** – Abundant RAM-compatible cradles; lighter weight reduces bracket stress on smaller cabs.

## Cross-platform observations

* **Power integration** – Regardless of OS, bundle DC-DC converters with surge suppression and ignition sensing to support graceful shutdowns.
* **Input devices** – Cab-friendly accessories (USB knobs, foot switches) should be evaluated per OS driver support.
* **Environmental sealing** – Tablets provide higher ingress protection out of the box; SBC + display builds need custom enclosures and conformal-coated PCBs for long-term reliability.
* **Spare strategy** – Maintain at least one imaged spare tablet/SBC per fleet to minimize planting season downtime.
* **Upgradeable components** – Windows mini PCs and Linux IPCs allow SSD and RAM upgrades; tablets trade repairability for compactness.

## Related SRS topics

* See the [AgIO ↔ Hardware PGN Baseline](./AgIO_PGN_Baseline.md) for messaging requirements that any platform must support via USB, serial, or CAN adapters.
* Sections under `sections/HW` capture controller and sensor requirements that pair with these compute platforms.

## Related ADRs

- [ADR-001 — Adopt .NET 8 C# Stack](../sections/1X_Platform_Foundations/11-ADR-001 - Adopt .NET 8 C stack for Nexus runtime.md)
- [ADR-003 — Avalonia UI](../sections/1X_Platform_Foundations/13-ADR-003 - Use Avalonia for the cross-platform Nexus UI shell.md)
- [ADR-006 — AgIO Link MCU Communications](../sections/4X_Interprocess_Communications/42-ADR-006 - MCU communications over AOG-Link (nanopb).md)
- [ADR-048 — RadioBridge](../sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md)
