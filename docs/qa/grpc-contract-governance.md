# gRPC Contract Governance Checklist

ADR-002 establishes gRPC + protobuf as the shared contract surface for Nexus services. This checklist operationalises that guidance so schema changes stay compatible and every release captures the expected review artefacts.

## Governance Roles

- **Contract clinic.** Architecture hosts a bi-weekly review where Core, AgIO, Plugin, and Bridge leads triage proposed protobuf diffs and negotiate rollout timelines.
- **Proto steward.** The `Aog.Abstractions` maintainer publishes versioned packages, tracks reserved field ranges, and coordinates simultaneous updates to generated clients.
- **Bridge verifier.** The Bridge team owns golden PGN/AOG-Link fixtures and ensures translators handle new/changed fields without data loss.

## Change Control Workflow

1. **Author prep.** Run `buf lint`/`buf breaking` locally (using the repository config) and capture the diff summary with semantic version recommendations.
2. **Clinic review.** Present the change, updated manifests, and bridge impact. Secure sign-off from Core, AgIO, and Plugin leads.
3. **Golden validation.** Replay the gRPC payloads through the Bridge regression harness and attach the round-trip logs.
4. **Documentation.** Update ADR references, changelogs, and how-to guides that rely on the
   modified contract. Link to affected tasks in [`tasks.md`](../../tasks.md).
5. **Publication.** Merge the change only after the `Aog.Abstractions` package is built and published to the internal feed with the agreed semantic version.

## Release Exit Criteria

- [ ] All protobuf descriptors in `Nexus SourceCode/proto` share the same released version tag.
- [ ] The Bridge harness captures a current replay showing successful translation between gRPC, AOG-Link, and PGN payloads.
- [ ] Release notes include a compatibility statement (additive/breaking) and upgrade guidance for plugin authors.
- [ ] `Aog.Abstractions` NuGet package is available on the internal feed and referenced by the release branch.

Following this process keeps contract churn predictable and aligned with ADR-002, reducing integration risk for downstream components.
