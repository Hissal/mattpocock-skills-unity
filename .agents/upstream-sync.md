# Upstream sync

How to merge `mattpocock/skills` (remote `upstream`) into this fork. Sync on demand, and at least whenever upstream cuts a release.

`upstream` is configured `--no-tags` and main-only, so upstream's `v1.x` release tags never reach new clones. The fork's own tags are `unity-v<version>`, which cannot collide with them.

## Steps

1. `git fetch upstream`, then branch from `main`: `git checkout -b sync/upstream-<YYYY-MM-DD>`.
2. `git merge upstream/main`. Always a merge commit, never a squash or rebase, so `upstream/main` stays an ancestor of `main` and the next sync only sees new commits.
3. Resolve upstream's release files by these rules, not by reading the hunks:
   - `.changeset/*.md` added by upstream: delete them all. Left in, the fork's next version PR would consume them. The fork's own changesets are named `unity-*.md`; keep those.
   - `CHANGELOG.md`: keep ours. The fork's changelog only lists fork releases.
   - The `version` field in `package.json` and `.claude-plugin/plugin.json`: keep ours. Take upstream's other edits to those files.
   - `package-lock.json`: never hand-merge. Take either side, then run `npm install` to regenerate it.
   - Every other conflict: resolve with the `resolving-merge-conflicts` skill, keeping each Unity delta.
4. Check the Unity deltas survived: `git diff upstream/main -- skills/ docs/` must show only the fork's intended Unity changes. A delta that vanished, or an upstream change that reads as a fork change, is a bad resolution. The reasoning behind each delta lives in its adapt ticket and in `research/`.
5. If upstream added, renamed, or removed a skill: place it in the right bucket per `CLAUDE.md`, re-read `ask-matt`'s `SKILL.md` so its map stays accurate, and run `scripts/link-skills.sh`.
6. Run `claude plugin validate . --strict` and `npm run check-plugin-version`.
7. Add one changeset, `.changeset/unity-upstream-sync-<YYYY-MM-DD>.md`, a patch bump by default, naming the upstream commit range merged and linking upstream's `CHANGELOG.md` for what changed.
8. Push the branch and open a PR. Merge it with a merge commit.
