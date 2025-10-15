# ADR-030: Field job sessions and lifecycle services

## Status
Proposed

**Relevant Plugin(s):** Job Tasks, Mapping, Variable Mapping, UI Shell, Device Manager, Telemetry Logging



## Context
AgOpenGPS v6 exposes field sessions as loosely-structured folders with ad-hoc menu flows for New, Resume, Open, Drive-In, and import verbs. Coverage tiles, boundaries, and guidance data live side-by-side, and Resume.txt is the only structured metadata. The Nexus Core host now orchestrates plugins, presets, and layouts, but has no first-class job lifecycle model, making it difficult to coordinate autosave, geofence discovery, or plugin participation. A unified Job abstraction is required so Core, UI, and plugins can exchange consistent metadata, load/save workflows, and resume behavior while remaining compatible with V6 job archives.

## Decision
Establish Jobs as a first-class concept spanning Core, UI, and plugins with the following pillars:

1. **Job metadata contract.** Define a versioned `aog.job.v1` JSON schema stored as `job.json` inside each job folder. Metadata captures identity, timestamps, spatial hints, asset paths (boundaries, coverage, guidance, prescriptions, attachments), equipment/preset pointers, layout references, and user tags. Resume.txt remains updated for V6 compatibility.
2. **Filesystem job store.** Maintain a `/Jobs/<Job DisplayName>/` root containing `job.json`, `Resume.txt`, and a `data/` subtree for boundaries, guidance, coverage tiles, prescriptions, and attachments. Hosts may add cloud-backed mirrors but must persist local structure for offline workflows.
3. **JobsService lifecycle API.** Provide a Core-hosted gRPC service exposing verbs for `New`, `Resume`, `Open`, `DriveIn`, `Import`, `Clone`, `Save`, and `Close`, plus `GetActive`, `List`, and `Watch` for state transitions and progress events. Importers accept ISOXML, KML, or plugin-defined types and normalize artifacts into the job store before activation.
4. **UI integrations.** Mirror the legacy Job menu (New, Resume, Open, Drive-In, Import ISOXML, From KML, Clone Existing, Close) and surface the active job in a top-bar chip that opens a job drawer showing boundaries, coverage status, guidance sets, and implement context. Drive-In monitors geofences and prompts operators when proximity matches a stored job.
5. **Plugin extensibility.** Allow plugins to register Job Sources under the Import menu, contribute decorators that attach data on open/save/close, and receive lifecycle hooks (`onJobOpen`, `onJobSave`, `onJobClose`) gated by `jobs.lifecycle` permissions. Job metadata can link to shared Layouts or Presets; live updates flow through these links while allowing snapshot overrides for historical integrity.
6. **Safety & autosave.** Track dirty state for coverage and guidance edits, autosave at intervals and before risky operations, and run crash-safe journaling for coverage tiles so replay can restore state after interruptions.

### Degraded operation & messaging
- **No mapping provider:** When mapping capabilities are absent (`mapping:offline`), the JobsService annotates active jobs as "Map-light" and skips coverage journaling expectations. The UI still renders job metadata and Drive-In prompts but adds a banner clarifying that coverage playback will be limited. Once mapping returns, Core backfills coverage pointers without forcing operators to restart the job.
- **Plugin hooks unavailable:** If lifecycle-capable plugins decline hooks (e.g., automation plugin disabled), Core logs the skipped hooks with reason codes and surfaces a toast in the Activity pane so operators understand why certain automations did not run. Jobs remain openable/resumable, but the job drawer highlights affected integrations.
- **Filesystem pressure / read-only media:** When the job store detects read-only media or low disk, JobsService automatically shifts to rolling snapshot mode and warns operators before autosave would fail. Crash recovery prompts include guidance on exporting the job or freeing space prior to resuming full journaling.

## Consequences
- **Consistent lifecycle orchestration.** Core, UI, and plugins share a single authority for job identity and storage, enabling autosave policies, Drive-In geofence matching, and deterministic crash recovery while keeping V6 Resume flows functional.
- **Extensible import pipeline.** ISOXML, KML, and plugin-defined importers normalize assets into the job store, making future sources (cloud prescriptions, RTK base lists) pluggable without diverging UI experiences.
- **Shared layout & preset context.** Jobs may reference presets or layouts by link or snapshot, letting seasonal layout updates propagate automatically while preserving overrides when required.
- **Follow-up work.** Implement the `aog.job.v1` schema and helpers, build the JobsService host, refresh the UI menu and drawer, port ISOXML/KML importers, add Drive-In discovery with geofence indexing, wire autosave + coverage journaling, and provide migration tools for V6 archives.

## Governance Updates
- **Schema migration tooling.** JobsService ships a semantic diff tool that highlights layout, asset, and provenance changes between versions. Migration PRs must attach generated reports.
- **Transactional hooks.** Plugin hook contracts declare commit/rollback semantics. Core enforces these hooks so partial failures revert gracefully and log reasons.
- **Release gates.** Before promoting schema changes, maintainers run full job lifecycle rehearsals (create, execute, archive) covering automation hooks and UI integrations.

## Validation
- **Crash recovery:** Resume-from-crash workflows must restore the previously active job within 8 seconds and avoid duplicating more than one PoseStream segment in journal entries.
- **Migration coverage:** The legacy archive migration harness must successfully convert at least 50 representative V6 jobs without schema validation failures, emitting warnings whenever fields are downgraded or skipped.
- **Drive-In accuracy:** Drive-In geofence discovery must populate implement entry/exit events with ≤ 50 cm spatial error when replayed against recorded RTK datasets.

## Legacy Implementation Notes
### AgOpenGPS v6
- Stores field sessions as folder trees with `Resume.txt` markers, coverage bins, and assorted JSON files; job metadata is implicit and tightly coupled to UI flows.

### Legacy Dev Branch
- Mirrors V6 behaviors without a centralized Jobs service or metadata schema; Drive-In relies on direct geofence scans of legacy folders.

## References
- [Data model & storage requirements](../SRS/sections/08_Data_Model_Storage.md)
- [Plugin lifecycle](../ADR/ADR-018-plugin-api.md)
- [Mapping & coverage responsibilities](../ADR/ADR-029-mapping-plugin-architecture.md)
