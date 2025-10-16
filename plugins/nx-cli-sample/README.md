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
sample-plugin/
├── README.md
├── plugin.json
└── Adapters/
    └── Nexus.SamplePlugin.Cli.dll (produced by package-nx-cli.ps1)
```

Copy the directory into `~/.nexus/plugins/org.agopengps.nx.sample/0.1.0/` and
rerun the CLI host to validate manifest discovery.
