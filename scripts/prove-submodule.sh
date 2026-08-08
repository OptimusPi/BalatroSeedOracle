#!/usr/bin/env bash
# Fail loud if Motely submodule is empty.
# Usage: ./scripts/prove-submodule.sh
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CSPROJ="$ROOT/src/MotelyJAML/Motely/Motely.csproj"
if [[ ! -f "$CSPROJ" ]]; then
  echo "SUBMODULE_MISSING — run: git submodule update --init --recursive"
  exit 1
fi
echo "SUBMODULE_OK  $CSPROJ"
