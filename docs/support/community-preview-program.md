# NX-096 Community preview program

The community preview program introduces an opt-in ring between internal testing and full
production releases. Preview builds surface candidate fixes to engaged operators, accelerating
feedback loops while bounding risk.

## Enrollment

- **Opt-in portal** — Dealers and advanced operators enrol through the dealer toolkit, selecting
  machines eligible for preview updates and agreeing to telemetry sharing for regression analysis.
- **Eligibility checks** — Only hardware that meets baseline requirements (stable power, remote
  support connectivity, rollback support) is approved. The toolkit runs automated checks mirroring
  the dealer deployment scripts.
- **Channel assignment** — Approved machines are moved to the `preview` update channel, which
  receives builds after CI + simulation validation but before general release.

## Build distribution

1. Publish preview builds with release notes highlighting targeted fixes and known limitations.
2. Require participating machines to upload daily field feedback snapshots (NX-094) so anomalies
   are caught quickly.
3. Provide rollback instructions and one-click tooling via the deployment toolkit to revert to the
   last stable build if necessary.

## Feedback loop

- **Weekly survey** — Participants receive a short survey summarising changes and capturing
  qualitative impressions. Responses are linked to telemetry uploads for context.
- **Preview review** — During the dealer escalation huddle, include a standing agenda item to
  review preview metrics, survey sentiment, and outstanding issues before promoting a build.
- **Exit criteria** — Builds graduate when telemetry shows no critical regressions for seven days
  and preview survey satisfaction stays above 85%. Releases failing these gates loop back into the
  backlog with documented fixes and communication plans.
