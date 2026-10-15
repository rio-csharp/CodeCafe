#!/usr/bin/env bash
set -euo pipefail

target_branch="${TARGET_BRANCH:?TARGET_BRANCH is required}"
pr_head_sha="${PR_HEAD_SHA:?PR_HEAD_SHA is required}"

git fetch --no-tags --prune origin "+refs/heads/${target_branch}:refs/remotes/origin/${target_branch}"

target_ref="refs/remotes/origin/${target_branch}"
merge_base="$(git merge-base "$target_ref" "$pr_head_sha")"
target_head="$(git rev-parse "$target_ref")"

if [ "$merge_base" != "$target_head" ]; then
  echo ""
  echo "ERROR: This PR is not based on the latest origin/$target_branch."
  echo ""
  echo "   Your branch was created from an older commit."
  echo "   Please rebase before merging:"
  echo ""
  echo "      git fetch origin"
  echo "      git checkout <your-branch>"
  echo "      git rebase origin/$target_branch"
  echo "      git push --force-with-lease"
  echo ""
  exit 1
fi

echo "PR branch is based on latest origin/$target_branch."
