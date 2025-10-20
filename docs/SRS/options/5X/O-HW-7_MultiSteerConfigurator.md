# O-HW-7 — Multi-steer equipment configurator primitives

## Problem
Guidance, rate, and section controllers currently assume a single steering axle or a
fixed hitch geometry. Rigs that articulate mid-frame or mix multiple steerable axles
require richer profiles so the kinematics solver knows how each joint moves and where
attachments mount. Operators also need a guided way to describe those joints without
editing JSON by hand.

## Goals
- Provide a repeatable configuration workflow that captures articulation joints,
  steerable axles, and hitch couplers in one topology graph.
- Support common steering modes (front, rear, coordinated, opposite, independent)
  plus center-pivot articulation, with optional combinations.
- Let hitches anchor to axle centers by default while permitting offset pivots or
  controlled joints for specialized implements.
- Produce machine profiles that [ADR-017](../../ADR/ADR-017-profiles-kinematics.md)
  kinematics can consume in simulation and
  runtime planners.
- Surface presets and illustrations so operators can pick a starting template
  before customizing lengths, sensor offsets, and control bindings.

## Non-goals
- Implementing the runtime kinematics math (covered by
  [ADR-017](../../ADR/ADR-017-profiles-kinematics.md) deliverables).
- Replacing existing axle presets for conventional tractors (they remain available).
- Modeling flexible frames or suspension deflection beyond rigid-body joints.

## Proposed approach

### Frames, units, and timebase contract
- Profiles declare a canonical map frame (EPSG code + origin), right-handed body frames
  (`x` forward, `y` left, `z` up), and explicit unit sets (meters, degrees by default) so
  [ADR-017](../../ADR/ADR-017-profiles-kinematics.md), planners, and visualizers
  consume a consistent basis without guesswork.
- Every signal publishes a monotonic timestamp tied to the profile `timebase`
  declaration (GPS time, steady monotonic, or PTP) with an allowed `maxSkewMs`; per-signal
  latency and variance metadata travel with attachment definitions.
- Exporters stamp each profile with `schemaVersion`, `profileId`, and a content hash
  computed as `SHA256(canonicalJson(profile \ calibrationBundle))` to keep regression
  diffs auditable and support “send me your profile” support workflows while ignoring
  operator-specific calibration bundles.

### 1. Machine skeleton selection
- Present starter skeletons: single-frame (no articulation), center articulation,
  tandem articulation (tractor + steered implement), and implement-only articulation.
- Each skeleton lists mandatory nodes (tractor body, articulation joint, implement
  body) plus optional nodes (extra axles, sensors, hitch receivers).
- Selecting a skeleton seeds default dimensions and joint types that the operator
  can refine.

### 2. Steering module catalog
| Module | Description | Compatible nodes |
| --- | --- | --- |
| `AxleSteer.Front` | Traditional front axle steering with tie-rod constraint. | Tractor/front axle |
| `AxleSteer.Rear` | Rear axle steering locked to operator input. | Tractor rear axle |
| `AxleSteer.Coordinated` | Front and rear axles steer in same direction with ratio parameter. | Paired axles |
| `AxleSteer.Counter` | Rear axle steers opposite front axle with ratio parameter. | Paired axles |
| `AxleSteer.Independent` | Front and rear axles receive separate commands (requires dual controllers). | Paired axles |
| `AxleSteer.PassiveCaster` | Free-caster implement axle follows hitch dynamics; optional caster angle sensor input. | Implement caster axle |
| `AxleSteer.HitchFollower` | Implement axle derives steer target from hitch yaw rate or follower sensor input. | Implement steer axle |
| `AxleSteer.Crab` | Bias steer target to hold a lateral offset between heading and velocity vectors (hillside/controlled-traffic use). | Tractor or implement steer axle |
| `TrackSteer.Differential` | Differential torque/velocity steering for two-track or quad-track drives. | Root body with tracked drive |
| `JointSteer.Articulation` | Central pivot angle forms primary steering input. | Articulation joint |
| `JointSteer.Implement` | Implement articulation joint follows its own controller or hitch follower. | Implement articulation |
| `JointSteer.Lateral` | Side-shift prismatic actuator to bias implement laterally. | Hitch or toolbar joint |

- Modules expose parameters: controller topic (autosteer, hitch steer), steering
  ratios, limits, damping, and `authorityPolicy` (auto, operator, mixed) with explicit
  preemption rules plus safe-neutral poses for loss-of-command handling.
