# Aog.Core.Mesh

The mesh package hosts the in-memory implementation of the live telemetry mesh core
service defined in [ADR-047](../../../docs/ADR/ADR-047_LiveTelemetryMesh.md).
`LiveTelemetryMeshService` exposes registration, publication, subscription, and presence
management primitives that mirror the pub/sub overlay described in the ADR.

Key behaviors:

- **Access control.** Share and subscribe profiles are normalised at registration time
  and validated for each publish or subscription request. Wildcards are supported using
  the `*` token in season/job scopes or by omitting layer lists.
- **Topic hygiene.** Topics must follow the `aog/live/{season}/{job}/{layer}` convention
  and are re-composed with trimmed identifiers before broadcasts fan out to watchers.
- **Presence TTL.** Presence heartbeats expire after five seconds (ADR default). Expired
  entries are pruned lazily whenever the presence list is queried.
- **Deterministic tests.** A `TimeProvider` dependency allows unit tests to control time
  flow when validating TTL and sequencing logic.
- **Diagnostics.** `GetDiagnostics()` surfaces ACL denials, fan-out counts, and presence
  expirations for observability.
- **ACL enforcement.** Publish, subscribe, and presence operations respect the normalized
  Share/Subscribe profiles. Access denials are tallied into diagnostics for operator
  tooling.

Follow-up tasks (NX-228 and beyond) will add the RadioBridge transport adapters that push
these publications out to hardware links.
