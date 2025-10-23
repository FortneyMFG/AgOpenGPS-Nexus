# Plugin Integration Guide — Core Services

This document explains how the Nexus **core** layer hosts zip plugins, which services it exposes, and the guardrails plugin entry points must respect when interacting with runtime subsystems.

---

## Runtime Responsibilities

The core host provides three responsibilities for plugins:

1. **Installation & Registry** – `PluginService` watches the plugin drop folders, unpacks zips, validates manifests, and tracks enablement in `%APPDATA%/Nexus/plugins/plugins.json`.
2. **Lifecycle Management** – For each enabled plugin, `PluginLeaseManager` coordinates capability leases (sections, guidance, telemetry, etc.) while `PluginHost` instantiates entry points, injects host services, and supervises shutdown.
3. **Shared Infrastructure** – Entry points obtain strongly typed facilities (event bus, command bus, telemetry sink, settings stores, simulation gateway, etc.) through the `IHostServices` façade described below.

---

## Host Services Surface

`Nexus.Sdk.Core` defines `IHostServices`, and the core layer provides the concrete implementation. The most frequently used members are:

| Property | Description |
| --- | --- |
| `IServiceProvider Services` | Scoped provider rooted in the plugin ALC. Plugin code should request its own services and configuration here. |
| `IEventBus EventBus` | High-throughput message bus. Publish/subscribe APIs must be used asynchronously (`ValueTask PublishAsync<T>()`). |
| `ICommandBus CommandBus` | Durable command queue for multi-module orchestration. |
| `PluginLeaseManager LeaseManager` | Acquisition and renewal of capability leases declared in the manifest. |
| `ISettingsStore Settings` | Namespaced configuration storage for plugin-scoped settings. |
| `ITelemetrySink Telemetry` | Structured telemetry pipeline used by `TelemetryLogging`. |
| `PathInfo Paths` | Provides plugin-scoped paths (logs, cache, temp, shared). All file I/O must go through these helpers. |

Entry points should cache `IHostServices` during `Initialize` and release references during `ShutdownAsync`.

---

## Registering Background Work

Plugins often need to start hosted services (timers, streaming sessions, reconciliation loops). Core provides the following helpers:

- `IHostedServiceRegistry` – Plugins register hosted services that follow the ASP.NET `IHostedService` contract. The registry ensures services start/stop alongside plugin enablement.
- `ITaskScheduler` – Lightweight scheduled callbacks for non-critical periodic work. Provides cancellation tokens tied to plugin shutdown.
- `ISimulationDirector` – For simulation-aware plugins (mapping, agronomic analytics) the director offers deterministic replay hooks.

All background work must respect the plugin cancellation token handed to `ShutdownAsync`. Failure to stop leads to an unload warning.

---

## Capability Leases

Capabilities (sections, steering, telemetry, mapping) are governed by leases. Plugins obtain leases via the `LeaseManager`:

```csharp
if (services.LeaseManager.TryAcquireLease(PluginId, "sections.control", out var handle))
{
    using var scope = services.Services.CreateScope();
    var orchestrator = scope.ServiceProvider.GetRequiredService<SectionOrchestrator>();
    orchestrator.Start(handle);
}
```

- Exclusive leases guarantee that only one plugin controls the capability.
- Shared leases allow concurrent observers but still require renewals.
- When a lease expires or is revoked, the host calls the plugin’s recovery handler (specified in the manifest).

See `Aog.Plugins/PluginLeaseManager.cs` for detailed semantics.

---

## Core Event Contracts

Plugins consume and publish messages defined in `Nexus SourceCode/proto/*.proto` and materialized via `Aog.Core.V1`. The most common payloads include:

- `SteerCmd`, `SteerState`
- `SectionMask`
- `LayerUpdate`
- `TelemetryEvent`

Contracts are versioned; the host refuses to activate plugins compiled against incompatible proto versions (detected through SDK compatibility checks).

---

## Error Handling & Diagnostics

- **Logging** – Use `IHostServices.Services.GetRequiredService<ILogger<T>>()`. Logs are automatically tagged with the plugin identifier.
- **Faults** – Throwing from `Initialize` aborts activation; the plugin remains disabled until the operator resolves the issue. Throwing from `ShutdownAsync` is logged but the ALC is still torn down.
- **Health Reports** – Use `ITelemetrySink.PublishAsync` to send health snapshots that flow into the Device Manager compatibility dashboard.

---

## Developing Core-Facing Plugins

1. Reference `Nexus.Sdk.Core`.
2. Implement `ICoreEntrypoint`.
3. Request host services you need in `Initialize`.
4. Register hosted services, event handlers, and leases.
5. Release all resources in `ShutdownAsync`.
6. Validate your manifest and package with `PackPlugin`.

For concrete examples, review the official plugin cards in `docs/Plugins/official/` and the sample implementation in `plugins/examples/fe.hello`.

