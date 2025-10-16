#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: build-packages.sh [options]

Options:
  --version <semver>       Package version (default: 0.0.0-dev)
  --configuration <name>   Build configuration (default: Release)
  --runtime <rid>          Target runtime identifier (default: linux-x64)
  --output <dir>           Output directory (default: <repo>/artifacts)
  --format <deb|rpm|all>   Package format to build (default: all)
  -h, --help               Show this help message
USAGE
}

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"

version="0.0.0-dev"
configuration="Release"
runtime="linux-x64"
output_dir="${repo_root}/artifacts"
format="all"

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
    --format)
      format="$2"
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

needs_rpm=false
case "${format}" in
  rpm)
    needs_rpm=true
    ;;
  all)
    needs_rpm=true
    ;;
  deb)
    ;;
  *)
    >&2 printf 'error: unknown format "%s". Use deb, rpm, or all.\n' "${format}"
    exit 1
    ;;
esac

if ${needs_rpm}; then
  require_tool rpmbuild
fi

map_deb_arch() {
  case "$1" in
    *-arm64|*aarch64*) echo "arm64" ;;
    *-armhf|*-arm) echo "armhf" ;;
    *-x86|*-win-x86) echo "i386" ;;
    *-x64|*-amd64) echo "amd64" ;;
    *) echo "amd64" ;;
  esac
}

map_rpm_arch() {
  case "$1" in
    *-arm64|*aarch64*) echo "aarch64" ;;
    *-armhf|*-arm) echo "armv7hl" ;;
    *-x86|*-win-x86) echo "i686" ;;
    *-x64|*-amd64) echo "x86_64" ;;
    *) echo "$(uname -m)" ;;
  esac
}

deb_arch="$(map_deb_arch "${runtime}")"
rpm_arch="$(map_rpm_arch "${runtime}")"

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

stage_root="${work_dir}/stage"
install -d "${stage_root}/usr/lib/aog/core"
install -d "${stage_root}/usr/lib/aog/agio"
install -d "${stage_root}/usr/bin"
install -d "${stage_root}/etc/aog/core"
install -d "${stage_root}/etc/aog/agio"
install -d "${stage_root}/lib/systemd/system"
install -d "${stage_root}/var/lib/aog"
install -d "${stage_root}/var/log/aog"

cp -a "${core_publish}/." "${stage_root}/usr/lib/aog/core/"
cp -a "${agio_publish}/." "${stage_root}/usr/lib/aog/agio/"

install -m 0644 "${core_appsettings}" "${stage_root}/etc/aog/core/appsettings.json"
install -m 0644 "${agio_appsettings}" "${stage_root}/etc/aog/agio/appsettings.json"

ln -sfn "/etc/aog/core/appsettings.json" "${stage_root}/usr/lib/aog/core/appsettings.json"
ln -sfn "/etc/aog/agio/appsettings.json" "${stage_root}/usr/lib/aog/agio/appsettings.json"

cat <<'ENV' > "${stage_root}/etc/aog/core.env"
# Environment overrides for the Nexus Core host.
# Example:
# NEXUS_COREHOST__AGIO__ENDPOINT=http://127.0.0.1:5105
# NEXUS_COREHOST__AGIO__ALLOWUNENCRYPTEDHTTP2=true
ENV

cat <<'ENV' > "${stage_root}/etc/aog/agio.env"
# Environment overrides for the Nexus AGiO host.
# Example:
# NEXUS_AGIOHOST__BACKEND__ASSEMBLY=Aog.Agio.Sim
# NEXUS_AGIOHOST__BACKEND__TYPE=Aog.Agio.Sim.SimAgioBackend
ENV

cat <<'WRAPPER' > "${stage_root}/usr/bin/aog-core"
#!/usr/bin/env bash
set -euo pipefail
exec /usr/bin/dotnet /usr/lib/aog/core/Aog.Core.Host.dll "$@"
WRAPPER

