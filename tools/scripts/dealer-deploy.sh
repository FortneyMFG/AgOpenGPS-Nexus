#!/usr/bin/env bash
set -euo pipefail

output=""
packages=()
profile=""
force=false

usage() {
    cat <<USAGE
Usage: dealer-deploy.sh --output <dir> [--package <path>]... [--profile <path>] [--force]

Copies Nexus release artifacts into a dealer-facing bundle with checksum and checklist files.
USAGE
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --output)
            output="$2"
            shift 2
            ;;
        --package)
            packages+=("$2")
            shift 2
            ;;
        --profile)
            profile="$2"
            shift 2
            ;;
        --force)
            force=true
            shift
            ;;
        --help|-h)
            usage
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage
            exit 1
            ;;
    esac
done

if [[ -z "$output" ]]; then
    echo "--output is required" >&2
    exit 1
fi

resolved_output="$(realpath "$output")"
if [[ -d "$resolved_output" && "$force" = false ]]; then
    echo "Output directory '$resolved_output' already exists. Use --force to reuse." >&2
    exit 1
fi

mkdir -p "$resolved_output/packages"
mkdir -p "$resolved_output/config"

copied_packages=()
for pkg in "${packages[@]}"; do
    if [[ ! -f "$pkg" ]]; then
        echo "Package '$pkg' not found" >&2
        exit 1
    fi
    dest="$resolved_output/packages/$(basename "$pkg")"
    cp "$pkg" "$dest"
    copied_packages+=("$dest")
done

if [[ -n "$profile" && -e "$profile" ]]; then
    if [[ -d "$profile" ]]; then
        if command -v rsync >/dev/null 2>&1; then
            rsync -a "$profile"/ "$resolved_output/config/"
        else
            cp -R "$profile"/. "$resolved_output/config/"
        fi
    else
        cp "$profile" "$resolved_output/config/"
    fi
fi

checksum_file="$resolved_output/checksums.txt"
: > "$checksum_file"
for file in "$resolved_output"/packages/*; do
    if [[ -f "$file" ]]; then
        if command -v sha256sum >/dev/null 2>&1; then
            sum=$(sha256sum "$file" | cut -d ' ' -f1)
        elif command -v shasum >/dev/null 2>&1; then
            sum=$(shasum -a 256 "$file" | cut -d ' ' -f1)
        elif command -v openssl >/dev/null 2>&1; then
            sum=$(openssl dgst -sha256 "$file" | awk '{print $2}')
        else
            echo "No SHA-256 tool available to compute checksum for $file" >&2
            sum="UNKNOWN"
        fi
        echo "$sum  $(basename "$file")" >> "$checksum_file"
    fi
done

cat <<'CHECKLIST' > "$resolved_output/dealer-checklist.md"
# Dealer Deployment Checklist

1. Copy this bundle to the dealer USB drive.
2. Validate package checksums with `sha256sum` (Linux/macOS) or `Get-FileHash` (Windows).
3. Load translated machine profiles via `legacy-tool translate` before the install visit.
4. Exercise the bench harness with `legacy-tool soak --seconds 30` and confirm the report is clean.
5. Attach field-specific settings or guidance files under `config/` if required.
6. Record the delivery in the dealer tracker and file the signed checklist.
CHECKLIST

echo "Dealer deployment bundle created at $resolved_output"
if [[ ${#copied_packages[@]} -gt 0 ]]; then
    echo "Copied packages:"
    for pkg in "${copied_packages[@]}"; do
        echo "  $pkg"
    done
fi