- Coordinated/counter steering modules reference a leader axle and follower ratio to
  satisfy [ADR-017](../../ADR/ADR-017-profiles-kinematics.md) linkage expectations.
- Track steering adds parameters for track separation, skid factor, and curvature lag
  so the solver can convert angular commands into differential torque setpoints.
- Crab steering exposes a velocity-heading bias target and optional slope sensor input
  so hillside modes stay within implement overlap tolerances.
- Passive caster and hitch-follower modules advertise whether they use inferred angles
  or explicit sensors, guiding sensor validation requirements.
- Every steering module declares backlash, deadband, max rate, and latency budgets so
  planners respect hardware limits and [ADR-017](../../ADR/ADR-017-profiles-kinematics.md)
  can simulate lag/overshoot.
- Axle/track nodes expose minimum turn radius, tire/track width, and Ackermann scrub
  limits so planners avoid impossible headland turns and plan smarter reverse entries.
- Front-axle steering calibration includes an Ackermann helper that converts linkage
  angle (pre-Ackermann) measurements into per-wheel targets by collecting ball-joint to
  kingpin offsets and the nominal tire contact patch offset outside the pivot; the GUI
  can solve the geometry and emit the delta between linkage angle and road-wheel angle
  that runtime modules expect.

### 3. Hitch coupler definitions
- Default rule: hitch anchor projects to the midpoint of the attached axle’s track.
- Operator can override with explicit longitudinal/lateral offsets relative to the
  axle or body frame, enabling tongue pivots ahead of an axle or drawbars behind the
  tractor.
- Each coupler specifies its degrees of freedom (fixed, passive pivot, operator
  controlled) across yaw, pitch, and roll hinges, plus optional float detents and
  lift/transport locks so toolbar raise/fold geometry stays explicit.
- Implement hitches can chain: tractor drawbar → implement articulation → toolbar
  axles, each emitting geometry for lookahead calculations.
- Train-of-implements support allows N serial hitches so carts, toolbars, and steer
  axles form a single rooted tree. Each joint advertises its own actuator/sensor set
  so dependency graphs and planner hints remain explicit.
- Couplers describe drive direction policy (`front`, `rear`, `neutral`) so planners
  know which body faces the direction of travel when push configurations engage.

### 4. Link graph editor
- Visual editor lists nodes (bodies, axles, joints) and edges (hinges, steering
  actuators, hitches).
- Graph validation ensures there is a single kinematic tree rooted at an authoritative
  body (tractor, swarm bot, or push unit) and flags cycles or unsupported chains.
- Exported profile serializes nodes with their transforms, joint types, steering
  modules, and couplers so [ADR-017](../../ADR/ADR-017-profiles-kinematics.md) ingestion
  builds the same graph.
- Desktop shell now ships the `KinematicsProfileEditorViewModel`, allowing operators to
  assemble axle nodes, joints, and mode policies inline while streaming
  ingestion diagnostics and canonical JSON/hashes back to the configurator.
- Dependency annotations capture multi-point steering chains (e.g., tractor → hitch
  → steer cart → toolbar axle) so planners understand command precedence.

### 4a. Ingestion API & validation contract
- Core exposes `POST /core/ingest` with body `{profileJson, deterministic?, seed?}`
  so configurator exports hydrate the runtime through a single entry point.
- Callers may pass `{deterministic: true, seed: <u64>}` to lock pseudo-random choices
  (noise, slip priors) for CI fixtures and regression replay; omitting the flag keeps
  normal stochastic behaviors.
- Profiles must declare `meta.compat.guard` entries (e.g., `"ADR-017>=0.7"`,
  `"Core>=1.12"`). Ingestion rejects mismatches and returns structured diagnostics.
- Timebase declarations export `timebase.maxSkewMs` and
  `lateMeasurementPolicy ∈ {drop, rewindFixedLag}`; the loader surfaces both fields to
  [ADR-017](../../ADR/ADR-017-profiles-kinematics.md) so estimators align buffers and
  deal with out-of-sequence measurements.
- Successful requests respond with `{profileId, contentHash, caps, topics}`. Validation
  failures return HTTP 422 with payload `{code, path[], message, hint}` to drive
  actionable UI fixes.
- Import → export must preserve `contentHash`; any change that modifies kinematics or
  sensors requires bumping `schemaVersion` or `profileId` before Core will accept the
  update.

