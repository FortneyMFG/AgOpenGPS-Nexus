# Legacy auto-run verification checklist

- [ ] Translate the latest vehicle XML with `legacy-tool translate` and stage the
      JSON under `config/`.
- [ ] Run `legacy-tool soak --seconds 30 --output soak.json` and confirm the UDP
      frame totals match the sample report.
- [ ] Import `legacy-auto-run.json` into Nexus and select the `legacy-auto-run`
      scenario for bench playback.
- [ ] Attach the generated `soak.json` and this signed checklist to the dealer
      package.

Operator: ____________________    Date: _______________
