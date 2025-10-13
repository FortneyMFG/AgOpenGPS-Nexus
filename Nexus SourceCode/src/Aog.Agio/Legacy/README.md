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

## UART framing helper

`LegacySerialFrameCodec` produces and parses the COBS-framed serial messages used by legacy
AgIO hardware. Pass it a complete UDP-style frame (sync bytes through checksum) and it
emits a `0x00`-terminated byte stream suitable for UART links. The helper reuses the legacy
checksum via `LegacyChecksum`, so callers only need to populate the payload before encoding.
