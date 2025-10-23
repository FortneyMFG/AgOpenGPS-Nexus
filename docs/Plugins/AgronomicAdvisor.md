# AI-Assisted Agronomic Advisor Plugin (Planned)

The Agronomic Advisor plugin leverages Nexus historical datasets to generate field-specific recommendations and variable-rate plans.

## Data foundation

- Ingests yield history, soil layers, weather snapshots, crop/genetics records, and management zones through ADR-013 derivation pipelines.
- Supports offline model packaging (ONNX/ML.NET bundles) with versioned manifests, feature schemas, and reproducible training metadata.
- Stores model provenance (`modelId`, `version`, training dataset hashes, feature list) to satisfy audit and replay requirements.

## Recommendation outputs

- Generates advisory layers (`advisor.vrRecommendation.population`, `advisor.vrRecommendation.nitrogen`, etc.) with per-cell targets, confidence scores, and rationale snippets.
- Produces textual insights (e.g., "South slope N recommendation reduced 12% due to soil OM") for dashboards and reports.
- Suggests follow-up tasks (soil sampling, drainage review) that feed TaskService and Automation Engine triggers.

## UX expectations

- Advisor panel in the desktop UI lists recommendations per field/session with sparkline trends, confidence indicators, and quick actions (Apply as Prescription, Flag for Review, Dismiss).
- Operators can compare advisor layers against existing plans, visualize deltas, and promote accepted outputs into `vr.planned.*` layers after validation.
- Mobile companions receive digest summaries suitable for scouting or landlord conversations.

## Governance & safety

- Recommendations run inside sandboxed workers with deterministic random seeds and bounded CPU budgets to protect runtime performance.
- CI fixtures replay historical seasons to verify advisor outputs remain stable across model updates.
- Integrates with Regulatory exports to document AI involvement when prescriptions influence compliance-sensitive operations.
