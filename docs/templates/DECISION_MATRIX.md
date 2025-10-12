# Decision matrix: <Section>/<Family ID>

## Context
- Section: [<Section name>](../SRS/sections/<file>.md)
- Family ID: DS-<id> (exclusive) / CS-<id> (composable)
- Prerequisites: Decisions that must land first (e.g., OS target).

## Options compared
- O-<id>
- O-<id>

## Criteria & weights
| Criterion | Weight | Rationale |
|---|---|---|
| Maintainability | 3 | e.g., Volunteer maintenance load |
| Offline resilience | 2 | ... |

## Scoring
| Option | Maintainability | Offline resilience | … | Weighted total | Notes |
|---|---|---|---|---|---|
| O-<id> | 3 | 1 | … | 14 | Key trade-offs |

> ⚠️ Keep this table neutral: use data, prototypes, or references. Final choices live in ADRs.

## Follow-up items
- Data we still need (benchmarks, licensing check, etc.).
- Dependencies to validate.
- Links to discussions.
