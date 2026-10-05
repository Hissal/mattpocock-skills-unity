# Upstream sync

How to merge `mattpocock/skills` (remote `upstream`) into this fork. Sync on demand, and at least whenever upstream cuts a release.

`upstream` is configured `--no-tags` and main-only, so upstream's `v1.x` release tags never reach new clones. The fork's own tags are `unity-v<version>`, which cannot collide with them.

## Steps

1. `git fetch upstream`, then branch from `main`: `git checkout -b sync/upstream-<YYYY-MM-DD>`.
2. `git merge upstream/main`. Always a merge commit, never a squash or rebase, so `upstream/main` stays an ancestor of `main` and the next sync only sees new commits.
3. Resolve conflicts by these rules. Upstream's release files follow a fixed rule, not a reading of the hunks:
   - `.changeset/*.md` added by upstream: delete them all. Left in, the fork's next version PR would consume them. The fork's own changesets are named `unity-*.md`; keep those.
   - `CHANGELOG.md`: keep ours. The fork's changelog only lists fork releases.
   - The `version` field in `package.json` and `.claude-plugin/plugin.json`: keep ours. Take upstream's other edits to those files.
   - `package-lock.json`: never hand-merge. Take either side, then run `npm install` to regenerate it.
   - Every other conflict: trace each side to its intent (upstream's commit or PR, the fork delta's adapt ticket and `research/` file) and keep both where they fit. Keep each Unity delta, re-applying it onto upstream's new text when upstream rewrote the file. A Unity asset or `.meta` conflict follows `unity-serialization`.
4. Check the Unity deltas survived: `git diff upstream/main -- skills/ docs/` must show only the fork's intended Unity changes. A delta that vanished, or an upstream change that reads as a fork change, is a bad resolution. The reasoning behind each delta lives in its adapt ticket and in `research/`.
5. If upstream added, renamed, or removed a skill: place it in the right bucket per `CLAUDE.md`, re-read `ask-matt`'s `SKILL.md` so its map stays accurate, and run `scripts/link-skills.sh`. A removed promoted skill keeps upstream's archived docs page, with the fork's Unity paragraph dropped from it, and its stale links in `~/.claude/skills` and `~/.agents/skills` are deleted by hand, since the script does not prune them.
6. Run `claude plugin validate . --strict` and `npm run check-plugin-version`.
7. Add one changeset, `.changeset/unity-upstream-sync-<YYYY-MM-DD>.md`, a patch bump by default, naming the upstream commit range merged and linking upstream's `CHANGELOG.md` for what changed.
8. Push the branch and open a PR. Merge it with a merge commit.