### 4b. Validation error taxonomy
| Code | Severity | Description |
| --- | --- | --- |
| `KIN-001` | Error | `RootMissing` — profile lacks an axle with `role: root`. |
| `KIN-002` | Error | `CycleDetected` — graph contains a cycle or multiple roots. |
| `KIN-010` | Error | `SensorMissing(role=articulation_angle, module=JointSteer.Articulation)` |
| `KIN-012` | Warn | `TimebaseSkewExceeded(maxSkewMs=10, observed=27)` — simulation-only |
| `KIN-020` | Error | `AckermannLUTNonMonotonic(side=left, idx=14)` |
| `KIN-030` | Error | `ModeInterlockUnsatisfied(interlock=speed>40, module=rear_steer)` |

- Severity `Error` blocks automation enablement, `Warn` limits the profile to
  “simulate only,” and forthcoming `Info` codes log without blocking.

### 5. Sensor & attachment catalog
- Promote sensors to first-class attachments with transforms anchored to nodes or
  joints: `Sensor.IMU`, `Sensor.Angle`, `Sensor.GNSS`, `Sensor.Camera`,
  `Sensor.WheelEncoder`, and `Sensor.Slip`.
- Each attachment supports multiplicity with explicit `mount.nodeId` _or_ `mount.jointId`,
  `mount.pose` (x, y, z, roll, pitch, yaw), semantic `role` (e.g., `primary_body_attitude`,
  `articulation_attitude`, `dual_antenna_left/right`, `implement_pose`), `rateHz`,
  `latencyClass` (`fast`, `medium`, `slow`), `trustWeight`, variance hints, and
  `producerPlugin` metadata.
- Redundancy policies allow multiple sensors per role; operators configure majority or
  weighted consensus plus automatic failover rules when a source drops out. Attachments
  capture per-source `latencyMs`, drift models (bias rate, temperature coefficients), and
  recalibration policies (time, temperature, service hours) for UI nagging and solver
  tuning.
- Wheel encoders and slip sensors mount per axle/track to feed odometry and skid
  estimation. Track modules accept time-varying slip/skid estimates and expose them to
  Core planners.
- Sensor presence gates module eligibility (e.g., articulated autosteer requires an
  articulation angle sensor or equivalent estimator) and exposes calibration checklists
  inside the UI.

### 6. Baselines & dual-antenna heading
- Support GNSS baselines referencing two antenna attachments with measured separation,
  including `resolvesHeading` flags and baseline length for solver weighting.
- Allow optional implement-mounted antennas to measure hitch articulation directly when
  dual-antenna packages span tractor and implement bodies.
- Store baseline metadata alongside sensor roles so
  [ADR-017](../../ADR/ADR-017-profiles-kinematics.md) fuses heading sources without
  bespoke configuration.

### 7. Presets & assets
- Bundle starter presets: articulated 4WD tractor, articulated 4WD + steerable rear
  hitch, bi-directional loader, self-propelled sprayer with coordinated rear steer,
  strip-till toolbar with steering carts, and quad-track tractor with differential
  steering.
- Each preset references SVG illustrations showing node layout, steering arrows,
  hitch motion, and sensor placements so operators understand segment attachments.
- Presets live alongside generic axle packages so power users can assemble custom
  rigs from raw modules.
