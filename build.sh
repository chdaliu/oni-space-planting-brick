#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

# Generate the building animation. If Pillow is missing, keep the committed
# animation assets and skip regeneration.
if command -v python3 >/dev/null 2>&1 && python3 -c "import PIL" >/dev/null 2>&1; then
    python3 tools/generate_anim.py
else
    echo "Skipping animation generation: python3 or Pillow not found."
fi

dotnet build -c Release "$@"

echo
echo "Deployed to:"
echo "  $HOME/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Local/ONI-SpacePlantingBrick"
