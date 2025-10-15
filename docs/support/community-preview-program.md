# NX-096 Community preview program

The community preview program introduces an opt-in ring between internal testing and full
production releases. Preview builds surface candidate fixes to engaged operators, accelerating
feedback loops while bounding risk. Align enrolment with the
[installer/update channel guide](../howto/installer-update-channels.md) so preview machines
stay on the correct release ring when you stage media for offline fleets.

## Enrollment

- **Opt-in portal** — Dealers and advanced operators enrol through the dealer toolkit, selecting
  machines eligible for preview updates and agreeing to telemetry sharing for regression analysis.
  Fleet coordinators should also review the
  [multi-machine sync workflow](../howto/multi-machine-sync.md) to keep preview rigs aligned
  with the anchor manifest when swapping identities.
- **Eligibility checks** — Only hardware that meets baseline requirements (stable power, remote
  support connectivity, rollback support) is approved. The toolkit runs automated checks mirroring
  the dealer deployment scripts.
- **Channel assignment** — Approved machines are moved to the `preview` update channel, which
  receives builds after CI + simulation validation but before general release.

## Build distribution

1. Publish preview builds with release notes highlighting targeted fixes and known limitations.
2. Require participating machines to upload daily field feedback snapshots (NX-094) so anomalies
   are caught quickly. The [field feedback telemetry aggregator](field-feedback-telemetry.md)
   keeps those uploads consistent with the dealer escalation process.
3. Provide rollback instructions and one-click tooling via the deployment toolkit to revert to the
   last stable build if necessary.

## Feedback loop

- **Weekly survey** — Participants receive a short survey summarising changes and capturing
  qualitative impressions. Responses are linked to telemetry uploads for context and should
  note the latest sync archive timestamp recorded in the
  [multi-machine synchronization workflow](../howto/multi-machine-sync.md).
- **Preview review** — During the dealer escalation huddle, include a standing agenda item to
  review preview metrics, survey sentiment, and outstanding issues before promoting a build.
- **Exit criteria** — Builds graduate when telemetry shows no critical regressions for seven days
  and preview survey satisfaction stays above 85%. Releases failing these gates loop back into the
  backlog with documented fixes and communication plans.
