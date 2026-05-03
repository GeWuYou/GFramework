#!/usr/bin/env bash
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0


set -euo pipefail

ROOT_DIR="$(CDPATH='' cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT_PATH="${ROOT_DIR}/.ai/environment/tools.raw.yaml"
MODE="${1:---check}"

usage() {
    cat <<'EOF'
Usage:
  bash scripts/collect-dev-environment.sh --check
  bash scripts/collect-dev-environment.sh --write

Modes:
  --check  Print the raw project-relevant environment inventory.
  --write  Write the raw inventory to .ai/environment/tools.raw.yaml.
EOF
}

ensure_supported_mode() {
    case "${MODE}" in
        --check|--write)
            ;;
        *)
            usage
            exit 1
            ;;
    esac
}

command_path() {
    local tool="$1"

    if command -v "${tool}" >/dev/null 2>&1; then
        command -v "${tool}"
    else
        printf '%s' ""
    fi
}

command_installed() {
    local tool="$1"

    if command -v "${tool}" >/dev/null 2>&1; then
        printf 'true'
    else
        printf 'false'
    fi
}

command_version() {
    local tool="$1"

    if ! command -v "${tool}" >/dev/null 2>&1; then
        printf '%s' "not-installed"
        return
    fi

    case "${tool}" in
        dotnet)
            dotnet --version 2>/dev/null || printf '%s' "unknown"
            ;;
        python3)
            python3 --version 2>/dev/null || printf '%s' "unknown"
            ;;
        node)
            node --version 2>/dev/null || printf '%s' "unknown"
            ;;
        npm)
            npm --version 2>/dev/null || printf '%s' "unknown"
            ;;
        bun)
            bun --version 2>/dev/null || printf '%s' "unknown"
            ;;
        git)
            git --version 2>/dev/null || printf '%s' "unknown"
            ;;
        rg)
            rg --version 2>/dev/null | head -n 1 || printf '%s' "unknown"
            ;;
        jq)
            jq --version 2>/dev/null || printf '%s' "unknown"
            ;;
        docker)
            docker --version 2>/dev/null || printf '%s' "unknown"
            ;;
        bash)
            bash --version 2>/dev/null | head -n 1 || printf '%s' "unknown"
            ;;
        *)
            "${tool}" --version 2>/dev/null | head -n 1 || printf '%s' "unknown"
            ;;
    esac
}

python_package_version() {
    local package_name="$1"

    python3 - "${package_name}" <<'PY'
from importlib import metadata
import sys

package_name = sys.argv[1]

try:
    print(metadata.version(package_name))
except metadata.PackageNotFoundError:
    print("not-installed")
PY
}

python_package_installed() {
    local package_name="$1"
    local version

    version="$(python_package_version "${package_name}")"

    if [[ "${version}" == "not-installed" ]]; then
        printf 'false'
    else
        printf 'true'
    fi
}

read_os_release() {
    local key="$1"

    python3 - "$key" <<'PY'
import pathlib
import sys

target_key = sys.argv[1]
values = {}
for line in pathlib.Path("/etc/os-release").read_text(encoding="utf-8").splitlines():
    if "=" not in line:
        continue
    key, value = line.split("=", 1)
    values[key] = value.strip().strip('"')

print(values.get(target_key, "unknown"))
PY
}

collect_inventory() {
    local os_name distro version_id kernel shell_name wsl_enabled wsl_version timestamp

    os_name="$(uname -s)"
    distro="$(read_os_release PRETTY_NAME)"
    version_id="$(read_os_release VERSION_ID)"
    kernel="$(uname -r)"
    shell_name="$(basename "${SHELL:-bash}")"
    timestamp="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"

    if grep -qi microsoft /proc/version 2>/dev/null; then
        wsl_enabled="true"
    else
        wsl_enabled="false"
    fi

    if command -v wslinfo >/dev/null 2>&1; then
        wsl_version="$(wslinfo --wsl-version 2>/dev/null || printf '%s' "unknown")"
    else
        wsl_version="unknown"
    fi

    cat <<EOF
schema_version: 1
generated_at_utc: "${timestamp}"
generator: "scripts/collect-dev-environment.sh"

platform:
  os: "${os_name}"
  distro: "${distro}"
  version: "${version_id}"
  kernel: "${kernel}"
  wsl: ${wsl_enabled}
  wsl_version: "${wsl_version}"
  shell: "${shell_name}"

required_runtimes:
  dotnet:
    installed: $(command_installed dotnet)
    version: "$(command_version dotnet)"
    path: "$(command_path dotnet)"
    purpose: "Builds and tests the GFramework solution."
  python3:
    installed: $(command_installed python3)
    version: "$(command_version python3)"
    path: "$(command_path python3)"
    purpose: "Runs local automation and environment collection scripts."
  node:
    installed: $(command_installed node)
    version: "$(command_version node)"
    path: "$(command_path node)"
    purpose: "Provides the JavaScript runtime used by docs tooling."
  bun:
    installed: $(command_installed bun)
    version: "$(command_version bun)"
    path: "$(command_path bun)"
    purpose: "Installs and previews the VitePress documentation site."

required_tools:
  git:
    installed: $(command_installed git)
    version: "$(command_version git)"
    path: "$(command_path git)"
    purpose: "Source control and patch review."
  bash:
    installed: $(command_installed bash)
    version: "$(command_version bash)"
    path: "$(command_path bash)"
    purpose: "Executes repository scripts and shell automation."
  rg:
    installed: $(command_installed rg)
    version: "$(command_version rg)"
    path: "$(command_path rg)"
    purpose: "Fast text search across the repository."
  jq:
    installed: $(command_installed jq)
    version: "$(command_version jq)"
    path: "$(command_path jq)"
    purpose: "Inspecting and transforming JSON outputs."

project_tools:
  docker:
    installed: $(command_installed docker)
    version: "$(command_version docker)"
    path: "$(command_path docker)"
    purpose: "Runs MegaLinter and other containerized validation tools."

python_packages:
  requests:
    installed: $(python_package_installed requests)
    version: "$(python_package_version requests)"
    purpose: "Simple HTTP calls in local helper scripts."
  rich:
    installed: $(python_package_installed rich)
    version: "$(python_package_version rich)"
    purpose: "Readable CLI output for local Python helpers."
  openai:
    installed: $(python_package_installed openai)
    version: "$(python_package_version openai)"
    purpose: "Optional scripted access to OpenAI APIs."
  tiktoken:
    installed: $(python_package_installed tiktoken)
    version: "$(python_package_version tiktoken)"
    purpose: "Optional token counting for prompt and context inspection."
  pydantic:
    installed: $(python_package_installed pydantic)
    version: "$(python_package_version pydantic)"
    purpose: "Optional typed config and schema validation for helper scripts."
  pytest:
    installed: $(python_package_installed pytest)
    version: "$(python_package_version pytest)"
    purpose: "Optional lightweight testing for Python helper scripts."
EOF
}

ensure_supported_mode

if [[ "${MODE}" == "--write" ]]; then
    mkdir -p "$(dirname "${OUTPUT_PATH}")"
    collect_inventory > "${OUTPUT_PATH}"
    printf 'Wrote %s\n' "${OUTPUT_PATH}"
else
    collect_inventory
fi
