#!/usr/bin/env bash
# One-time import, only in this new mobile repository. Never pushes to desktop.
set -euo pipefail
if test -f src/BlackjackCasino.Core/BlackjackCasino.Core.csproj; then
  echo 'Desktop baseline already imported.'
  exit 0
fi
if test "${GITHUB_REPOSITORY:-}" != 'TeKatz/blackjack-casino-mobile'; then
  echo 'Import is restricted to TeKatz/blackjack-casino-mobile.' >&2
  exit 1
fi
source_commit=ddbb708c01405637b4f19fd15365813c31133b06
git config user.name 'github-actions[bot]'
git config user.email '41898282+github-actions[bot]@users.noreply.github.com'
git fetch --no-tags https://github.com/TeKatz/blackjack-casino.git "$source_commit"
git merge --no-ff --no-commit --allow-unrelated-histories -s ours "$source_commit"
# Keep the mobile configuration and workflows; inherit all other source files.
git archive "$source_commit" | tar -x --anchored --exclude='.github' --exclude='Directory.Packages.props' --exclude='README.md'
git show "$source_commit:README.md" > docs/desktop-original-readme.md
git add .
git commit -m 'Import pinned desktop baseline with complete ancestry for Android development'
git push origin HEAD:main
if ! git ls-remote --exit-code --heads origin development > /dev/null; then
  git push origin HEAD:development
fi
