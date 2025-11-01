#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DEFAULT_DATA_PATH="${HOME}/Downloads/MU_Red_1_20_61_Full/Data"
DATA_PATH="${MU_DATA_PATH:-$DEFAULT_DATA_PATH}"
COPY_DATA=1
DOTNET_ARGS=()

usage() {
  cat <<EOF
Usage: $(basename "$0") [options] [dotnet publish args...]

Options:
  --data <path>    Override MU data folder (defaults to ${DEFAULT_DATA_PATH})
  --no-copy        Skip copying Data folder into publish output
  -h, --help       Show this help message

Any additional arguments are passed straight through to dotnet publish.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --data)
      shift
      [[ $# -gt 0 ]] || { echo "error: --data requires a path" >&2; exit 1; }
      DATA_PATH="$1"
      ;;
    --no-copy)
      COPY_DATA=0
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      DOTNET_ARGS+=("$1")
      ;;
  esac
  shift || true
done

if [[ ! -d "$DATA_PATH" ]]; then
  echo "error: data path '$DATA_PATH' not found" >&2
  exit 1
fi

export MU_DATA_PATH="$DATA_PATH"

echo "Publishing MuMac with MU_DATA_PATH=${MU_DATA_PATH}"
dotnet publish "${REPO_ROOT}/MuMac/MuMac.csproj" \
  -f net9.0 \
  -c Release \
  -p:EnableMobileTargets=false \
  "${DOTNET_ARGS[@]}"

PUBLISH_DIR="${REPO_ROOT}/MuMac/bin/Release/net9.0/osx-x64/publish"

if [[ "$COPY_DATA" -eq 1 ]]; then
  echo "Copying data files into ${PUBLISH_DIR}/Data"
  rsync -a --delete "${MU_DATA_PATH}/" "${PUBLISH_DIR}/Data/"
else
  echo "Skipping data copy (use --no-copy to suppress this message)." 
fi

echo "Publish complete. App bundle located at ${PUBLISH_DIR}"
