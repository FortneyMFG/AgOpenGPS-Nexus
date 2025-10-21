# 91-O6 — Remote gRPC/WebSocket clients backed by the Linux Core

## Summary
Frontends (desktop, web, tablet) connect to the Linux Core through gRPC and WebSocket APIs. A native desktop UI (Qt/Avalonia) can
ship alongside a browser-based client for headless mode and lightweight display tablets. Layout metadata is stored server-side so
multiple clients stay synchronized.

## Details
- gRPC channel for high-frequency data (guidance state, sections, machine telemetry) with generated clients (.NET, C++, TypeScript).
- WebSocket/JSON channel mirrors key streams for browser dashboards, kiosk displays, and mobile devices.
- Session management handles authentication, capability negotiation (touch vs. mouse, multi-monitor), and layout persistence.
- Remote clients subscribe to layer metadata and PGN-derived feeds exposed by the Core/bridge.
- Supports kiosk auto-login launching UI fullscreen; multi-monitor layouts stored in JSON.
- Tablet/phone "display client" option fetches map tiles + telemetry for remote visualization.

## Pros
- Enables headless deployments with remote control from any device with a browser.
- Keeps Windows desktop viable while adding Linux-native and browser experiences.
- Facilitates multi-monitor setups and remote supervision without duplicating logic.

## Cons
- Requires robust API design and session security.
- Adds latency/bandwidth considerations for high-rate data in browser clients.
- Demands synchronization mechanisms for configuration changes across clients.

## Risks & mitigations
- **Risk:** Network drops interrupt control → **Mitigation:** Maintain local fail-safe UI for primary operator; buffer commands.
- **Risk:** API churn breaks clients → **Mitigation:** Versioned endpoints + compatibility tests per release.
- **Risk:** Browser performance for 3D map → **Mitigation:** Use WebGL (MapLibre/Three.js) with throttled updates; fall back to raster.

## Borrowables
- Existing multi-monitor layout persistence from Windows app.
- AgDiag and community web dashboards for telemetry streaming examples.
- Map tile pipeline from current renderer for reuse in browser client.

## Rough effort
M (requires API scaffolding, UI rewrites, and sync features).

## References
- Community discussions on headless deployments, kiosk mode, and remote displays (GitHub Discussions Feb 2024).
- [Section 91 — UI Shell & Layout](../9X_Frontends_Ops/91_UI_Shell_Layout.md)
- [Section 41 — Inter-Application API](../4X_Interprocess_Communications/41_Inter_Application_API.md)

## Related ADRs
- [ADR-003 — Avalonia UI](../../ADR/ADR-003-avalonia-ui.md)
- [ADR-018 — Plugin API](../../ADR/ADR-018-plugin-api.md)
- [ADR-028 — Stack Boundaries](../../ADR/ADR-028-stack-boundaries.md)
