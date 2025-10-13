# AgIO ↔ Hardware PGN Baseline

This reference captures the current AgOpenGPS/AgIO parameter group number (PGN) framing as implemented today. The SRS must preserve
backward compatibility with these payloads (or provide shims) while introducing new transports or schemas.

## Message framing

An AOG message of length `n` has the following general format:

| Byte 0 | Byte 1 | Byte 2 | Byte 3 | Byte 4 | ... | Byte n-1 |
| ------ | ------ | ------ | ------ | ------ | --- | -------- |
| `0x80` | `0x81` | `Src`  | `PGN`  | `Len`  | Data | `CRC` |

* **Src** – Sender identifier.
* **PGN** – Parameter Group Number.
* **Len** – Length of the Data payload in bytes.
* **Data** – Payload associated with the PGN.
* **CRC** – Checksum of bytes 2 through `n-2`.

Serial transports wrap the frame above using COBS encoding with a trailing `0x00` delimiter
and reuse the same one-byte checksum. Hardware commonly operates at 115200 bps, but Nexus
adds a 921600 bps option to match modern legacy-compatible modules. `LegacySerialFrameCodec`
provides a reference implementation of the encoder/decoder pair.

## PGN catalog

The tables below mirror today’s UDP/serial catalog grouped by module. Fields marked `***` or `*` represent reserved or currently
undocumented bytes. All counts are little endian unless noted.

### Steer module

* IP: `192.168.5.126`
* Hello: `126`
* Port: `5126`
* Hello payload: `00 00 56 00 00 7E`

| PGN Name        | Src | Src (dec) | PGN | PGN (dec) | Len | Byte 5          | Byte 6          | Byte 7         | Byte 8              | Byte 9       | Byte 10      | Byte 11 | Byte 12 | Byte 13 | Notes |
|-----------------|-----|-----------|-----|-----------|-----|-----------------|-----------------|----------------|---------------------|--------------|--------------|---------|---------|---------|-------|
| Steer Data      | 7F  | 127       | FE  | 254       | 8   | Speed (lo)      | Speed (hi)      | Status         | Steer angle (lo)    | Steer angle (hi) | XTE | SC1–8  | SC9–16 | CRC | |
| Steer Settings  | 7F  | 127       | FC  | 252       | 8   | gainP           | highPWM         | lowPWM         | minPWM              | countsPerDeg | Steer offset (lo) | Steer offset (hi) | ackermanFix | CRC |
| Steer Config    | 7F  | 127       | FB  | 251       | 8   | set0            | pulseCount      | minSpeed       | sett1               | ***          | ***          | ***     | ***     | CRC     | |
| From AutoSteer  | 7E  | 126       | FD  | 253       | 8   | Actual steer angle (lo) | Actual steer angle (hi) | IMU heading (lo) | IMU heading (hi) | IMU roll (lo) | IMU roll (hi) | Switch | PWMDisplay | CRC | |
| From AutoSteer2 | 7E  | 127       | FA  | 250       | 8   | Sensor value    | ***             | ***            | ***                 | ***          | ***          | ***     | ***     | CRC     | |

### Machine module

* IP: `192.168.5.123`
* Hello: `123`
* Port: `5123`
* Hello payload: `00 00 56 00 00 7B`

| PGN Name        | Src | Src (dec) | PGN | PGN (dec) | Len | Byte 5 | Byte 6 | Byte 7 | Byte 8 | Byte 9 | Byte 10 | Byte 11 | Byte 12 | Byte 13 | Byte 14 | Byte 15 | Byte 16 | Byte 17 | Byte 18 | Byte 19 | Byte 20 | Byte 21 | Byte 22 | Byte 23 | Byte 24 | Byte 25 | Byte 26 | Byte 27 | Byte 28 | Byte 29 | Byte 30 | Byte 31 | Byte 32 | Byte 33 | Byte 34 | Byte 35 | Byte 36 | Byte 37 | Byte 38 |
|-----------------|-----|-----------|-----|-----------|-----|--------|--------|--------|--------|--------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|
| Machine Data    | 7F  | 127       | EF  | 239       | 8   | uturn  | speed×10 | hydLift | Tram   | Geo Stop | *** | SC1–8 | SC9–16 | CRC | | | | | | | | | | | | | | | | | | | | | | |
| Machine Config  | 7F  | 127       | EE  | 238       | 8   | raiseTime | lowerTime | hydEnable | set0 | User1 | User2 | User3 | User4 | CRC | | | | | | | | | | | | | | | | | | | | | | |
| Pin Config      | 7F  | 127       | EC  | 236       | 24  | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 | 17 | 18 | 19 | 20 | 21 | 22 | 23 | 24 | CRC |
| SectionDimensions | 7E | 126       | EB  | 235       | 33  | 1 | 1Hi | 2 | 2Hi | 3 | 3Hi | 4 | 4Hi | 5 | 5Hi | 6 | 6Hi | 7 | 7Hi | 8 | 8Hi | 9 | 9Hi | 10 | 10Hi | 11 | 11Hi | 12 | 12Hi | 13 | 13Hi | 14 | 14Hi | 15 | 15Hi | 16 | 16Hi | NumSec | CRC |
| From Machine    | 7B  | 123       | ED  | 237       | 8   | 1 | 2 | 3 | 4 | * | ? | ? | ? | CRC | | | | | | | | | | | | | | | | | | | | | | |
| 64 sections     | 7F  | 127       | E5  | 229       | 10  | 1–8 | 9–16 | 17–24 | 25–32 | 33–40 | 41–48 | 49–56 | 57–64 | Lspeed | Rspeed | CRC | | | | | | | |

