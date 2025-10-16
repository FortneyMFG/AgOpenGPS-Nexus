# PoseStream & SectionState Fixtures

These fixtures back ADR-007 deterministic replay validation for PoseStreamService.
Each line in `pose-section-fixture.jsonl` is a [PoseStreamFrame.v1](../../schemas/PoseStreamFrame.v1.json)
entry paired with SectionState deltas. Replay tooling and CI harnesses use the
sequence numbers to verify monotonic ordering, SectionState tallies, and entity
pose hierarchies.

## Files

- `pose-section-fixture.jsonl` — Trimmed 20 Hz sample captured from the planter
  regression path. The first three records mirror the `PoseStreamFrame.sample`
  schema to validate ingestion pipelines.

## Usage

1. Load the JSONL file using the deterministic SimClock harness.
2. Validate each record against `PoseStreamFrame.v1` and ensure SectionState
   tallies match [`SectionStateTally.v1`](../../schemas/SectionStateTally.v1.json).
3. Compare aggregated tallies to expected values in CI to detect drift.
