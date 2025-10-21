# 43 — Channel Security (Status: collecting proposals)

## Problem statement
Establish authentication, encryption, and key-rotation policies for inter-process and inter-device links (gRPC, UDP bridges, BLE, SocketCAN) so Nexus deployments remain secure even when transports traverse shared networks.【F:docs/SRS/sections/9X_Frontends_Ops/95_Security_Permissions.md†L12-L156】【F:docs/SRS/sections/4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md†L1-L35】

## Requirements (from contributors)
- R-CHAN-000 (MUST, transport encryption): Require TLS 1.3/mTLS for gRPC and WebSocket channels; UDP/SocketCAN bridges must support DTLS or per-frame HMAC when hardware allows, with fallbacks documented for legacy controllers.【F:docs/SRS/sections/9X_Frontends_Ops/95_Security_Permissions.md†L24-L92】
- R-CHAN-001 (MUST, identity): Issue device certificates tied to Nexus identity records (CM5, tablets, servers) with renewal workflows and revocation lists consumed by Core at startup.【F:docs/SRS/sections/9X_Frontends_Ops/95_Security_Permissions.md†L94-L156】
- R-CHAN-002 (SHOULD, key rotation): Automate key rotation on a configurable cadence (e.g., 30 days) with rolling restarts that preserve ongoing guidance sessions.【F:docs/SRS/sections/9X_Frontends_Ops/95_Security_Permissions.md†L128-L156】
- R-CHAN-003 (MUST, capability gating): Bind channel capabilities (pose.read, section.command, storage.write) to authenticated identities so unauthorized clients cannot subscribe or issue control commands.【F:docs/SRS/sections/9X_Frontends_Ops/95_Security_Permissions.md†L48-L120】
- R-CHAN-004 (SHOULD, observability): Emit security audit events (handshake success/failure, cert expiry, policy overrides) into telemetry pipelines for dashboards and alerting.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L96-L146】
- R-CHAN-005 (MUST, offline pairing): Provide offline pairing workflows (QR codes, short-lived pairing tokens) for field rigs without internet access while still provisioning unique credentials per device.【F:docs/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L16-L44】

## Current sentiment
Secure channels are non-negotiable as Nexus shifts toward headless and remote deployments; contributors are prioritizing certificate automation and per-channel capability enforcement before enabling remote plugin access.【F:docs/SRS/sections/9X_Frontends_Ops/95_Security_Permissions.md†L24-L156】【F:docs/SRS/sections/4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md†L1-L35】
