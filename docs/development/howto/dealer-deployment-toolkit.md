# Dealer deployment toolkit (NX-087)

The dealer toolkit bundles a repeatable set of scripts and checklists for staging
Nexus builds before they leave the lab. Use it whenever you prepare USB media or
network shares for regional dealers.

## What the toolkit does

- Copies release packages and translated machine profiles into a structured
  bundle using [`dealer-deploy.ps1`](../../tools/scripts/dealer-deploy.ps1) (Windows) or
  [`dealer-deploy.sh`](../../tools/scripts/dealer-deploy.sh) (Linux/macOS).
- Generates a SHA-256 manifest so dealers can validate downloads offline.
- Drops a pre-filled checklist that walks the installer through verification,
  including the new `legacy-tool translate` and `legacy-tool soak` commands.

## Quick start

1. Collect the Windows and Linux release archives plus any translated profile
   JSON from the legacy CLI.
2. Run the deployment script for your platform:
   - Windows PowerShell: `pwsh tools/scripts/dealer-deploy.ps1 -Output ./staging -Packages ./Nexus-Windows.zip -Profile ./profiles`
   - Linux/macOS: `tools/scripts/dealer-deploy.sh --output ./staging --package ./Nexus-Linux.tar.gz --profile ./profiles`
3. Copy the contents of the generated folder to dealer media.
4. Hand off the `dealer-checklist.md` file and review it with the installer.

Both scripts are idempotent; pass `-Force`/`--force` if you want to reuse an
existing output directory.

## Included files

| Path | Purpose |
| --- | --- |
| `packages/` | Release archives copied from the build output. |
| `config/` | Optional profile JSON or field assets you want preloaded. |
| `checksums.txt` | SHA-256 manifest generated during staging. |
| `dealer-checklist.md` | Printable checklist for the dealer hand-off. |

## Verification checklist

The generated checklist reminds dealers to:

1. Validate package checksums before leaving the shop.
2. Import machine profiles with `legacy-tool translate` and copy the JSON into
   `config/`.
3. Run `legacy-tool soak --seconds 30` against the bench harness and confirm the
   soak report shows matching frame counts.
4. Record delivery in the regional tracker and file the signed checklist.

## Troubleshooting

- **Missing checksum utilities:** The shell script falls back to `openssl` when
  `sha256sum` is unavailable. If neither tool exists the checksum entry is marked
  `UNKNOWN` and the script emits a warning.
- **Existing output directory:** Use `-Force`/`--force` to reuse an existing
  staging folder after confirming old artifacts are safe to overwrite.
- **Profile translation:** Re-run `legacy-tool translate` for any new legacy
  XML files and copy the resulting JSON into `config/` before invoking the
  deployment scripts.

This workflow keeps dealer media consistent while guaranteeing each package has
been validated and spot-tested with the legacy soak harness.
