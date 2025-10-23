# Nexus CLI Completions Kit

The unified `nx` host leverages System.CommandLine's suggestion directive so you
can ship shell completions alongside plugin bundles. Use the helper script to
export Bash, Zsh, and PowerShell completions for the currently built host.

```powershell
./tools/scripts/generate-nx-completions.ps1 -NxBinary /path/to/nx
```

The script writes results to `./artifacts/nx-cli/completions/` by default. Source
them using the conventional approach for your shell:

- **Bash:** `source artifacts/nx-cli/completions/nx.bash`
- **Zsh:** `source artifacts/nx-cli/completions/nx.zsh`
- **PowerShell:** `. artifacts/nx-cli/completions/nx.ps1`

Regenerate the completions whenever new verbs or options land. Plugin teams are
encouraged to drop pre-generated files into their release zips for faster
onboarding.
