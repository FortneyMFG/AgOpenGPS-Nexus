# Legacy Data Migrator

The legacy data migrator converts AgOpenGPS V6 telemetry captures and field history CSVs
into the Parquet- and JSON-based artefacts consumed by Nexus replay and analytics
workflows.

## Usage

```
dotnet run --project tools/Aog.Tools.LegacyDataMigrator migrate \
    --input /path/to/legacy-field \
    --output ./migrated-field
```

* `--input` should point at a directory containing a `logs/` folder with CSV telemetry
  exports (`pose.csv`, `imu.csv`, `can.csv`, `sections.csv`, `plugin.csv`).
* `--history` can be used to override the default `field-history.csv` location when
  the agronomic history file is stored elsewhere.
* The tool writes Parquet telemetry files plus a `field-history.json` summary to the
  output directory and prints a migration summary to `stdout`.