cat <<'WRAPPER' > "${stage_root}/usr/bin/aog-agio"
#!/usr/bin/env bash
set -euo pipefail
exec /usr/bin/dotnet /usr/lib/aog/agio/Aog.Agio.dll "$@"
WRAPPER

chmod 0755 "${stage_root}/usr/bin/aog-core" "${stage_root}/usr/bin/aog-agio"

install -m 0644 "${script_dir}/systemd/aog-core.service" "${stage_root}/lib/systemd/system/aog-core.service"
install -m 0644 "${script_dir}/systemd/aog-agio.service" "${stage_root}/lib/systemd/system/aog-agio.service"

package_name="aog-core"
release="1"

build_deb() {
  local deb_root="${work_dir}/deb-root"
  rm -rf "${deb_root}"
  mkdir -p "${deb_root}"
  cp -a "${stage_root}/." "${deb_root}/"

  local debian_dir="${deb_root}/DEBIAN"
  mkdir -p "${debian_dir}"

  local installed_size
  installed_size=$(du -sk "${deb_root}" | cut -f1)

  cat > "${debian_dir}/control" <<CONTROL
Package: ${package_name}
Version: ${version}
Section: utils
Priority: optional
Architecture: ${deb_arch}
Maintainer: Nexus Packaging <noreply@example.com>
Depends: dotnet-runtime-8.0
Installed-Size: ${installed_size}
Description: AOG Core headless services for Linux
 Headless Core + AGiO hosts packaged for Linux servers and SBCs.
 Provides systemd units, wrapper launchers, and configuration templates.
CONTROL

  cat > "${debian_dir}/postinst" <<'POSTINST'
#!/bin/bash
set -e

user=aogsvc
group=aogsvc

if ! getent group "${group}" >/dev/null 2>&1; then
  addgroup --system "${group}"
fi

if ! id -u "${user}" >/dev/null 2>&1; then
  adduser --system --home /var/lib/aog --ingroup "${group}" --shell /usr/sbin/nologin "${user}"
fi

mkdir -p /var/lib/aog /var/log/aog
chown -R "${user}:${group}" /usr/lib/aog || true
chown -R "${user}:${group}" /etc/aog || true
chown -R "${user}:${group}" /var/lib/aog || true
chown -R "${user}:${group}" /var/log/aog || true

if command -v systemctl >/dev/null 2>&1; then
  systemctl daemon-reload || true
  systemctl enable aog-agio.service || true
  systemctl enable aog-core.service || true

  if systemctl is-enabled aog-agio.service >/dev/null 2>&1; then
    systemctl restart aog-agio.service || true
  fi
  if systemctl is-enabled aog-core.service >/dev/null 2>&1; then
    systemctl restart aog-core.service || true
  fi
fi
POSTINST

  cat > "${debian_dir}/prerm" <<'PRERM'
#!/bin/bash
set -e

if command -v systemctl >/dev/null 2>&1; then
  case "$1" in
    remove|upgrade)
      systemctl stop aog-core.service >/dev/null 2>&1 || true
      systemctl stop aog-agio.service >/dev/null 2>&1 || true
      systemctl disable aog-core.service >/dev/null 2>&1 || true
      systemctl disable aog-agio.service >/dev/null 2>&1 || true
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

  dpkg-deb --build --root-owner-group "${deb_root}" "${output_dir}/${package_name}_${version}_${deb_arch}.deb"
  printf 'Debian package created: %s\n' "${output_dir}/${package_name}_${version}_${deb_arch}.deb"
}