### IMU module

* IP: `192.168.5.121`
* Hello: `121`
* Port: `5121`
* Hello payload: `00 00 56 00 00 79`

| PGN Name     | Src | Src (dec) | PGN | PGN (dec) | Len | Byte 5 | Byte 6 | Byte 7 | Byte 8 | Byte 9 | Byte 10 | Byte 11 | Byte 12 | Byte 13 |
|--------------|-----|-----------|-----|-----------|-----|--------|--------|--------|--------|--------|---------|---------|---------|---------|
| From IMU     | 79  | 121       | D3  | 211       | 8   | Heading (lo) | Heading (hi) | Roll (lo) | Roll (hi) | Gyro (lo) | Gyro (hi) | 0 | 0 | CRC |
| IMU Disconnect | 7C | 124       | D4  | 212       | 2   | 1 | 0 | CRC | | | | | |

### GPS module

* IP: `192.168.5.124`
* Port: `5124`
* Hello payload: `00 00 56 00 00 7C`

| PGN Name   | Src | Src (dec) | PGN | PGN (dec) | Len | Payload summary |
|------------|-----|-----------|-----|-----------|-----|-----------------|
| Main Antenna | 7C | 124 | D6 | 214 | 51 | Longitude (8 bytes), Latitude (8), Heading true dual (4), Heading true (4), Speed (4), Roll (4), Altitude (4), Satellites tracked (2), Fix quality (1), HDOP×100 (2), Age×100 (2), IMU heading (2), IMU roll (2), IMU pitch (2), IMU yaw rate (2), CRC |

### Tool GPS module

* IP: `192.168.5.125`
* Port: `10000`
* Hello payload: `00 00 56 00 00 7D`

| PGN Name   | Src | Src (dec) | PGN | PGN (dec) | Len | Payload summary |
|------------|-----|-----------|-----|-----------|-----|-----------------|
| Tool Antenna | 7D | 125 | D7 | 215 | — | Tool antenna payload (implementation specific) |

### GPS/IMU/WAS combined module

* IP: `192.168.5.122`
* Port: `5122`
* Hello payload: `00 00 56 00 00 79`

| PGN Name | Src | Src (dec) | PGN | PGN (dec) | Len | Payload summary |
|----------|-----|-----------|-----|-----------|-----|-----------------|
| ToAutosteer | 79 | 122 | F9 | 249 | 8 | Wheel angle sensor low/high. |

### Tool steer module

* IP: `192.168.5.122`
* Hello: `122`
* Port: `5122`
* Hello payload: `00 00 56 00 00 7A`

| PGN Name        | Src | Src (dec) | PGN | PGN (dec) | Len | Payload summary |
|-----------------|-----|-----------|-----|-----------|-----|-----------------|
| Tool Steering   | 7F  | 127       | E9  | 233       | 8   | Vehicle/tool XTE, status, speed×10, CRC. |
| Tool Settings   | 7F  | 127       | E7  | 231       | 8   | Bitfield for inversion, motor driver selection, CRC. |
| From Tool Steer | 7A  | 122       | E6  | 230       | 8   | Actual/error steering feedback, PWM, status, CRC. |
| Switch Control  | 77  | 119       | EA  | 234       | 8   | Main/auto groups, section counts, on/off groups, CRC. |

### Hello, subnet, and diagnostics messages

| Flow | Src | PGN | Len | Purpose |
|------|-----|-----|-----|---------|
| Hello sent to module | 7F | C8 (200) | 3 | Module ID handshake; CRC. |
| Hello replies | 7E/7B/79/78 | same as Src | 5 | Module metadata. |
| Subnet change | 7F | C9 (201) | 5 | IP subnet adjustment. |
| Scan request  | 7F | CA (202) | 3 | Discovery trigger. |
| Subnet replies | 7E/7B/79/78 | CB (203) | 7 | Module IP + subnet info. |
| Hardware message | 7F | DD (221) | var | On-screen message with color + duration. |
| Nudge by machine | 7F | DE (222) | 3 | Section shift commands. |

## Implications for the SRS

* Legacy PGN consumers expect the framing, IDs, and CRC described above.
* Any new transport (gRPC, WebSocket, CAN) must either reproduce this catalog or provide a translator.
* Capability negotiation must not break existing “Hello” and subnet messages without an opt-in upgrade path.
