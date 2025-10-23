#!/usr/bin/env bash
set -euo pipefail

if [[ ${#@} -eq 0 ]]; then
  mapfile -t files < <(git diff --name-only --cached -- '*.md')
else
  files=("$@")
fi

if [[ ${#files[@]} -eq 0 ]]; then
  echo "No markdown files to check." >&2
  exit 0
fi

missing=0
for file in "${files[@]}"; do
  [[ -f "$file" ]] || continue
  if [[ "${file##*.}" != "md" ]]; then
    continue
  fi

  if ! head -n 1 "$file" | grep -qx -- '---'; then
    echo "[lint-doc-front-matter] $file: missing opening front matter delimiter (---)" >&2
    missing=1
    continue
  fi

  front_matter=$(awk 'BEGIN{capture=0} /^---$/{if(capture){exit} capture=1; next} capture{print}' "$file")

  if [[ -z "$front_matter" ]]; then
    echo "[lint-doc-front-matter] $file: empty front matter block" >&2
    missing=1
    continue
  fi

  for field in owner status last_reviewed related_tickets; do
    if ! grep -q -- "^${field}:" <<<"$front_matter"; then
      echo "[lint-doc-front-matter] $file: missing '${field}' entry" >&2
      missing=1
    fi
  done

done

if [[ $missing -ne 0 ]]; then
  echo "Front matter lint failed." >&2
  exit 1
fi

echo "Front matter lint passed." >&2
