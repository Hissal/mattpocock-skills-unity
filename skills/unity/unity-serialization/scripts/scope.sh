#!/usr/bin/env bash
# Grep-only reserialize scope, for when no Editor can run SerializationScope.cs.
# Lists every UnityYAML file under Assets/ and Packages/ that references the seed
# scripts, to a fixpoint: each matched prefab adds its own GUID, pulling in nested
# instances, variants and scene instances.
#
# Usage: scope.sh <project> <seed>...
#   <seed> is a script path (its .meta supplies the GUID) or a bare 32-hex GUID.
# Seed with every script whose type serializes the changed type, and their
# subclasses. Without the Editor's type closure, hosts reached through subclasses,
# nested types or [SerializeReference] fields are missing unless their scripts are
# seeded too: say so in the proposal.

set -euo pipefail

if [ $# -lt 2 ]; then
  echo "usage: scope.sh <project> <script.cs|guid>..." >&2
  exit 2
fi

project=$1
shift
patterns=$(mktemp)
matched=$(mktemp)
trap 'rm -f "$patterns" "$matched"' EXIT

guid_of() {
  sed -n 's/^guid: \([0-9a-f]\{32\}\).*/\1/p' "$1.meta"
}

for seed in "$@"; do
  if [[ $seed =~ ^[0-9a-f]{32}$ ]]; then
    guid=$seed
  elif [ -f "$seed.meta" ]; then
    guid=$(guid_of "$seed")
  else
    echo "scope.sh: no .meta beside $seed" >&2
    exit 1
  fi
  echo "guid: $guid" >> "$patterns"
done

cd "$project"
dirs=(Assets)
[ -d Packages ] && dirs+=(Packages)

while :; do
  added=0
  while IFS= read -r file; do
    grep -qxF "$file" "$matched" && continue
    head -c 5 "$file" | grep -q '^%YAML' || continue
    echo "$file" >> "$matched"
    added=1
    case $file in
      *.prefab) echo "guid: $(guid_of "$file")" >> "$patterns" ;;
    esac
  done < <(grep -rlF --exclude='*.meta' -f "$patterns" "${dirs[@]}" || true)
  [ $added -eq 0 ] && break
done

sort "$matched"
