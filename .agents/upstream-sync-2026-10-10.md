# Upstream sync slice 112

Implementation record for [#112](https://github.com/Hissal/mattpocock-skills-unity/issues/112), under [#107](https://github.com/Hissal/mattpocock-skills-unity/issues/107). This is a provisional integration slice, not the completed sync PR.

## Integration handoff

- Branch: `sync/upstream-2026-10-10`.
- Prepared in `C:/dev/skills/mattpocock-skills-unity-sync-112` to preserve the two staged research files in the original checkout.
- Fetched fork main: `3d926c6e1c2024ee2579f9f5fdb362d8dc5e6515`, equal to local main at preparation.
- Previous upstream base: `4588b32ecab9ecc9fc8cc6b6c5e7d675b6004b0d`.
- Merged upstream tip: `49dd158d1076134a641b33efb035946536778336`, with merge ancestry retained. No later upstream commits are included.
- Continue #113 and #114 on this branch after their decision dependencies resolve. #115 adds the single sync changeset, performs whole-result validation, and opens the complete PR. Leave this branch unmerged until then.

## Reconciliation

The pinned range contains 42 commits and changes 59 files. Five files conflicted. `implement` retains both the Unity verification line and upstream's issue fetching and explicit skill calls. `setup` retains its Unity config block and adds missing-label creation. The canonical install block, README, and contributor instructions retain the fork version for the later slices.

The remaining skill fixes and their upstream docs merged automatically. Additional docs alignment removes stale open-bug claims for ask-matt and TDD and explains the synced setup, diagnosis, wayfinder, grilling, and wizard behavior. Experimental chief-of-staff and claude-handoff changes stay in `in-progress` and outside the plugin.

All 16 incoming upstream changesets were excluded. Existing fork changesets, changelog, package and lockfile, plugin version and marketplace manifest are unchanged. No skills were added, removed, renamed, or promoted. Installed skills were not re-linked.

A comparison of the fork against the prior upstream base found 74 skill/docs paths with fork differences. Unchanged fork skill files remain byte-equivalent after newline normalization. Every nonblank original added delta line survives in overlapping skill files. Source review of the changed skill and docs hunks complements that preservation check; line retention alone does not prove prompt behavior.

## Deferred upstream content

The following incoming content is intentionally absent or held at its pre-sync fork value. The upstream merge ancestry includes these changes, so subsequent slices must explicitly reapply approved content from the pinned tip rather than expect another merge to deliver it.

- #113: `.agents/install-block.md`, `.agents/adr/0002-ship-as-a-claude-code-plugin.md`, and installation sections in `README.md` and `CLAUDE.md`.
- #114: incoming `SCOPE.md`, issue forms and configuration under `.github/ISSUE_TEMPLATE/`, `.github/workflows/needs-info.yml`, `.github/workflows/triage-label.yml`, `docs/agents/triage-labels.md`, contributor-policy portions of `CLAUDE.md` and `README.md`, and all incoming `.out-of-scope/` changes. Existing rejection records remain unchanged pending adaptation.
- #115: the single fork patch sync changeset and the complete PR.

No automatic closure policy or repository-setting change is enabled by this slice.

## Validation on 2026-10-10

Repository checks ran on Windows with Node `v22.23.2` and Claude Code `2.1.296`. GitHub CLI was `2.90.0`, so the newer sub-issue command path was source-reviewed and its documented API fallback remains relevant:

- `npm run check`: passed, including its plugin-version and plugin-validation checks.
- `npm run validate-plugin`: passed strict validation, with only the documented root contributor-context warnings excluded.
- `npm run check-plugin-version`: passed; package and plugin remain at `1.1.0`.
- `git diff --check` and staged whitespace check: passed.
- Preservation checks: passed for fork release files, manifests, deferred installation/contributor files, existing changesets and policy files, and skill identities.

Focused wizard checks ran in WSL Ubuntu, Bash `5.2.21(1)-release`, Python 3.12, with temporary files on the Linux filesystem. The working-copy template was normalized from CRLF to LF in temporary copies. The one-off driver is `%TEMP%/issue-112-wizard-validation.py`; no test framework was added to the repo.

All 13 focused checks passed:

1. Quote/source round trips for empty strings, spaces, apostrophes, backslashes, double quotes, dollar signs, backticks, command substitution text, semicolons, hashes, equals signs, and trailing spaces. No injected marker file appeared. This regression fails against the prior-base template and passes against the synced template.
2. Rerun default retention and one-line upsert idempotence.
3. Plain double-quoted dotenv decoding and refusal of unsafe double-quoted defaults.
4. Visible-input EOF rejects an empty capture without reusing a saved value; partial input is retained.
5. Secret-input EOF follows the same behavior.
6. Writes retain the symlink and update its target while preserving another key.
7. New files have mode `0600`; an existing `0640` mode survives an update.
8. Missing and failing browser openers leave a manual URL warning, using stubs.
9. Unavailable tracker commands record secret and variable fallback work, using stubs.
10. Readline left-arrow insertion produces `abc` from typed `ac`, left arrow, and `b`, through a PTY.
11. Terminal clearing falls back to escape sequences when `tput` fails, through a PTY.
12. Rewriting the script during a paused stage does not change the already-parsed stage body, through a PTY.
13. `bash -n` passes for the complete template.

## Limits

Browser launch and tracker fallback checks used stubs. No real browser was launched, no secret was written, and no tracker transition was exercised. macOS, native Windows Git Bash, real terminal rendering, and filesystem permission behavior outside WSL remain Unvalidated. Generated consumer wizards and global installations were untouched.

No new installation route or contributor automation is advertised or enabled here. Their live verification belongs to the dependent slices. Prompt/source review and repository checks do not prove every skill in every harness. No Unity runtime mechanic changed, so Unity Editor compilation, tests, and a player build were not required.

The Windows sandbox could not start commands. Approved execution outside that sandbox completed this work; restarting T3 alone did not resolve sandbox startup.
