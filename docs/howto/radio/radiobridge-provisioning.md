# RadioBridge provisioning kit

NX-244 introduces a repeatable provisioning workflow for the RadioBridge transports described in
[ADR-048](../../SRS/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md). This guide covers generating device profiles,
activating ELRS and LoRa adapters, and validating that the new forward error correction settings are
applied consistently across the fleet.

## Prerequisites

- .NET SDK 8.0 on the provisioning workstation.
- Access to the Nexus source tree (the provisioning CLI lives in
  [`tools/Aog.Tools.RadioBridge`](../../Nexus%20SourceCode/tools/Aog.Tools.RadioBridge)).
- Mesh credentials for the farm or lab environment where the bridge devices will be staged.

## Generate provisioning profiles

Use the RadioBridge tooling to mint per-device JSON profiles containing device identifiers,
pre-shared keys, and topic tiers.

```bash
# From Nexus SourceCode/tools/Aog.Tools.RadioBridge
$ dotnet run -- provision \
    --device-id bridge.lora.alpha \
    --label "LoRa Bridge Alpha" \
    --radio-kind lora \
    --capability radio \
    --capability bridge \
    --capability lora \
    --key-bytes 16 \
    --output /secure-share/radio/bridge.lora.alpha.json
```

If no output path is supplied the profile is emitted to stdout so that air-gapped environments can
paste the document into secured tooling. The CLI also supports deterministic keys for lab fixtures:
`dotnet run -- provision --device-id bridge.elrs.dev --key 0123456789ABCDEF`. The entry point lives in
[`Aog.Tools.RadioBridge.csproj`](../../Nexus%20SourceCode/tools/Aog.Tools.RadioBridge/Aog.Tools.RadioBridge.csproj) so you can
embed it into provisioning automation.

### Profile schema

Each provisioning file follows the [`RadioBridgeProvisioningProfile`](../../Nexus%20SourceCode/tools/Aog.Tools.RadioBridge/RadioBridgeProvisioningProfile.cs) contract:

| Field | Description |
| ----- | ----------- |
| `deviceId` | Mesh device identifier registered with the Live Telemetry Mesh. |
| `label` | Friendly label shown in diagnostics payloads. |
| `radioKind` | Either `elrs` or `lora`. Used to pick adapter defaults and diagnostics metadata. |
| `capabilities` | Additional capability strings advertised during registration. |
| `preSharedKey` | Hex-encoded pre-shared key for transport encryption. |
| `topics` | Optional allow-list describing the topics the bridge should subscribe to or share. |

Store the generated JSON in a secure vault or configuration repository. Never commit production keys
back to git.

## Configure the AGiO host

1. Copy the provisioning profile onto the device running `Aog.Agio`. The recommended path is
   `/opt/nexus/radio/<device-id>.json` with permissions restricted to the service account.
2. Update [`appsettings.json`](../../Nexus%20SourceCode/src/Aog.Agio/appsettings.json) (or the environment
   variables used in production) with the appropriate RadioBridge adapter settings. For LoRa bridges, enable forward error correction and point the
   endpoint at the serial concentrator:

   ```json
   "RadioBridge": {
     "Lora": {
       "Enabled": true,
       "DeviceId": "bridge.lora.alpha",
       "DeviceLabel": "LoRa Radio Bridge",
       "Endpoint": "lora://ttyACM0?baud=57600",
       "EnableForwardErrorCorrection": true,
       "DiagnosticsSeasonId": "system",
       "DiagnosticsJobId": "radio-lora"
     }
   }
   ```

   ELRS bridges use the same structure under `RadioBridge:Elrs` and typically run with
   `EnableForwardErrorCorrection` set to `false`.
3. Restart the [`Aog.Agio` host](../../Nexus%20SourceCode/src/Aog.Agio/Aog.Agio.csproj) so that the adapter picks up the new configuration.

## Validation checklist

- `dotnet test Nexus SourceCode/tests/Aog.Core.Tests --filter RadioBridgeTransportTests` — verifies
  retry logic and Hamming(12,8) decoding (NX-242) using
  [`RadioBridgeTransportTests`](../../Nexus%20SourceCode/tests/Aog.Core.Tests/Mesh/RadioBridgeTransportTests.cs).
- Inspect mesh diagnostics for the new device: the payload should include `radio.kind`,
  `radio.fec`, RSSI, and retry counters.
- Trigger a test publication (for example by replaying a coverage topic) and confirm the bridge
  forwards frames to the radio modem or simulator.

## Operational notes

- LoRa adapters default to a 250 ms send interval and seven retransmission attempts. Adjust the
  values in the options file if local spectrum rules require more conservative behaviour.
- The provisioning CLI can be embedded into existing manufacturing tooling. Pass `--manifest`
  pointing at a CSV to bulk-generate profiles for entire production batches.
- When rotating keys, regenerate the JSON profile and update `PreSharedKey` on the device. The
  adapter reloads the profile on restart.
