#!/usr/bin/env bash
# Simple launcher (Linux)
set -euo pipefail
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
"${DIR}/core/Nexus.Core"  &
CORE_PID=$!
"${DIR}/agio/Nexus.AgIO"  &
UI="${DIR}/ui/Nexus.UI"
if [ -x "$UI" ]; then "$UI" & fi
wait $CORE_PID
