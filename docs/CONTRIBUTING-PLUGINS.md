# Plugin Contribution Guide

## Packaging & Distribution

- Package plugins as ZIP archives containing binaries, manifests, and optional assets. The root manifest (`plugin.manifest.json`) must declare capabilities, layer definitions, required ADR alignment, and signature metadata.
- Sign manifests using the repository’s signing policy (deterministic SHA-256 + optional code signing certificate). Include signature blobs under `signatures/` and list trusted issuers.
- Provide a rollback plan: each release notes compatible Core/ADR revisions, migration steps, and fallback versions.

## Catalog Governance

- Register new layers with the Layer Registry via PRs referencing ADR-010. Include schema files under `/schemas`, examples under `/schemas/examples`, and planned/actual flags (`x-nexus-planned`, `x-nexus-actual`).
- For editable layers, integrate with the LayerEditService, providing attribute panel definitions and undo/redo semantics as described in ADR-044.
- When publishing cost, profit, crop, genetics, or yield layers, link to the corresponding ADRs (045–050) and ensure session provenance is populated.【F:docs/ADR/ADR-045_CropTypePlugin.md†L29-L71】【F:docs/ADR/ADR-046_GeneticsPlugin.md†L21-L66】【F:docs/ADR/ADR-049_YieldPlugin.md†L21-L52】【F:docs/ADR/ADR-050_CostProfitPlugin.md†L21-L52】

## Permissions & Safety

- Declare capability requirements (pose.read, section.command, storage.write, etc.) in the manifest. Plugins attempting unauthorized actions are rejected by Core’s capability gate per ADR-018.
- Remote dashboards default to monitor-only; plugins must not escalate permissions without an operator-approved lease, aligning with SRS §09 safety posture.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L33-L60】
- Multi-machine data sharing must respect share/subscribe profiles. Sensitive topics (layer edits, profitability) remain opt-in.【F:docs/plugins/MultiMachine.md†L1-L80】

## Submission Checklist

1. Update or reference relevant ADRs/SRS sections.
2. Add or update JSON schemas and examples when new payloads ship.
3. Include documentation updates under `docs/plugins/`.
4. Provide automated tests or fixtures demonstrating determinism.
5. Attach QA notes and rollback plans in the PR description.

Following this guide keeps the Nexus plugin ecosystem governed, auditable, and aligned with the 2025 architecture refresh.
