#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: build-deb.sh [options]

Options:
  --version <semver>       Package version string (default: 0.0.0-dev)
  --configuration <name>   Build configuration (default: Release)
  --runtime <rid>          Target runtime identifier (default: linux-arm64)
  --output <dir>           Output directory for the resulting .deb (default: <repo>/artifacts)
  -h, --help               Show this help message
USAGE
}

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"

version="0.0.0-dev"
configuration="Release"
runtime="linux-arm64"
output_dir="${repo_root}/artifacts"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version)
      version="$2"
      shift 2
      ;;
    --configuration)
      configuration="$2"
      shift 2
      ;;
    --runtime)
      runtime="$2"
      shift 2
      ;;
    --output)
      output_dir="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      >&2 printf 'error: unknown option %s\n' "$1"
      usage
      exit 1
      ;;
  esac
done

mkdir -p "${output_dir}"

require_tool() {
  if ! command -v "$1" >/dev/null 2>&1; then
    >&2 printf 'error: required tool "%s" not found on PATH.\n' "$1"
    exit 1
  fi
}

require_tool dotnet
require_tool dpkg-deb

core_project="${repo_root}/Nexus SourceCode/src/Aog.Core.Host/Aog.Core.Host.csproj"
agio_project="${repo_root}/Nexus SourceCode/src/Aog.Agio/Aog.Agio.csproj"
core_appsettings="${repo_root}/Nexus SourceCode/src/Aog.Core.Host/appsettings.json"
agio_appsettings="${repo_root}/Nexus SourceCode/src/Aog.Agio/appsettings.json"

for project in "${core_project}" "${agio_project}"; do
  if [[ ! -f "${project}" ]]; then
    >&2 printf 'error: project not found: %s\n' "${project}"
    exit 1
  fi
done

work_dir="$(mktemp -d)"
trap 'rm -rf "${work_dir}"' EXIT

publish_root="${work_dir}/publish"
core_publish="${publish_root}/core"
agio_publish="${publish_root}/agio"

printf 'Publishing Core host (%s)...\n' "${runtime}"
dotnet publish "${core_project}" \
  --configuration "${configuration}" \
  --runtime "${runtime}" \
  --self-contained false \
  --output "${core_publish}"

printf 'Publishing AGiO host (%s)...\n' "${runtime}"
dotnet publish "${agio_project}" \
  --configuration "${configuration}" \
  --runtime "${runtime}" \
  --self-contained false \
  --output "${agio_publish}"

package_name="nexus-pi"
case "${runtime}" in
  *-arm64)
    architecture="arm64"
    ;;
  *-armhf|*-arm)
    architecture="armhf"
    ;;
  *-x64)
    architecture="amd64"
    ;;
  *-x86)
    architecture="i386"
    ;;
  *)
    architecture="all"
    ;;
esac

pkgroot="${work_dir}/pkgroot"
install -d "${pkgroot}/opt/nexus/core"
install -d "${pkgroot}/opt/nexus/agio"
install -d "${pkgroot}/lib/systemd/system"
install -d "${pkgroot}/etc/nexus/core"
install -d "${pkgroot}/etc/nexus/agio"

cp -a "${core_publish}/." "${pkgroot}/opt/nexus/core/"
cp -a "${agio_publish}/." "${pkgroot}/opt/nexus/agio/"

install -m 0644 "${core_appsettings}" "${pkgroot}/etc/nexus/core/appsettings.json"
install -m 0644 "${agio_appsettings}" "${pkgroot}/etc/nexus/agio/appsettings.json"

# Ensure the hosts pick up operator edits in /etc/nexus by symlinking
# their working directory copies of appsettings.json back to /etc.
rm -f "${pkgroot}/opt/nexus/core/appsettings.json"
rm -f "${pkgroot}/opt/nexus/agio/appsettings.json"
ln -s "/etc/nexus/core/appsettings.json" "${pkgroot}/opt/nexus/core/appsettings.json"
ln -s "/etc/nexus/agio/appsettings.json" "${pkgroot}/opt/nexus/agio/appsettings.json"

