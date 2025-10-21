# ADR-002: Expose Nexus services over gRPC/protobuf contracts

## Status
Accepted

**Relevant Plugin(s):** Full Stack


## Context
The Nexus runtime needs a unified, typed inter-process API that Core, UI, plugins, and automation tools can share while remaining portable across Windows and Linux deployments. Options exploration highlights gRPC/protobuf surfaces published via `Aog.Abstractions` as the preferred evolution path because it isolates hardware integration details inside AgIO/Bridge services and lets higher-level processes communicate over a consistent contract surface.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L4-L58】【F:docs/SRS/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md†L9-L36】 Contributors also want determinism, versioning, and compatibility bridges while introducing new transports.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L26-L60】【F:docs/SRS/sections/4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md†L1-L35】 The MCU communications stack is now defined separately by ADR-006 (AOG-Link) so this decision focuses strictly on intra-host service boundaries.

## Decision
Adopt gRPC with protobuf IDLs as the authoritative inter-process API for Nexus services. All Core, UI, plugin, and automation components consume generated clients from the `Aog.Abstractions` package. AgIO/Bridge services host the canonical gRPC endpoints, translating between gRPC contracts and the MCU-facing AOG-Link datagram protocol (ADR-006) and, where needed, bridging AOG-Link frames to legacy PGN transports.

## Consequences
- Positive impacts
  - Strongly typed, versioned contracts shared across all components.
  - Enables streaming APIs with flow control, aligning with simulation and telemetry needs.
  - Supports language-agnostic clients for future integrations beyond .NET.
- Negative/mitigated impacts
  - Adds hosting overhead relative to raw sockets; mitigated by reusing ASP.NET Core gRPC infrastructure.
  - Requires contract governance to avoid breaking changes; addressed through ADR reviews and semantic versioning.
- Follow-up actions
  - Define protobuf packages and namespaces for the first wave of contracts (NX-003).
  - Implement the Bridge translation path between gRPC services, AOG-Link datagrams, and legacy PGNs (see NX-120 series tasks).

## Legacy Implementation Notes
### AgOpenGPS v6
- Inter-process coordination is limited to serial and UDP PGN streams managed by AgIO and the WinForms host, leaving no typed API surface between components today.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L7-L15】【F:docs/SRS/references/AgIO_PGN_Baseline.md†L1-L24】

### Legacy Dev Branch
- The dev branch continues to lean on the same PGN transports while experimenting with SocketCAN and normalization inside bridge prototypes, rather than shipping a shared gRPC contract layer.【F:docs/SRS/sections/4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md†L1-L36】

## Governance Updates
- **Contract review board.** The Platform Architecture group now runs a bi-weekly contract clinic. Schema diffs require sign-off from Core, AgIO, and Plugin leads with golden-file verification across wire compatibility fixtures. Proposed breaking changes must ship dual-field shims and a downgrade guide before approval.
- **Compatibility automation.** Generated proto descriptors feed a lint that rejects unreviewed field renames/removals and enforces reserved ranges. Integration tests replay recorded PGN/AOG-Link payloads through Bridge services to ensure codecs remain reversible.
- **Change-control checklist.** Plugin authors must attach: protobuf diff summary, golden round-trip logs, bridge regression report, and documentation updates. Release tooling blocks package publication until the checklist is complete.

## References
- [Section 42 — Transports](../SRS/sections/4X_Interprocess_Communications/42_Transports.md)
- [Option 11-O1 — Unified .NET 8 + Avalonia stack](../SRS/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md)
- [Option O-COMM-6 — PGN compatibility bridge](../SRS/sections/4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md)
- [ADR-006 — MCU communications over AOG-Link (nanopb)](ADR-006-aog-link-mcu-communications.md)
