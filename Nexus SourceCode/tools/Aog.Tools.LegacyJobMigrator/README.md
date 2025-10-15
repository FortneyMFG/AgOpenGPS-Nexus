# Legacy Job Migrator

The legacy job migrator upgrades `job.json` documents created before ADR-041 to the
session-aware schema. It injects a session summary, assigns an optional season, and
normalises timestamps so validation against `job.schema.json` succeeds.

## Usage

```
dotnet run --project tools/Aog.Tools.LegacyJobMigrator migrate \
    --input /path/to/job.json \
    --season season:2025 \
    --operator user:operator.maya
```

* `--input` points at the legacy `job.json` file. The tool updates the file in place
  unless `--output` is provided.
* `--season` assigns or overrides the job's `context.seasonId`.
* `--operator` can be specified multiple times to seed `activeOperators` on the
  generated session summary.
* `--session-name` overrides the default "Migrated Session" label used for the
  synthesised session entry.

The migrator is idempotent. If `job.json` already contains session entries, it skips
the file unless `--force` is supplied.
