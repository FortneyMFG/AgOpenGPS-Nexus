# Official Nexus Plugins

The documents in this directory summarise the behaviour, integration points, and packaging status for the plugins that ship with Nexus. Each card links back to the relevant source files, manifests, and regression tests so contributors can quickly understand how the feature hooks into the zip plugin runtime.

If you are contributing a new plugin or migrating an existing in-tree implementation, start with `docs/plugins/architecture.md`, then consult the individual plugin card for domain-specific details.

## Available Cards

- [AutoSteer](AutoSteer.md)
- [Combine Yield](CombineYield.md)
- [Compatibility Evaluator](Compatibility.md)
- [Cost & Profit Analytics](CostProfit.md)
- [Crop Context](Crop.md)
- [Field Health](FieldHealth.md)
- [File IO](FileIO.md)
- [Genetics](Genetics.md)
- [Guidance](Guidance.md)
- [ISOBUS Bridge](Isobus.md)
- [Job Tasks](JobTasks.md)
- [Mapping](Mapping.md)
- [Planter Monitor](PlanterMonitor.md)
- [Sections Control](Sections.md)
- [Telemetry Logging](TelemetryLogging.md)
- [Variable Mapping](VariableMapping.md)
- [Weather](Weather.md)

Each card follows a common structure:

1. **Overview** – What the plugin does and why it exists.
2. **Capabilities** – Declared capabilities and core responsibilities.
3. **Core Integration** – Key services, message types, and lease requirements.
4. **UI Integration** – Blocks, windows, or surfaces exposed to the shell.
5. **Dependencies** – Direct dependencies on other plugins or platform services.
6. **Packaging Notes** – Special considerations when producing the zip package.
7. **Related Resources** – Helpful links to manifests, ADRs, or test suites.

> **Tip:** Use these cards in PR descriptions and release notes to explain changes to operators and downstream integrators.

