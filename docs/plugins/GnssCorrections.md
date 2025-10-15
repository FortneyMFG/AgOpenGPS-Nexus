# GNSS Correction Service Plugin (Planned)

The GNSS Correction plugin manages NTRIP, PPP, and base-station connections to deliver high-accuracy positioning to Nexus deployments.

## Capabilities

- Configures multiple correction sources (public CORS, private NTRIP, local base) with credential storage and automatic reconnection.
- Logs correction streams for post-processing and yield map QA, storing metadata (mountpoint, latency, age of differential) alongside session provenance.
- Provides fallback strategies (auto-switch to secondary mountpoint) and alarms when corrections degrade beyond configured thresholds.

## UX & operations

- UI surfaces connection status, latency, correction age, and baseline information with color-coded indicators.
- Supports QR code import/export for correction profiles to simplify contractor setup.
- Exposes diagnostics (RTCM message counts, packet loss, auth failures) through Telemetry & Health dashboards.

## Integration

- Supplies correction quality metrics to Equipment Health and Automation Engine for rule evaluation.
- Shares correction logs with Replay/Telemetry for deterministic reprocessing and compliance documentation.
- Coordinates with Device Manager to pause autosteer or alert operators when corrections fall back to autonomous accuracy.
