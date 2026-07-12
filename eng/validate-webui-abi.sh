#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 /path/to/webui.h" >&2
  exit 64
fi

header=$1
repository_root=$(cd "$(dirname "$0")/.." && pwd)
bindings=$repository_root/src/CsWebUi.Native/WebUiNative.cs

if [[ ! -f $header ]]; then
  echo "WebUI header was not found: $header" >&2
  exit 66
fi

if [[ ! -f $bindings ]]; then
  echo "Binding source was not found: $bindings" >&2
  exit 66
fi

if ! diff -u \
  <(grep '^WEBUI_EXPORT' "$header" | sed -E 's/.* (webui_[a-z0-9_]+)\(.*/\1/' | sort -u) \
  <(grep 'EntryPoint = "webui_' "$bindings" | sed -E 's/.*EntryPoint = "(webui_[a-z0-9_]+)".*/\1/' | sort -u); then
  echo "CsWebUi.Native does not match the pinned WebUI export surface." >&2
  exit 1
fi

echo "Validated $(grep '^WEBUI_EXPORT' "$header" | wc -l) WebUI exports."
