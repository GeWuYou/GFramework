#!/usr/bin/env bash
set -euo pipefail

if ! command -v git >/dev/null 2>&1; then
    echo "git is required to enumerate tracked C# files." >&2
    exit 2
fi

if ! command -v grep >/dev/null 2>&1; then
    echo "grep is required to validate C# naming conventions." >&2
    exit 2
fi

repo_root="$(git rev-parse --show-toplevel)"
cd "$repo_root"

readonly PASCAL_CASE_REGEX='^(?:[A-Z](?=[A-Z][a-z0-9])|[A-Z]{2}(?=$|[A-Z][a-z0-9])|[A-Z][a-z0-9]+)+$'

files_checked=0
declare -a namespace_violations=()
declare -a directory_violations=()
declare -A seen_directories=()
declare -A seen_directory_violations=()

is_excluded() {
    local path="$1"
    case "$path" in
        Godot/script_templates|Godot/script_templates/*)
            return 0
            ;;
        GFramework.SourceGenerators.Tests/*/snapshots|GFramework.SourceGenerators.Tests/*/snapshots/*)
            # Source-generator snapshots are committed test assets rather than hand-authored source layout.
            # Keep naming enforcement for the real test code, but skip generated snapshot trees.
            return 0
            ;;
        *)
            return 1
            ;;
    esac
}

validate_segment() {
    local segment="$1"

    if [[ ! "$segment" =~ ^[A-Za-z][A-Za-z0-9]*$ ]]; then
        printf '%s' "must start with a letter and contain only letters or digits"
        return 1
    fi

    if [[ ! "$segment" =~ ^[A-Z] ]]; then
        printf '%s' "must start with an uppercase letter"
        return 1
    fi

    if [[ "$segment" =~ ^[A-Z]+$ ]]; then
        if (( ${#segment} <= 2 )); then
            return 0
        fi

        printf '%s' "acronyms longer than 2 letters must use PascalCase"
        return 1
    fi

    if ! printf '%s\n' "$segment" | grep -Pq "$PASCAL_CASE_REGEX"; then
        printf '%s' "must use PascalCase; only 2-letter acronyms may stay fully uppercase"
        return 1
    fi

    return 0
}

check_directory_path() {
    local relative_dir="$1"
    local raw_segment=""
    local segment=""
    local reason=""
    local key=""

    IFS='/' read -r -a raw_segments <<< "$relative_dir"
    for raw_segment in "${raw_segments[@]}"; do
        IFS='.' read -r -a segments <<< "$raw_segment"
        for segment in "${segments[@]}"; do
            if ! reason="$(validate_segment "$segment")"; then
                key="$relative_dir|$segment|$reason"
                if [[ -z "${seen_directory_violations[$key]:-}" ]]; then
                    seen_directory_violations["$key"]=1
                    directory_violations+=("- $relative_dir -> \"$segment\": $reason")
                fi

                return
            fi
        done
    done
}

while IFS= read -r relative_file; do
    if [[ -z "$relative_file" ]] || is_excluded "$relative_file"; then
        continue
    fi

    ((files_checked += 1))

    while IFS=: read -r line_number namespace; do
        [[ -z "$line_number" ]] && continue

        IFS='.' read -r -a segments <<< "$namespace"
        errors=()
        for segment in "${segments[@]}"; do
            if ! reason="$(validate_segment "$segment")"; then
                errors+=("  * $segment: $reason")
            fi
        done

        if (( ${#errors[@]} > 0 )); then
            namespace_violations+=("- $relative_file:$line_number -> $namespace")
            namespace_violations+=("${errors[@]}")
        fi
    done < <(
        sed '1s/^\xEF\xBB\xBF//' "$relative_file" |
            grep -nE '^[[:space:]]*namespace[[:space:]]+[A-Za-z][A-Za-z0-9_.]*[[:space:]]*([;{]|$)' |
            sed -E 's/^([0-9]+):[[:space:]]*namespace[[:space:]]+([^[:space:];{]+).*/\1:\2/'
    )

    current_dir="$(dirname "$relative_file")"
    while [[ "$current_dir" != "." ]]; do
        if [[ -z "${seen_directories[$current_dir]:-}" ]]; then
            seen_directories["$current_dir"]=1
            check_directory_path "$current_dir"
        fi

        current_dir="$(dirname "$current_dir")"
    done
done < <(git ls-files -- '*.cs')

if (( ${#namespace_violations[@]} > 0 || ${#directory_violations[@]} > 0 )); then
    echo "C# naming validation failed."

    if (( ${#namespace_violations[@]} > 0 )); then
        echo
        echo "Namespace violations:"
        printf '%s\n' "${namespace_violations[@]}"
    fi

    if (( ${#directory_violations[@]} > 0 )); then
        echo
        echo "Directory violations:"
        printf '%s\n' "${directory_violations[@]}"
    fi

    exit 1
fi

echo "C# naming validation passed for $files_checked tracked C# files."
