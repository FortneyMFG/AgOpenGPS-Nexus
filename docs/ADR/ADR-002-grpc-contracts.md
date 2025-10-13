# ADR-002: Expose Nexus services over gRPC/protobuf contracts

## Status
Accepted

## Context
The communications slice requires modern transports that coexist with legacy PGN flows while enabling typed APIs for Core, UI, and plugin consumers. Options exploration highlights gRPC/protobuf surfaces published via `Aog.Abstractions` as the preferred evolution path because it keeps hardware integration inside AgIO backends and lets higher-level services share contracts across Windows and Linux builds.【F:docs/SRS/sections/03_Comm_Transports.md†L4-L62】【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L9-L36】 Contributors also want determinism, versioning, and compatibility bridges while introducing new transports.【F:docs/SRS/sections/03_Comm_Transports.md†L26-L60】【F:docs/SRS/options/O-COMM-6_PGNCompatibilityBridge.md†L1-L35】

## Decision
Adopt gRPC with protobuf IDLs as the authoritative inter-process API for Nexus services. All Core, UI, plugin, and automation components consume generated clients from the `Aog.Abstractions` package. AgIO backends publish the canonical gRPC hosts, translating to and from serial/UDP/CAN PGNs where required so legacy devices remain supported during the transition.

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
  - Document the PGN bridge strategy that feeds the gRPC host (NX-004/NX-005 tasks).

## References
- [Section 03 — Communications & Transports](../SRS/sections/03_Comm_Transports.md)
- [Option O-STACK-1 — .NET 8 + Avalonia stack](../SRS/options/O-STACK-1_DotNet8Avalonia.md)
- [Option O-COMM-6 — PGN compatibility bridge](../SRS/options/O-COMM-6_PGNCompatibilityBridge.md)
