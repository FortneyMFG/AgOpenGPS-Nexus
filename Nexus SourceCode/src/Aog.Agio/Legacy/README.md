# Legacy UDP Gateway Skeleton

The legacy gateway bridges AgOpenGPS UDP PGNs with the typed gRPC contracts used by
the Nexus core. The initial skeleton focuses on the main GPS antenna PGN (`0xD6`) and
maps it to the `Aog.Core.V1.Pose` message.

## Runtime wiring

Add the backend to `AgioHost:Backend` so the AGiO host loads it instead of the default
simulation backend:

```json
{
  "AgioHost": {
    "Backend": {
      "Assembly": "Aog.Agio",
      "Type": "Aog.Agio.Legacy.LegacyUdpGatewayBackend"
    }
  }
}
```

The backend registers `LegacyUdpGateway` with no-op transport and observer instances.
Hardware-specific transports can replace `ILegacyUdpTransport`/`ILegacyPoseObserver`
through dependency injection to forward PGNs over UDP and surface decoded poses to Core.

## Encoding

`LegacyPoseCodec` preserves the legacy framing semantics:

* Sync bytes: `0x80 0x81`.
* Source address: `0x7C` by default for the GPS antenna.
* PGN: `0xD6` with a 51-byte payload and 8-bit checksum (sum of bytes `2..n-2`).
* Pose fields (latitude, longitude, heading, roll, altitude, speed) are converted to
  their protobuf equivalents, while metadata such as fix quality and HDOP are exposed via
  `LegacyPoseMetadata` so callers can retain them when publishing to Core.

The codec is symmetric—unit tests assert that encoding and decoding round-trip cleanly and
that corrupt checksums are rejected.

## Steering and section commands

`LegacyAutoSteerCodec` emits the auto-steer PGN (`0xFE`) that legacy steer and section
controllers expect. The codec combines `SteerCmd` and `SectionMask` messages into a single
frame with the following layout:

| Byte offset | Field | Source |
| --- | --- | --- |
| 0 | Sync (`0x80`) | Legacy framing |
| 1 | Sync (`0x81`) | Legacy framing |
| 2 | Source address (`0x7F`) | PC host |
| 3 | PGN (`0xFE`) | Auto-steer data |
| 4 | Payload length (`8`) |  |
| 5 | Speed LSB | (reserved, zeroed) |
| 6 | Speed MSB | (reserved, zeroed) |
| 7 | Status (`0`/`1`) | `SteerCmd.Enable` |
| 8 | Target angle LSB | `SteerCmd.TargetWheelAngleDeg * 100` |
| 9 | Target angle MSB | `SteerCmd.TargetWheelAngleDeg * 100` |
| 10 | Light-bar distance | (reserved, zeroed) |
| 11 | Section bits 0-7 | `SectionMask.Mask` |
| 12 | Section bits 8-15 | `SectionMask.Mask` |
| 13 | Checksum | Sum of bytes `2..12` |

Higher section bits are truncated for the initial minimal bridge; a follow-up task will map
the extended section PGNs once Core exposes the additional bits.