- Legacy "conventional tractor" presets map to the axle-centric model (rear root,
  fixed drawbar, front steer axle) so existing operators feel at home while gaining
  the richer metadata surface. Presets carry `hardwareHints` (e.g., "requires
  articulation WAS", "dual GNSS recommended") to set calibration expectations
  up front.

### 8. Mode profiles, interlocks & runtime toggles
- Allow operators to define named mode profiles (e.g., road vs. field) that toggle
  steering modules, rate limits, and hitch behaviors without rebuilding the graph.
- Profiles serialize enabled module lists and parameter overrides so
  [ADR-017](../../ADR/ADR-017-profiles-kinematics.md) can swap constraint sets at
  runtime when automation or the operator requests a mode change.
- Mode profiles declare interlocks (speed, implement state, operator acknowledgement)
  and `modeTransitionGuard` policies so automation can reject unsafe transitions.
- Overrides include reversible diffs (e.g., relaxed hitch limits in field mode) and
  can lock steer modules automatically above configured road speeds. Profiles also
  nominate fail-safe fallbacks that Core can drop into on authority loss (e.g., lock
  rear steer, clamp hitch angles).

### 9. Transport & width management
- Joint definitions flag transport locks (folded booms, toolbar lifts) and map to mode
  overrides so section width and coverage math stay valid during road moves.
- Linear joints and wing actuators emit effective width telemetry so section control
  and planners account for telescoping booms or wing tilt.

### 10. Calibration & verification workflow
- Embed guided calibration flows per sensor role: tape-measure seed entry, zeroing,
  figure-eight runs for yaw biases, hill rolls for pitch/roll, and caster trail rollouts.
- Record `calibrationStamp`, residuals, and error budgets per attachment so Core can
  surface stale or suspect calibrations.
- Persist calibration bundles with operator identity, run notes, and profile content
  hash to support regression triage.
- Provide hitch zeroing and caster trail estimation helpers tied to corresponding
  sensor roles to close the loop between mechanical setup and software models.
- Steering calibration wizard measures linkage (pre-Ackermann) motion for steer axles
  and collects wheelbase-to-kingpin and kingpin-to-tire offsets so the configurator can
  compute the Ackermann correction factor automatically instead of asking operators to
  hand-tune per-wheel angles.
- After calibration, run an automatic graph validation comparing predicted vs.
  measured implement pose; flag operators when closure error exceeds tolerance.
- Honor per-attachment drift models and `recalPolicy` triggers so the UI can mark
  sensors overdue for re-zero when time, temperature, or operating hours exceed limits.

### 11. Operator UX touches
- Graph view adds an “Attachments” lane where operators drag sensor cards onto nodes or
  joints; the UI displays mount frames, lever-arm vectors, and current calibration age.
- Per-role status bars show the authoritative sensor, standby peers, residuals, and
  failover state so redundancy is understandable at a glance.
- Calibration checklist highlights unresolved roles (e.g., missing articulation IMU)
  and links directly to the appropriate guided workflow.
- Timebase indicators surface the authoritative clock source and warn when skew exceeds
  the configured tolerance so consensus voters retain confidence.
- Wizard flow walks operators through skeleton selection → axle/drawbar confirmation →
  steering module assignment → Ackermann wizard capture → sensor roles → mode profiles
  → calibration drives → quick simulation preview, with inline diagrams highlighting
  kingpin, tie-rod, and tire contact offsets to demystify Ackermann inputs.

### 12. Telemetry & health surfaces
- Publish `/machine/health` as a fixed schema so operators and support staff can verify
  redundancy behavior in real time:
  ```json
  {
    "mode": "field",
    "votes": {"heading": "dual_gnss"},
    "dropouts": ["wheel_angle_left"],
    "latencyMs": {"estimator": 58, "autosteer": 42},
    "calAges": {"imu_roof": "12d"},
    "slip": {"rear": {"sLat": 0.12, "conf": 0.7}}
  }
  ```
- Emit `/planner/limits` summarizing curvature guardrails and drive direction so guidance
  and headland planners share the same motion envelope:
  ```json
  {
    "kappaMax": 0.19,
    "minTurnApertureM": 13.6,
    "crabFeasible": true,
    "scrubWarn": false,
    "driveDirection": "front"
  }
  ```
- Provide `/estimator/debug` with residual RMS per attachment role (position, heading,
  yaw rate, articulation angle, axle steer) to accelerate calibration triage.
- Surface `/calibration/status` enumerating unresolved roles, drift-triggered rechecks,
  and next recommended actions.
- Linear joints publish effective width telemetry; transport locks push updates within
  100 ms so section planners adjust coverage immediately during fold/raise transitions.

### 13. Validation fixtures
- **Ackermann wizard loopback:** ship a CSV sample (`rack_angle_deg, δL_deg, δR_deg`)
  through the geometry solver and confirm recomputed rack angles stay within 0.2° RMS
  across the operating range.
- **Articulation zero:** capture a straight-line run after zeroing; articulated joint
  bias must remain within ±0.2°.
- **Slip sanity:** on flat gravel the lateral slip prior should average 0 ± 0.02 m/s;
  on an 8 % sidehill at 9 kph the sign must align with slope direction and magnitude
  should land between 0.08 m/s and 0.16 m/s while `κ_max` drops ≥15 %.
- **Mode flip:** toggle road → field at 25 kph; verify rear-steer transients stay under
  1° and telemetry topics update within 150 ms.
- **Transport locks:** fold booms or raise toolbars; effective width telemetry and
  planner limits must update within 100 ms.

### 14. Minimal runnable (MVP) profile
- Skeleton coverage: single-frame tractor, center articulation tractor, and tractor +
  steerable implement train presets ready for export.
- Steering catalog: `AxleSteer.Front`, `JointSteer.Articulation`, and optional
  `AxleSteer.Crab` toggle.
- Sensor set: single + dual GNSS, roof IMU, articulation WAS, axle-linkage WAS (feeding
  the Ackermann wizard), and wheel encoders.
- Hitch set: `Fixed` and `HingeYaw` joints with offsets plus `driveDirectionPolicy`.
- Mode profiles: `road`, `field`, and `fail_safe` with interlocks and fallback semantics.
- Runtime hand-off: axle-graph FK, per-axle `s_ℓ`/`s_y`, and per-wheel slip-lite.
- Post-MVP stretch goals: track differential steering, lateral prismatic joints,
  multi-trailer chains, and pitch float joints.

### 15. Definition of done
- **Graph validity:** exported rigs must form a single rooted tree with no cycles; every
  enabled steering module declares required sensors or estimators before activation.
- **Pose accuracy:** ≤ 5 cm RMS toolpoint cross-track error at 8–12 kph on flat ground
  with crab disabled; ≤ 10 cm RMS on an 8 % sidehill.
- **Mode transitions:** road ↔ field toggles settle in < 150 ms with < 1° transient on
  rear steer or hitch joints.
- **Latency budget:** command-to-estimated joint motion loop ≤ 120 ms at 95th percentile.
- **Persistence:** profile export→import round-trips byte-for-byte identical aside from
  regenerated `contentHash` and `calibrationStamp` fields.

### 16. Schema polish & compatibility
- Enumerate `units.linear ∈ {meter}`, `latencyClass ∈ {fast, medium, slow}`, and
  `authorityPolicy ∈ {auto, operator, mixed}` to keep validation deterministic.
- Add `profile.kind: multiSteer` and compatibility guards such as
  `compat.guard: ["ADR-017>=0.7"]` for downstream consumers.
- Steerable axles carry a `steerGeometry` block documenting whether wheel angles come
  from geometry solves or empirical lookup tables (`{mode: geometry|empirical, lutId}`)
  plus the sensor inputs that feed those solves.
- Sensor attachments include `driftModel` and `recalPolicy` enumerations; drift triggers
  drive UI reminders and `/calibration/status` updates.
- Health exports bundle `profileId`, `schemaVersion`, and `contentHash` for audit trails
  and regression diffs.

## Axle-centric runtime plan ([ADR-017](../../ADR/ADR-017-profiles-kinematics.md) hand-off)
The multi-steer configurator exports data directly into an axle-anchored runtime graph so
[ADR-017](../../ADR/ADR-017-profiles-kinematics.md) can model rigs without bespoke adapters. The runtime plan below mirrors the
schema the configurator emits and keeps math lightweight while honoring drawbar geometry
and tire slip:

1. **Core primitives**
   - *Axle*: primary reference frame for each vehicle or implement segment. Each axle
     carries pose in the parent frame `T_parent^Axle = (x, y, z, roll, pitch, yaw)` and
     parameters such as track width, wheelbase-to-next-axle, steering DOF, and tandem
     grouping identifiers.
   - *Drawbar*: rigid link between axles with anchor offsets, length vector `\vec{L}` in
     the parent axle frame, and joint type (`Fixed`, `HingeYaw`, `HingePitch`,
     `Prismatic`) including limits, float/lock state, backlash, and rate caps.
   - *Wheel*: child of an axle with mount pose (left/right offsets) and steer angle `δ`
     derived from the axle steer DOF or Ackermann mapping. Tire parameters include width
     and cornering stiffness per normal load so slip estimators stay consistent.
   - *Group/Tandem*: optional reduction that collapses parallel axles into a single
     equivalent axle with effective center of gravity, track width, and stiffness when
     operators prefer simplified models.

2. **Graph rules**
   - Exactly one root axle anchors the graph (rear axle, tracked module, or lead robot).
   - All other axles attach via drawbar chains; a conventional tractor front axle is a
     fixed drawbar link from the root, while articulated tractors swap the fixed joint
     for a `HingeYaw` at the pivot.
   - Implement trains simply continue the chain: tractor axle → drawbar → cart axle →
     drawbar → toolbar axle. Validation keeps the graph acyclic and rooted.

3. **2.5D state & motion**
   - Runtime keeps the estimator minimal: root pose `(x, y, ψ)`, forward speed `v`, yaw
     rate `ψ̇`, active hinge/prismatic joint states, and per-axle slip states `s_ℓ`
     (longitudinal) and `s_y` (lateral).
   - Root motion honors slip with
     `ṽ = v (1 - s_ℓ)`, `ẋ = ṽ cos ψ - s_y sin ψ`, `ẏ = ṽ sin ψ + s_y cos ψ`, and
     `ψ̇ = κ(δ_axles, q_art, tracks) · ṽ`.
   - Curvature sources include bicycle approximations `κ ≈ tan(δ)/L` for steer axles,
     articulation gains `κ ≈ γ · q_art`, and track differentials
     `κ ≈ (ω_R - ω_L) / (b · k_skid)`.

4. **Forward kinematics (axle reference)**
   - Traverse drawbars from the root, applying joint transforms then translating by
     `\vec{L}` to locate child axles.
   - Wheel poses resolve from axle transforms and steering angles, respecting anchor
     offsets away from axle centers.

5. **Steering distribution**
   - Each steerable axle exposes a single `δ_axle` command. Wheel steering angles follow
     Ackermann equations
     `δ_L = arctan(L / (R - t/2))`, `δ_R = arctan(L / (R + t/2))` with `R = 1/κ` and
     track width `t`. When the runtime cannot resolve `R` directly it applies a
     first-order Ackermann correction from the commanded `δ_axle`.

6. **Per-wheel slip (lightweight)**
   - Compute each axle planar twist `(v_x, v_y, ψ̇)` from root motion and forward
     kinematics, then transform wheel contact velocities `(u_i, v_i)`.
   - Slip angle `α_i = atan2(v_i, |u_i| + ε) · sgn(u_i)` feeds a clipped lateral force
     `F_{y,i} = sat(C_α(N_i) α_i, ± μ_y N_i)` using quasi-static normal load splits. Sum
     wheel forces per axle to bound curvature and bias slip priors `s_y`.

7. **Hitch forces & torques**
   - Drawbar joints ingest measured draft/downforce when available; otherwise inferred
     lateral forces maintain implement path adherence. Equal and opposite reactions flow
     back into axle load splits.

8. **Selective pitch/roll modeling**
   - Remain planar globally but support `HingePitch` joints and roll-derived load
     transfer from IMU gravity vectors so sidehill behavior and toolbar float remain
     faithful.

9. **Worked examples**
   - Conventional tractor: rear axle (root) → fixed drawbar (`\vec{L} = [wheelbase, 0, 0]`)
     → front steer axle with Ackermann wheels.
   - Articulated tractor: rear axle → zero-length drawbar with `HingeYaw` articulation →
     fixed drawbar to front axle; articulation angle is the primary steering input.
   - Implement train: continue chaining axles and drawbars for carts, toolbars, and
     steer axles. Hitch follower or caster modules consume the same joints.

10. **Schema deltas captured in configurator**
    - New `axles` collection stores role (`root`, `steer`, `implement`), track, steering
      metadata, Ackermann hints, and min turn radii.
    - `drawbars` record parent/child axle IDs, anchor offsets, joint types, and length
      vectors.
    - `wheels` mount on axles with tire properties and optional encoder/slip sensors.
    - `slipModel` section toggles per-axle longitudinal/lateral slip states and per-wheel
      estimators.

11. **Runtime loop summary**
    - Predict root pose with slip-aware unicycle equations, propagate joints via forward
      kinematics, compute wheel slip/forces, enforce planner guardrails (`κ_max`,
      headland aperture, crab feasibility), fuse sensors (GNSS, IMU, joint angles) with
      slip priors, publish toolpoint poses plus health telemetry, and compute
      per-axle capacity so planners honor the advertised `κ_max` each cycle.

The configurator ships these structures so
[ADR-017](../../ADR/ADR-017-profiles-kinematics.md) can hydrate runtime types (e.g.,
`AxleGraph`, `DrawbarFk`, `PerWheelSlipLite`) without further schema translation. Future
runtime modules may live in Core, but the exported JSON drives both simulation and
hardware controllers immediately.

## Integration hooks
- Profiles export into the same store used by NX-056 machine profile translators,
  tagging them as `multiSteer` for compatibility guards.
- Export includes schema version, timebase, and content hash metadata so downstream
  services can confirm compatibility and detect drift during support handoffs.
- Simulation provider uses the serialized graph to spawn hinge joints and controllers
  in the composite fabric, satisfying [ADR-017](../../ADR/ADR-017-profiles-kinematics.md)
  accuracy targets.
- Autosteer and section planners query the profile registry for steering degrees of
  freedom, enabling lookahead policies to account for joint lag and hitch offsets.
- Exporters also emit a joint summary (degree-of-freedom counts, primary steering
  inputs, maximum curvature) for high-level planners that do not need full geometry
  but must understand motion limits. Summaries include headland hints (supported turn
  patterns, minimum apertures) and scrub limits so planners skip infeasible maneuvers.
- Linear joints (`Joint.Linear`) capture telescoping booms or toolbar offsets so
  section control overlap calculations respect reach changes even when no steering
  occurs.
- Capability summaries emit degrees of freedom, primary steering inputs, backlash,
  latencies, max curvature, and scrub limits so planners, teleop clients, and diagnostics
  can reason about automation limits.
- Health and telemetry topics (`/machine/health`, `/planner/limits`, `/estimator/debug`,
  `/calibration/status`) publish sensor votes, slip priors, latency, calibration ages,
  and motion envelopes so Core, planners, and operator UIs stay synchronized.
- Profiles ship `driveDirectionPolicy`, authority metadata, and safe-neutral poses so
  Core arbitration aligns control leases, e-stop compatibility, and fallback behavior.

### Legacy map appendix
- Include a `legacy_id` per preset so support can correlate historical tickets with new
  axle-centric exports.
- Provide migration notes for common rigs (e.g., “Conventional tractor → rear root
  axle, fixed drawbar to front steer axle; requires roof IMU, front linkage WAS;
  optional dual GNSS”) to accelerate dealer onboarding.

### Security & operations
- Profiles may optionally carry `signature: {alg, kid, value}` blocks so OEMs and
  dealers can ship authenticated presets; ingestion verifies signatures when policies
  require it.
- `calibrationBundle` payloads omit personally identifiable information by default;
  operators explicitly opt-in before attaching names or notes for support cases.

### Component boundaries
- Keep the articulated kinematics solver within AOG Core so guidance, section control,
  and state estimation consume a single authoritative motion model without cross-plugin
  latency or version drift.
- Plugins remain responsible for sourcing sensor inputs and actuating steering modules;
  they surface joint angles and control topics that the core solver consumes.
- Future kinematics extensions (e.g., suspension flex) can graduate to optional Core
  feature flags, but the baseline articulated solver ships with the platform to keep
  regression coverage in lockstep with navigation updates.
- Solver accommodates per-joint backlash/deadband definitions rather than pushing
  nonlinearities into plugins, keeping a single authoritative motion model.

## Open questions
1. What UX affordances best communicate multiple simultaneous steering modes without
   overwhelming the operator (e.g., stacked cards, stepper wizard, or graph view)?
2. What redundancy policy (median, Mahalanobis, heuristic) best balances responsiveness
   and robustness when multiple sensors share a role?
3. Should backlash modeling remain parametric per joint, or should
   [ADR-017](../../ADR/ADR-017-profiles-kinematics.md) expose a richer non-linear solver
   interface for hardware-specific hysteresis curves?
4. How aggressively should mode profiles auto-switch (speed, geofence) versus requiring
   operator confirmation to avoid unexpected behavior mid-field?
5. What minimum sensor/capability set should be enforced before enabling articulated
   autosteer, crab biasing, or track differential modes on production rigs?
6. How should redundancy voters balance latency, variance, and drift when redundant
   sensors disagree but remain within tolerance windows?
7. What conditions should trigger automatic fall-back into fail-safe mode profiles, and
   when must the operator confirm the downgrade?
8. Should transport locks automatically update planner-effective widths, or should the
   operator confirm geometry changes to avoid surprises mid-field?
9. Should the Ackermann wizard publish a formal LUT/geometry spec (separate option note)
   or fold into O-HW-7 as a normative appendix?
10. What automated checks verify the definition-of-done targets (latency, RMS error)
    inside CI versus requiring field trials?

## Schema sketch
```yaml
meta:
  schemaVersion: 0.7
  profileId: MF-8245R-StripTill-2025-10-15
  kind: multiSteer
  compat: {guard: ["ADR-017>=0.7"]}
  mapFrame: {epsg: 3857, origin: {x: 0, y: 0, z: 0}}
  units: {linear: meter, angle: degree}
  timebase: {type: gpsTime, maxSkewMs: 10}
  contentHash: 34c2cdd1

safety:
  eStopCompatible: true
  safeNeutral:
    - {jointId: hitch, pose: {yawDeg: 0}}
  authority:
    - {moduleId: front_axle_cmd, policy: mixed, operatorPreempt: true}

nodes:
  - id: tractor_body
    type: Body
    pose: {x: 0, y: 0, z: 0, r: 0, p: 0, yw: 0}
  - id: front_axle
    type: Axle
    parent: tractor_body
    joint:
      type: Fixed
      pose: {x: 2.8, y: 0, z: 0}
    dynamics:
      minTurnRadiusM: 6.5
      tireWidthM: 0.55
      ackermann:
        maxErrorDeg: 2.0
        wheelbaseM: 2.85
        trackM: 2.1
    steerGeometry:
      mode: geometry
      solverId: ackermann_wizard_v1
      inputs: [hitch_angle, linkage_was_left, linkage_was_right]
  - id: hitch
    type: Joint
    parent: tractor_body
    child: implement_body
    joint:
      type: HingeYaw
      limits: {minDeg: -45, maxDeg: 45}
      dynamics: {deadbandDeg: 0.5, maxRateDps: 40, latencyMs: 60}
  - id: toolbar_float
    type: Joint
    parent: implement_body
    child: toolbar
    joint:
      type: HingePitch
      limits: {minDeg: -5, maxDeg: 15}
      float: true

attachments:
  - id: imu_roof
    type: Sensor.IMU
    mount: {nodeId: tractor_body, pose: {x: 0.0, y: 0.0, z: 2.7, r: 0, p: 0, yw: 0}}
    role: primary_body_attitude
    rateHz: 200
    latencyClass: fast
    latencyMs: 5
    trustWeight: 1.0
    variance: {yawDeg2: 0.02, pitchDeg2: 0.01, rollDeg2: 0.01}
    driftModel: {biasDegPerHr: 0.1, tempCoeffDegPerC: 0.002}
    recalPolicy: {maxHours: 200, maxTempDeltaC: 20}
    producerPlugin: imu.canbus
  - id: gnss_pair_left
    type: Sensor.GNSS
    mount: {nodeId: tractor_body, pose: {x: -0.5, y: -0.8, z: 2.9}}
    role: dual_antenna_left
    rateHz: 20
    latencyClass: medium
    latencyMs: 45
  - id: gnss_pair_right
    type: Sensor.GNSS
    mount: {nodeId: tractor_body, pose: {x: 0.5, y: 0.8, z: 2.9}}
    role: dual_antenna_right
    rateHz: 20
    latencyClass: medium
    latencyMs: 45
  - id: hitch_angle
    type: Sensor.Angle
    mount: {jointId: hitch}
    role: articulation_attitude
    rateHz: 100
    latencyClass: fast
    latencyMs: 12
  - id: linkage_was_left
    type: Sensor.Angle
    mount: {nodeId: front_axle, pose: {x: 0.2, y: -1.05, z: 0.6}}
    role: steer_linkage_left
    rateHz: 200
    latencyClass: fast
    latencyMs: 8
  - id: linkage_was_right
    type: Sensor.Angle
    mount: {nodeId: front_axle, pose: {x: 0.2, y: 1.05, z: 0.6}}
    role: steer_linkage_right
    rateHz: 200
    latencyClass: fast
    latencyMs: 8

baselines:
  - id: dual_heading
    type: GNSSBaseline
    left: gnss_pair_left
    right: gnss_pair_right
    resolvesHeading: true
    lengthM: 1.6

steeringModules:
  - id: front_axle_cmd
    type: AxleSteer.Front
    nodeRef: front_axle
    controllerTopic: autosteer/front
    limits: {maxDeg: 30}
    dynamics: {latencyMs: 40, maxRateDps: 120, deadbandDeg: 0.3}
    authorityPolicy: mixed
    safeNeutral: {angleDeg: 0}
    steerGeometry: {mode: geometry, source: ackermann_wizard_v1}

modeProfiles:
  - id: road
    enable: [front_axle_cmd]
    overrides:
      hitch.dynamics.deadbandDeg: 2.0
      hitch.limits.maxDeg: 10
      toolbar_float.float: false
    interlocks:
      - iff: {speedKph: "> 40"}
        then: {disable: [rear_steer], requireAck: true}
  - id: fail_safe
    enable: []
    overrides:
      hitch.limits.maxDeg: 5
    fallback: true

capabilities:
  dofSummary: {steerInputs: 2, linear: 1, hinge: 2}
  maxCurvature: 0.18
  headland:
    patterns: [u_turn, omega]
    minTurnApertureM: 14.0
```
