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