build_rpm() {
  local rpm_top="${work_dir}/rpmbuild"
  local specs_dir="${rpm_top}/SPECS"
  rm -rf "${rpm_top}"
  mkdir -p "${specs_dir}" "${rpm_top}/BUILD" "${rpm_top}/RPMS" "${rpm_top}/SOURCES" "${rpm_top}/SRPMS"

  cat > "${specs_dir}/${package_name}.spec" <<'SPEC'
Name:           %{_pkgname}
Version:        %{_pkgversion}
Release:        %{_pkgrelease}%{?dist}
Summary:        AOG Core headless services
License:        MIT
URL:            https://github.com/farmOS/AgOpenGPS-Nexus
Requires:       dotnet-runtime-8.0
BuildArch:      %{_pkgarch}
AutoReqProv:    no

%description
Headless Core and AGiO services with systemd units, wrapper launchers, and
configuration templates for Linux servers and SBC deployments.

%prep

%build

%install
mkdir -p "%{buildroot}"
cp -a "%{_pkgstage}"/. "%{buildroot}/"

%files
%defattr(-,root,root,-)
/usr/bin/aog-core
/usr/bin/aog-agio
/usr/lib/aog
%config(noreplace) /etc/aog/core/appsettings.json
%config(noreplace) /etc/aog/agio/appsettings.json
%config(noreplace) /etc/aog/core.env
%config(noreplace) /etc/aog/agio.env
/lib/systemd/system/aog-core.service
/lib/systemd/system/aog-agio.service
%dir /var/lib/aog
%dir /var/log/aog

%pre
getent group aogsvc >/dev/null 2>&1 || groupadd -r aogsvc
getent passwd aogsvc >/dev/null 2>&1 || useradd -r -g aogsvc -d /var/lib/aog -s /sbin/nologin aogsvc

%post
/usr/bin/install -d -m 0755 -o aogsvc -g aogsvc /var/lib/aog /var/log/aog || true
/usr/bin/chown -R aogsvc:aogsvc /usr/lib/aog || true
/usr/bin/chown -R aogsvc:aogsvc /etc/aog || true
/usr/bin/chown -R aogsvc:aogsvc /var/lib/aog || true
/usr/bin/chown -R aogsvc:aogsvc /var/log/aog || true
if command -v systemctl >/dev/null 2>&1; then
  systemctl daemon-reload >/dev/null 2>&1 || true
  systemctl enable aog-agio.service >/dev/null 2>&1 || true
  systemctl enable aog-core.service >/dev/null 2>&1 || true
  if systemctl is-enabled aog-agio.service >/dev/null 2>&1; then
    systemctl restart aog-agio.service >/dev/null 2>&1 || true
  fi
  if systemctl is-enabled aog-core.service >/dev/null 2>&1; then
    systemctl restart aog-core.service >/dev/null 2>&1 || true
  fi
fi

%preun
if [ "$1" -eq 0 ]; then
  if command -v systemctl >/dev/null 2>&1; then
    systemctl stop aog-core.service >/dev/null 2>&1 || true
    systemctl stop aog-agio.service >/dev/null 2>&1 || true
    systemctl disable aog-core.service >/dev/null 2>&1 || true
    systemctl disable aog-agio.service >/dev/null 2>&1 || true
  fi
fi

%postun
if command -v systemctl >/dev/null 2>&1; then
  systemctl daemon-reload >/dev/null 2>&1 || true
fi
SPEC

  rpmbuild \
    --define "_topdir ${rpm_top}" \
    --define "_buildrootdir ${rpm_top}/BUILDROOT" \
    --define "_pkgstage ${stage_root}" \
    --define "_pkgname ${package_name}" \
    --define "_pkgversion ${version}" \
    --define "_pkgrelease ${release}" \
    --define "_pkgarch ${rpm_arch}" \
    -bb "${specs_dir}/${package_name}.spec"

  local rpm_file
  rpm_file="${rpm_top}/RPMS/${rpm_arch}/${package_name}-${version}-${release}.${rpm_arch}.rpm"
  cp "${rpm_file}" "${output_dir}/"
  printf 'RPM package created: %s\n' "${output_dir}/$(basename "${rpm_file}")"
}

case "${format}" in
  deb)
    build_deb
    ;;
  rpm)
    build_rpm
    ;;
  all)
    build_deb
    build_rpm
    ;;
esac
