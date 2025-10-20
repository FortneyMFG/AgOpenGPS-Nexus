# O-TELE-4: Layer diagnostics and health monitoring

## Summary
Adds observability tooling that tracks packet rates, bad samples, and legend parity across variable-rate layers so field operators and CI pipelines can confirm data quality before and during application.

## Details
- Inspector panel surfaces raw payload bytes, decoded engineering values, quality, weight, and source PGN metadata, mirroring AgDiag to simplify field triage.
- Provide a packet-rate monitor overlay (reusing AgDiag graphs) that highlights when UDP/CAN utilization nears recommended limits and correlates spikes with specific layers.
- Add a legend parity test shortcut rendering shared color ramps in both OpenGL and WPF to confirm matching bin edges and colors after metadata updates.
- Track `badSample` counters per layer whenever decoded values are NaN/Inf or exceed expected bounds and expose them in dashboards/diagnostics logs.
- Publish operator guidance for inspector workflow, rate-limit thresholds, and troubleshooting steps in release notes.

## Pros
- Makes new layer telemetry auditable in the field without specialist tooling.
- Shares test fixtures between desktop UI and diagnostics, reducing divergence.
- Encourages disciplined monitoring of bandwidth and sample quality.

## Cons
- Additional overlays and counters may crowd the UI if not designed carefully.
- Requires continued maintenance to keep diagnostic flows aligned with evolving PGNs.
- Operators need training to interpret new metrics and charts correctly.

## Risks & mitigations
- **Risk:** Diagnostics degrade render performance. **Mitigation:** Gate heavy overlays behind opt-in toggles and reuse backend snapshots rather than recalculating values per frame.
- **Risk:** Users ignore alerts due to noise. **Mitigation:** Provide hysteresis/hold settings for alarm bands and default thresholds tuned to typical rigs.
- **Risk:** Tooling drift between AgDiag and the main app. **Mitigation:** Share libraries for PGN decoding and legend rendering so both apps remain in sync.

## Borrowables
- AgDiag graphs, logging infrastructure, and inspector concepts can be ported directly.
- Existing telemetry monitors in AgIO provide starting points for rate-limit visualization.
- Current troubleshooting documentation can be expanded with layer-specific playbooks.

## Rough effort
M — Requires coordinated updates to diagnostics overlays, logging, and documentation plus shared libraries for decoding.

## References
- [Section 64 — Telemetry & Health](../sections/6X_Core_Domain_Services/64_Telemetry_Health.md)
- [Section 74 — Monitoring Systems](../sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md)
- [AgIO PGN baseline](../references/AgIO_PGN_Baseline.md)

## Related ADRs
- [ADR-019 — Provenance, Audit, & QA](../../ADR/ADR-019-provenance-audit-qa.md)
- [ADR-047 — Live Telemetry Mesh](../../ADR/ADR-047_LiveTelemetryMesh.md)
- [ADR-052 — Field Health Plugin](../../ADR/ADR-052_FieldHealthPlugin.md)

