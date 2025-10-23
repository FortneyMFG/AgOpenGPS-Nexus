# Legacy auto-run scenario pack (NX-090)

This pack snapshots the baseline configuration required to replay legacy UDP
traffic inside Nexus. It complements the dealer toolkit by providing a ready-to-
run scenario plus sample verification logs captured with the soak harness.

## Files

- [`legacy-auto-run.json`](legacy-auto-run.json) — scenario definitions that bind the Nexus
  runtime to legacy UDP pose, steering, and section streams.
- [`verification/soak-report.sample.json`](verification/soak-report.sample.json) — example
  output from `legacy-tool soak --seconds 30` showing balanced frame counts.
- [`verification/checklist.md`](verification/checklist.md) — operator checklist used after the
  soak run.

## Usage

1. Import `legacy-auto-run.json` via **Simulation → Edit scenarios…** and choose
   the `legacy-auto-run` preset to bind the legacy streams to the Nexus runtime.
2. Run the soak harness (`legacy-tool soak --seconds 30 --output soak.json`) on
   the bench rig and confirm the totals match the sample report.
3. Archive the soak output alongside the dealer checklist for traceability.

## Verification logs

Sample soak report ([`verification/soak-report.sample.json`](verification/soak-report.sample.json)):

```json
{
  "udp": {
    "poseFrames": 30,
    "steerCommandFrames": 30,
    "steerStateFrames": 30,
    "sectionFrames": 30,
    "effectiveRateHz": 90.0
  }
}
```

This demonstrates that pose, steering command, steering state, and section
frames were captured in lockstep during the bench soak.
