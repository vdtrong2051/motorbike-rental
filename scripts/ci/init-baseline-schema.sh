#!/usr/bin/env bash
set -euo pipefail
# GD3 adds a MySQL table via independent SQL, not an EF migration.
# Starting EF Core 9, current code cannot apply OLD migrations when the model differs.
# Bootstrap old schema using the exact *accepted GD2 code* to preserve the guardrail.
BASE_COMMIT="f50ed26f3f695df3a43bb334fb84734ad428a58a"
BASE_DIR="$(mktemp -d -t rental-gd3-baseline-XXXXXXXX)"
# git worktree needs an empty/nonexistent target.
rmdir "$BASE_DIR"
cleanup() {
  git worktree remove --force "$BASE_DIR" >/dev/null 2>&1 || true
}
trap cleanup EXIT

if ! git cat-file -e "${BASE_COMMIT}^{commit}" 2>/dev/null; then
  # checkout@v4 defaults to shallow clone. Fetch only the short accepted history.
  git fetch --no-tags --depth=15 origin develop
fi
git worktree add --detach "$BASE_DIR" "$BASE_COMMIT"
(
  cd "$BASE_DIR/RentalPlatform"
  dotnet tool restore
  dotnet tool run dotnet-ef database update --configuration Release
)
echo "Existing EF schema initialized using accepted GD2 model. GD3 table is independent SQL."
