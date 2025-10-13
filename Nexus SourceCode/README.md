# Nexus Source Code

## Core Host (NX-010)

The Nexus Core host is a generic-host based console application that bootstraps dependency
injection, configuration, and structured logging for the headless guidance engine.

### Running the host

```bash
dotnet run --project "src/Aog.Core.Host/Aog.Core.Host.csproj"
```

The host reads configuration from `appsettings.json` and environment variables prefixed with
`NEXUS_`. A background health service emits periodic heartbeat logs (`Core host heartbeat OK.`)
that higher-level orchestration or smoke tests can watch for successful startup/shutdown.

### Configuration

`CoreHost:Health:IntervalSeconds` controls how frequently the heartbeat message is emitted. The
value must be greater than zero; validation runs when the host starts.
