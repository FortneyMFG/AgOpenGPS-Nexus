# <Section Title> (Status: collecting proposals)

## Problem statement
1–3 sentences on what this slice must enable.

## Requirements (from contributors)
- R-<id> (MUST/SHOULD/COULD): <short requirement>
- ...

## Options
<!-- Each option links to its own OPTION.md file for details -->
- O-<id>: <option name> — 1-liner summary
- ...

### Option families & decision ordering
<!-- Capture whether options are mutually exclusive or composable and which families depend on earlier decisions. -->
| Family ID | Type | Options | Decides before | Notes |
|---|---|---|---|---|
| DS-01 | Exclusive | O-<id>, O-<id> | DS-02 | e.g., Pick one base OS target before UI framework. |
| CS-01 | Composable | O-<id>, O-<id> | — | e.g., Add-on capabilities we can layer together. |

## Comparison (quick matrix)
<!-- Keep a lightweight pros/cons table for early discussion. Deeper scoring lives in a dedicated decision matrix doc. -->
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-<id> | … | … | … | … |

## Decision matrices
<!-- Link to docs/templates/DECISION_MATRIX.md instances when a family needs deeper analysis. -->
- [ ] DS-01 decision matrix drafted
- [ ] DS-02 decision matrix drafted

## Evaluation criteria
What matters here (latency, maintainability, portability, UX, cost to migrate).

## Current sentiment
Bullet points summarizing discussion (no decision).

## Open questions
- Q-<id>: …
- ...
