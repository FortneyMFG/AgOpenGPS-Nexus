# NX-076 Fault injection harness

The QA console also generates deterministic fault-injection timelines so simulation and bench
runs can replay power, network, and sensor perturbations without hand-editing scripts. Fault
definitions declare event type, target, start offset, duration, severity, and optional jitter window;
the harness resolves jitter using a reproducible seed and exports an ordered schedule for test
frameworks to consume.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Faults/FaultInjectionRunner.cs†L9-L74】

## Scenario format

A scenario JSON file contains:

- **name** — free-form description of the campaign.
- **seed** — optional integer to guarantee deterministic jitter.
- **events** — array of events with `type`, `target`, `offsetSeconds`, `durationSeconds`,
  `severity` (0-1), optional `jitterSeconds`, and free-form `notes`.

Sample scenarios live under `tools/qa/faults`. When executed, the harness applies jitter (if
present), clamps negative offsets, and sorts the schedule by start time. The returned schedule
includes the total duration and each resolved event so orchestration scripts can pause/resume
hardware injectors at the right moment.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Faults/FaultInjectionRunner.cs†L24-L68】

## Building a schedule

```bash
# Generate a schedule from the sample scenario and save it for dashboards
dotnet run --project "Nexus SourceCode/tools/Aog.Tools.Qa/Aog.Tools.Qa.csproj" -- fault run \
  --scenario tools/qa/faults/power-network.json \
  --output out/fault-schedule.json
```

Because the output captures jittered offsets, the same scenario and seed always produce an
identical timeline. This makes it safe to version-control schedules or feed them into replay
tests that expect fixed timings.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Program.cs†L142-L180】

## Authoring new scenarios

1. Enumerate the failure modes to exercise (e.g., CAN drop, sensor saturation, DC brownout).
2. Add events with reasonable durations and severity; include `jitterSeconds` when you want
   variation but still need deterministic scheduling.
3. Store the scenario under `tools/qa/faults` and run `qa fault run` to emit the resolved
   schedule. The harness throws descriptive errors if required fields are missing, if durations are
   not positive, or if targets are undefined, keeping scenarios honest.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Faults/FaultInjectionRunner.cs†L54-L66】

By capturing these timelines in source control we gain repeatable coverage for recovery
behaviours—ensuring future firmware and host builds survive network drops, power cycles,
and synthetic sensor failures before they reach the field.
