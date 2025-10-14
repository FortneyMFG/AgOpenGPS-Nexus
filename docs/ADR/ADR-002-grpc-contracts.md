# ADR-002: Expose Nexus services over gRPC/protobuf contracts

## Status
Accepted

## Context
The Nexus runtime needs a unified, typed inter-process API that Core, UI, plugins, and automation tools can share while remaining portable across Windows and Linux deployments. Options exploration highlights gRPC/protobuf surfaces published via `Aog.Abstractions` as the preferred evolution path because it isolates hardware integration details inside AgIO/Bridge services and lets higher-level processes communicate over a consistent contract surface.【F:docs/SRS/sections/03_Comm_Transports.md†L4-L58】【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L9-L36】 Contributors also want determinism, versioning, and compatibility bridges while introducing new transports.【F:docs/SRS/sections/03_Comm_Transports.md†L26-L60】【F:docs/SRS/options/O-COMM-6_PGNCompatibilityBridge.md†L1-L35】 The MCU communications stack is now defined separately by ADR-006 (AOG-Link) so this decision focuses strictly on intra-host service boundaries.

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

## References
- [Section 03 — Communications & Transports](../SRS/sections/03_Comm_Transports.md)
- [Option O-STACK-1 — .NET 8 + Avalonia stack](../SRS/options/O-STACK-1_DotNet8Avalonia.md)
- [Option O-COMM-6 — PGN compatibility bridge](../SRS/options/O-COMM-6_PGNCompatibilityBridge.md)
- [ADR-006 — MCU communications over AOG-Link (nanopb)](ADR-006-aog-link-mcu-communications.md)
