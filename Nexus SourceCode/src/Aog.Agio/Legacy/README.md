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

`LegacyDiscoveryCodec` handles the identity + capability handshake described in the
SRS (binary PGN `0xD4`). The gateway exposes `PublishDiscoveryAsync` and surfaces
decoded announcements through `ILegacyDiscoveryObserver`. Downstream callers can
convert announcements to `CapabilityDescriptor` records via
`LegacyDiscoveryAnnouncement.ToCapabilityDescriptors()` to bridge the UDP handshake
with the gRPC capabilities exchange.

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

`LegacySteerCodec` provides the steering bridge used by NX-081:

| PGN | Direction | Payload summary | Notes |
| --- | --- | --- | --- |
| `0xFE` | Core → Legacy | `speed_hundredths_kph`, `guidance_status`, `target_angle_hundredths_deg`, `tram_control`, `section_bitmap_low`, `section_bitmap_high` | Encoded via `EncodeSteerCommand`; `SectionMask.Mask` is packed little-endian (sections 1–8 in byte 11, 9–16 in byte 12). |
| `0xFD` | Legacy → Core | `actual_angle_hundredths_deg`, `heading_hundredths_deg`, `roll_hundredths_deg`, `switch_bits`, `pwm` | Decoded via `TryDecodeSteerState`; switch bits expose work/steer/remote inputs and populate `LegacySteerStateMetadata`. |

Steering commands surface through `ILegacySteerCommandObserver`, feedback through
`ILegacySteerStateObserver`, and section bitmasks through `ILegacySectionObserver`.

When available, `LegacySteerCommandMetadata.CurrentSpeedMps` is converted to the legacy
`speed_hundredths_kph` format before being written to payload bytes 5–6 so
downstream legacy hardware receives the current vehicle speed.
The section payload is capped at 16 spray/boom sections. `SectionMask.SectionCount`
values above 16 are truncated and their masks are clipped to 16 bits before
encoding so downstream hardware never sees the extended widths yet surfaced by
`Aog.Core.V1.SectionMask`. Guidance status bytes follow legacy semantics: bit 0
denotes "autosteer engaged" and is toggled automatically from
`SteerCmd.Enable`, while the remaining bits pass through from
`LegacySteerCommandMetadata.GuidanceStatus` for controllers that piggyback mode
flags or error codes. Extended PGNs and wider section masks will remain
unimplemented until the codec enforces the 32-bit framing documented in the SRS.
TODO(NX-042): expand the codec once the extended framing bug is addressed.

## UART framing helper

`LegacySerialFrameCodec` produces and parses the COBS-framed serial messages used by legacy
AgIO hardware. Pass it a complete UDP-style frame (sync bytes through checksum) and it
emits a `0x00`-terminated byte stream suitable for UART links. The helper reuses the legacy
checksum via `LegacyChecksum`, so callers only need to populate the payload before encoding.
