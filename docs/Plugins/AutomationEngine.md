# Rules & Automation Engine Plugin (Planned)

The Automation Engine lets operators define when/then rules that react to telemetry, layer changes, or task events without writing code.

## Rule model

- Rules contain triggers (coverage stopped, yield below threshold, GNSS age exceeded), optional conditions, and actions (sound buzzer, raise alert, create task, toggle rate mode).
- Stored as declarative JSON documents with versioned schemas, capability flags, and safety classifications (monitor-only vs. control).
- Supports variable substitution (e.g., field-specific thresholds) and schedules (active only during certain hours or jobs).

## Runtime behavior

- Runs inside the Core sandbox with deterministic evaluation windows aligned to SimClock to keep replay results stable.
- Provides rule evaluation telemetry (ruleId, trigger state, action outcome) for dashboards and audits.
- Enforces safety gating—actions requiring control capabilities check permissions and hardware state before execution.

## UX expectations

- Visual rule builder with trigger/action pickers, preview simulator, and validation warnings when dependencies are missing.
- Rule library with templates (idle alert, drainage flag, low yield advisory) that operators can clone and adjust.
- In-session notifications show when rules fire, with quick links to acknowledge, snooze, or open related tasks.

## Integration

- Hooks into TaskService to create follow-up tasks when automation detects anomalies (e.g., yield < target).
- Works with Telemetry Logging and Replay so rules can be tested against recorded sessions before deployment.
- Shares rule manifests with Plugin Catalog for distribution and with Regulatory plugin to document automation involved in compliance workflows.
