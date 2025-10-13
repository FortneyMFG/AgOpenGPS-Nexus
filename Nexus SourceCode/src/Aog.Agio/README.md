# AGiO Host Safety Logging

The AGiO host now records watchdog transitions and failsafe actions to structured JSONL files.
By default logs are written under `logs/safety` alongside the host binary. Each entry captures a
UTC timestamp, event type, and metadata describing the actuator or watchdog reason.

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
