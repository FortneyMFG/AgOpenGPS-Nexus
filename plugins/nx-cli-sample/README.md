# Sample Nexus CLI Plugin

This bundle demonstrates how a plugin can expose CLI verbs either through a
local adapter (`Nexus.SamplePlugin.Cli`) or via the gRPC reflection service
introduced for the unified `nx` host.

## Verbs

- `nx sample calibrate` — accepts `--offset`, `--gain`, and `--dry-run` options to
  simulate applying calibration coefficients.
- `nx sample sniff` — captures raw diagnostic samples with `--duration` and
  `--file` options.

Both commands respect the host-level `--output` switch when the adapter is
loaded alongside the plugin manifest.

## Layout

```
org.agopengps.nx.sample-0.1.0.zip
├── manifest.json
├── assets/
│   └── README.md
└── lib/
    ├── Nexus.SamplePlugin.Cli.dll
    ├── Nexus.SamplePlugin.Cli.deps.json
    └── Nexus.SamplePlugin.Cli.runtimeconfig.json
```

Use `tools/ci/package-sidecar-plugins.ps1` to emit the archive and then expand
it into `~/.nexus/plugins/org.agopengps.nx.sample/0.1.0/` before rerunning the
CLI host to validate manifest discovery.
