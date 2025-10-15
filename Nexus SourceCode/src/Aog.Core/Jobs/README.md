# JobsService Filesystem Store

NX-221 introduces a filesystem-backed `JobsService` implementation that is
responsible for orchestrating job lifecycle transitions inside the Core host.
The service persists metadata to a `job.json` document within each job folder,
tracks the active job via an `active.json` marker, and mirrors the legacy
`Resume.txt` file so V6 workflows remain functional.

## Key concepts

- **Job store** — `FileSystemJobStore` manages job folders under the configured
  `RootDirectory`, creating folders on demand and keeping metadata in sync with
  lifecycle events.
- **Lifecycle orchestration** — `JobLifecycleOrchestrator` exposes `Create`,
  `Resume`, `Close`, `GetActive`, and `List` verbs while emitting
  `JobStateChanged` events for interested observers.
- **Hosted service** — `JobsHostedService` initialises the store during host
  startup and logs lifecycle transitions so operators and telemetry sinks can
  trace state changes.

## Configuration

`JobsServiceOptions` are supplied through `CoreHost:Jobs` in `appsettings.json`
or `NEXUS_CoreHost__Jobs__*` environment variables:

```json
{
  "CoreHost": {
    "Jobs": {
      "RootDirectory": "jobs",
      "ActiveStateFileName": "active.json",
      "DefaultSessionName": "Session 1"
    }
  }
}
```

`RootDirectory` is created automatically when the service starts. The default
values favour development scenarios and can be redirected to production storage
when deploying the Core host.