core_env_path="${pkgroot}/etc/nexus/core.env"
agio_env_path="${pkgroot}/etc/nexus/agio.env"
cat <<'ENV' > "${core_env_path}"
# Environment overrides for the Nexus Core host.
# Example:
# NEXUS_COREHOST__AGIO__ENDPOINT=http://127.0.0.1:5105
# NEXUS_COREHOST__AGIO__ALLOWUNENCRYPTEDHTTP2=true
ENV

cat <<'ENV' > "${agio_env_path}"
# Environment overrides for the Nexus AGiO host.
# Example:
# NEXUS_AGIOHOST__BACKEND__ASSEMBLY=Aog.Agio.Sim
# NEXUS_AGIOHOST__BACKEND__TYPE=Aog.Agio.Sim.SimAgioBackend
ENV

install -m 0644 "${script_dir}/systemd/nexus-core.service" "${pkgroot}/lib/systemd/system/nexus-core.service"
install -m 0644 "${script_dir}/systemd/nexus-agio.service" "${pkgroot}/lib/systemd/system/nexus-agio.service"

installed_size=$(du -sk "${pkgroot}" | cut -f1)

debian_dir="${pkgroot}/DEBIAN"
install -d "${debian_dir}"

cat > "${debian_dir}/control" <<CONTROL
Package: ${package_name}
Version: ${version}
Section: utils
Priority: optional
Architecture: ${architecture}
Maintainer: Nexus Packaging <noreply@example.com>
Depends: dotnet-runtime-8.0
Installed-Size: ${installed_size}
Description: AgOpenGPS Nexus services for Raspberry Pi
 Nexus headless services (Core + AGiO) packaged for Raspberry Pi OS.
 Provides systemd units and default configuration under /etc/nexus.
CONTROL

cat > "${debian_dir}/postinst" <<'POSTINST'
#!/bin/bash
set -e

user=nexus
group=nexus

if ! getent group "${group}" >/dev/null 2>&1; then
  addgroup --system "${group}"
fi

if ! id -u "${user}" >/dev/null 2>&1; then
  adduser --system --home /var/lib/nexus --ingroup "${group}" --shell /usr/sbin/nologin "${user}"
fi

mkdir -p /var/lib/nexus
chown -R "${user}:${group}" /opt/nexus || true
chown -R "${user}:${group}" /etc/nexus || true

if command -v systemctl >/dev/null 2>&1; then
  systemctl daemon-reload || true
  systemctl enable nexus-agio.service || true
  systemctl enable nexus-core.service || true

  if systemctl is-enabled nexus-agio.service >/dev/null 2>&1; then
    systemctl restart nexus-agio.service || true
  fi
  if systemctl is-enabled nexus-core.service >/dev/null 2>&1; then
    systemctl restart nexus-core.service || true
  fi
fi
POSTINST

cat > "${debian_dir}/prerm" <<'PRERM'
#!/bin/bash
set -e

if command -v systemctl >/dev/null 2>&1; then
  case "$1" in
    remove|upgrade)
      systemctl stop nexus-core.service >/dev/null 2>&1 || true
      systemctl stop nexus-agio.service >/dev/null 2>&1 || true
      systemctl disable nexus-core.service >/dev/null 2>&1 || true
      systemctl disable nexus-agio.service >/dev/null 2>&1 || true
      ;;
    abort-install|abort-upgrade)
      ;;
    *)
      ;;
  esac
fi
PRERM

cat > "${debian_dir}/postrm" <<'POSTRM'
#!/bin/bash
set -e
if command -v systemctl >/dev/null 2>&1; then
  systemctl daemon-reload >/dev/null 2>&1 || true
fi
POSTRM

chmod 0755 "${debian_dir}/postinst" "${debian_dir}/prerm" "${debian_dir}/postrm"

output_path="${output_dir}/${package_name}_${version}_${architecture}.deb"

dpkg-deb --build --root-owner-group "${pkgroot}" "${output_path}"

printf 'Package created: %s\n' "${output_path}"
