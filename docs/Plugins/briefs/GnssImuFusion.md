# GNSS + IMU Fusion Plugin

The GNSS + IMU Fusion plugin publishes a stabilized vehicle pose stream by blending RTK GNSS fixes, IMU rates, and wheel speed inputs. It exposes fused pose and inertial topics to Core consumers while tracking quality metrics so guidance, mapping, and replay services can reason about navigation confidence.

## Responsibilities
- Run the position source aggregator to prioritise hardwired receivers but gracefully fall back to network feeds when hardware faults occur, ensuring a continuous pose stream for dependent services.【F:Nexus SourceCode/src/Aog.Agio/Gnss/PositionSourceAggregator.cs†L33-L135】
- Emit fused pose samples, inertial telemetry, and associated confidence scores through the manifest-declared transports so Core and the UI shell can hydrate panels and overlays.【F:docs/development/SRS/appendices/samples/plugins/gnss-imu-fusion/1.0.0.json†L1-L46】
- Acquire exclusive leases for navigation pose publication and share IMU/quality telemetry so arbitration logic can keep other plugins informed without interrupting guidance flows.【F:docs/development/SRS/appendices/samples/plugins/gnss-imu-fusion/1.0.0.json†L68-L86】

## Dependencies
- **Hard:** Core runtime for event publication, AgIO sensor backplanes, and the NTRIP client that keeps RTCM corrections flowing into the fusion stack.【F:docs/Plugins/nexus-plugin-dependency-map.md†L792-L813】
- **Soft:** Mapping overlays, Device Manager health feeds, and Telemetry Logging for replay capture. The plugin reports degraded state rather than failing outright when these services are absent.【F:docs/Plugins/nexus-plugin-dependency-map.md†L814-L830】

## Configuration
The manifest surfaces operator-tunable settings for quality windows, IMU bias limits, wheel speed weighting, and RTCM hold times so deployments can balance responsiveness and noise resilience.【F:docs/development/SRS/appendices/samples/plugins/gnss-imu-fusion/1.0.0.json†L13-L32】 Operators can also enable bundled simulation providers that synthesise fused pose and IMU topics at deterministic rates for lab validation and regression suites.【F:docs/development/SRS/appendices/samples/plugins/gnss-imu-fusion/1.0.0.json†L33-L86】

## Operational Notes
- When all preferred hardware feeds fault, the aggregator backs off before scanning again, allowing sensors to recover without overwhelming the event bus.【F:Nexus SourceCode/src/Aog.Agio/Gnss/PositionSourceAggregator.cs†L87-L134】
- Quality telemetry remains available even when navigation pose publication is revoked, enabling downstream services to warn operators about degraded localization while preserving safe fallbacks.【F:docs/development/SRS/appendices/samples/plugins/gnss-imu-fusion/1.0.0.json†L68-L86】
