# AGiO Host Safety Logging

The AGiO host now records watchdog transitions and failsafe actions to structured JSONL files.
By default logs are written under `logs/safety` alongside the host binary. Each entry captures a
UTC timestamp, event type, and metadata describing the actuator or watchdog reason.

## NTRIP Client backend

The official NTRIP client backend streams RTCM corrections from internet casters and forwards
them to enabled AOG-Link transports. Enable the backend by selecting
`Aog.Agio.Ntrip.NtripClientBackend` and configuring the `AgioHost:NtripClient` section:

```json
{
  "AgioHost": {
    "Backend": {
      "Assembly": "Aog.Agio",
      "Type": "Aog.Agio.Ntrip.NtripClientBackend"
    },
    "NtripClient": {
      "Host": "caster.example.com",
      "Port": 2101,
      "MountPoint": "MY-MOUNT",
      "Username": "rtk-user",
      "Password": "secret",
      "UseTls": true,
      "ReconnectBackoff": "00:00:05"
    }
  }
}
```

- **Host / Port / MountPoint** — Target caster endpoint and mountpoint name.
- **Username / Password** — Optional basic-auth credentials; omit both for anonymous access.
- **UseTls** — Enables TLS negotiation for casters served over HTTPS.
- **ReconnectBackoff** — Delay applied before reconnecting when the connection is interrupted.

## GNSS correction policy aggregator

Upcoming correction services (local base, radio relays, PPP/NTRIP) share a common failover policy
implemented by `Aog.Agio.Corrections.CorrectionSourceAggregator`. The aggregator cycles through
registered `ICorrectionSourceFactory` instances, activates the first available source, and falls
back when the active provider completes, is cancelled, or faults.

Register the aggregator in DI and bind `CorrectionSourceAggregatorOptions` to control which source
families participate and how aggressive the retry cadence should be:

```json
{
  "AgioHost": {
    "Corrections": {
      "EnableNetworkSources": true,
      "SourceFailureBackoff": "00:00:01",
      "ExhaustedBackoff": "00:00:05",
      "PreferredOrder": [
        "LocalBaseStation",
        "SerialRadio",
        "NetworkService",
        "Replay"
      ]
    }
  }
}
```

- **EnableNetworkSources** — Disable to require on-site base/radio providers only.
- **SourceFailureBackoff** — Delay before the next candidate is tried after a fault.
- **ExhaustedBackoff** — Delay before the scan repeats when no sources are available.
- **PreferredOrder** — Optional ordered list of `CorrectionSourceKind` values.

## Configuration

The `AgioHost:SafetyLogs` section controls retention and output paths:

```json
{
  "AgioHost": {
    "SafetyLogs": {
      "Directory": "logs/safety",
      "RetentionDays": 30,
      "MaxFiles": 90
    }
  }
}
```

- **Directory** — Location where daily JSONL files are created.
- **RetentionDays** — Minimum number of days to keep (inclusive).
- **MaxFiles** — Hard cap on the number of retained daily files.

Retention is enforced whenever new events are recorded or when the export tool runs.

## Exporting logs

Use the `Aog.Tools.SafetyLog` utility to bundle retained files into a zip archive:

```bash
# Create an archive using defaults
dotnet run --project "tools/Aog.Tools.SafetyLog/Aog.Tools.SafetyLog.csproj" -- export --log-dir logs/safety --output out
```

The tool prints the absolute path to the generated archive and respects optional
`--retention-days` and `--max-files` overrides when building the package.

## Mesh presence integration

The AGiO host publishes live pose telemetry onto the [ADR-047](../../../docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md)
mesh by default. `MeshTelemetryAggregator` listens to decoded pose samples, registers the
host as a mesh device, and emits presence heartbeats plus trail snapshots at a configurable
interval. Configure the bridge under `AgioHost:Mesh` to customise the device identifier,
label, and trail behaviour:

```json
{
  "AgioHost": {
    "Mesh": {
      "DeviceId": "tractor.alpha",
      "DeviceLabel": "Tractor Alpha",
      "Capabilities": [ "telemetry", "presence" ],
      "TrailCapacity": 200,
      "TrailPublishInterval": "00:00:02"
    }
  }
}
```

Presence updates require that pose headers include `SeasonId` and `JobId` metadata. Trail
payloads are serialized as JSON arrays (camelCase) with the most recent trail points and are
published on `aog/live/{season}/{job}/trail` with `MeshDataTier.Trails` permissions.

## RadioBridge ELRS adapter

The RadioBridge ELRS adapter (NX-236/NX-239) forwards mesh publications over ELRS or simulated
radio links using the transport defined in [ADR-048](../../../docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md).
Enable the adapter by configuring `AgioHost:RadioBridge:Elrs`:

```json
{
  "AgioHost": {
    "RadioBridge": {
      "Elrs": {
        "Enabled": true,
        "DeviceId": "bridge.elrs.alpha",
        "DeviceLabel": "Field Radio Bridge",
        "Endpoint": "sim://loopback",
        "SendInterval": "00:00:00.100",
        "DiagnosticsInterval": "00:00:05",
        "DiagnosticsSeasonId": "system",
        "DiagnosticsJobId": "radio"
      }
    }
  }
}
```

- **Endpoint** — Use `sim://` for integration tests or `serial://ttyUSB0?baud=420000` for hardware.
- **SendInterval** — Controls how frequently the adapter scans for retransmissions.
- **DiagnosticsInterval** — Publishes JSON summaries on
  `aog/live/{DiagnosticsSeasonId}/{DiagnosticsJobId}/{DeviceId}.radio` with RSSI and retry counters.

The adapter registers itself with the mesh, relays outbound publications to the radio bridge, and
feeds inbound publications back into the mesh. It ships with firmware stubs and a simulator under
`Aog.Agio.RadioBridge.Simulation` that unit tests and firmware teams can use while hardware
drivers evolve (NX-241).

## RadioBridge LoRa adapter

The RadioBridge LoRa adapter (NX-237/NX-244) targets concentrators and modems that expose
LoRa links. It reuses the RadioBridge transport with forward error correction enabled and a
slower retry cadence tuned for low-bitrate radios. Configure the adapter via
`AgioHost:RadioBridge:Lora`:

```json
{
  "AgioHost": {
    "RadioBridge": {
      "Lora": {
        "Enabled": true,
        "DeviceId": "bridge.lora.alpha",
        "DeviceLabel": "LoRa Radio Bridge",
        "Endpoint": "lora://ttyACM0?baud=57600",
        "SendInterval": "00:00:00.250",
        "DiagnosticsInterval": "00:00:05",
        "DiagnosticsSeasonId": "system",
        "DiagnosticsJobId": "radio-lora",
        "EnableForwardErrorCorrection": true
      }
    }
  }
}
```

- **Endpoint** — Accepts `lora://` URIs for serial concentrators (defaults to 57 600 baud) and
  `sim://lora-loopback` for integration tests.
- **EnableForwardErrorCorrection** — Emits Hamming(12,8) parity blocks on data frames to improve
  resilience when RSSI degrades. Disable this flag for legacy firmware that cannot decode the FEC
  bit.
- Mesh publications forwarded by the LoRa adapter include diagnostic metadata such as
  `radio.kind=lora` and `radio.fec=enabled` so downstream services can differentiate transports.

Diagnostics for the LoRa adapter follow the same topic layout as ELRS but include additional
fields indicating the configured send interval and whether FEC is active. This documentation
bundle pairs with the provisioning kit described in [RadioBridge provisioning](../../docs/Core/howto/radio/radiobridge-provisioning.md).
