# Fork installation verification, 2026-10-10

Implementation evidence for [#113](https://github.com/Hissal/mattpocock-skills-unity/issues/113), governed by [decision #111](https://github.com/Hissal/mattpocock-skills-unity/issues/111), under [#107](https://github.com/Hissal/mattpocock-skills-unity/issues/107). This slice preserves fork identities, existing routes, release files, manifests, and the promoted-only selection model.

## Environment and boundary

Windows 10.0.26200, Node 22.23.2, Codex CLI 0.162.1, GitHub Copilot CLI 1.0.95, VS Code 1.141.0, commit `2a59476c9bfcb90b3ddc372c36762471b7dfad1c`. Implemented from integration tip `d1f332e` in branch `docs/verify-fork-installation-routes`.

Temporary state and raw logs are under `%TEMP%/issue-113-install-routes`. Subprocess environment maps override `CODEX_HOME` and `COPILOT_HOME` to that directory; no persistent user environment variable or global harness configuration was changed. VS Code CLI checks use separate `--user-data-dir` and `--extensions-dir` directories. Existing installed skills were neither edited nor removed.

Temporary configuration isolates writes, but both CLIs can still discover global `~/.agents/skills`. Do not treat inherited skills as installation evidence. Codex results were filtered by `pluginId`; Copilot's inherited entries could not establish plugin availability. Raw outputs remain local and are not a duplicate skill catalog.

The published fork installed by the GitHub source was main at `3d926c6e1c2024ee2579f9f5fdb362d8dc5e6515`, version 1.1.0. These observations establish the existing manifest route at that commit, not installation of the unpushed integration branch. That branch retains the same manifests and release identity.

## Codex

With the temporary home, these commands exited zero:

```bash
codex plugin marketplace add Hissal/mattpocock-skills-unity --json
codex plugin add mattpocock-skills-unity@hissal
codex plugin list
```

The resolver returned marketplace `hissal`, installed plugin `mattpocock-skills-unity`, version 1.1.0, and cache `codex/plugins/cache/hissal/mattpocock-skills-unity/1.1.0`. Its manifest declares 35 explicit skill paths across engineering, productivity, and Unity; every declared SKILL.md exists. Codex warned that helper binaries cannot be created under a temporary home, but installation and listing succeeded.

A fresh bounded `codex app-server --stdio` process received `initialize`, `initialized`, and `skills/list` with `forceReload: true`. After filtering for `pluginId == mattpocock-skills-unity@hissal`, all 35 declared promoted skills were returned enabled, including user-invoked `implement-spec`, model-invoked `tdd`, and the Unity router and seven Unity mechanics skills. There were no skill errors and no non-promoted plugin skills. Unrelated global inherited entries were excluded from this evidence. The response does not expose implicit-invocation policy, and no model execution or human slash-command invocation was exercised.

The old historical ADR limitation concerned Codex's own single-path manifest. This Codex version successfully reads the fork's existing Claude-format curated manifest, without a new manifest or bucket changes.

Startup occurred after installation, but no new published plugin version became available during that startup. Re-listing the same version does not prove an automatic update. Complete intended invocation behavior and startup release transitions remain Unvalidated. The supported-route and automatic-update claims are deferred together.

## Copilot CLI

With the temporary home, these commands exited zero:

```bash
copilot plugin marketplace add Hissal/mattpocock-skills-unity
copilot plugin install mattpocock-skills-unity@hissal
copilot plugin list
```

The resolver returned `mattpocock-skills-unity@hissal`, version 1.1.0. Temporary settings recorded GitHub source `Hissal/mattpocock-skills-unity`, marketplace `hissal`, and the enabled plugin. All 35 promoted manifest paths exist in the plugin cache. This is file presence, not proof of discovered invocation availability or non-promoted exclusion.

`copilot skill list --json` was run from the temporary directory. It reported global inherited and built-in skills rather than distinct installed-plugin skill entries, so same-name inherited skills cannot be counted as successful fork-plugin discovery. It also reported:

```text
copilot\installed-plugins\hissal\mattpocock-skills-unity\skills\productivity\writing-for-agents\SKILL.md: failed to parse YAML frontmatter: mapping values are not allowed in this context at line 2 column 146
```

The unquoted colon in that description is present on published main and unchanged in the integration result. This pre-existing loader limitation is recorded without expanding this ticket into a skill repair.

The proposed `extraKnownMarketplaces.hissal.autoUpdate: true` opt-in and an automatic release transition were not exercised. No update command was used as a substitute for automatic-update evidence. Invocation availability, exclusion behavior, and opt-in automatic updates remain Unvalidated; defer supported-route and auto-update claims.

## VS Code

`code --version` identified the version above. A bounded `code --user-data-dir <temporary-dir> --extensions-dir <temporary-dir> --list-extensions` completed with no installed extensions, establishing separate empty CLI configuration directories.

The selected route requires the interactive **Chat: Install Plugin From Source** command. Native application control is unavailable in this environment, and the installed CLI does not expose that palette action. No unobservable window was launched, no existing VS Code profile was changed, and no existing sign-in state was copied. Actual source installation, resolved identity, skill availability, exclusion, and a daily automatic release transition remain Unvalidated. The route is deferred rather than inferred from upstream's source review.

## Result and guidance

All three selected verification attempts are recorded; none meets the complete evidence gate in #111. Codex and Copilot have direct initial fork-install evidence, but a successful install alone does not authorize advertising a partially validated supported route. The canonical block, README, contributor instructions, and installation ADR retain the existing Claude Code and editable skills.sh routes and explicitly defer the new supported-route and automatic-update claims.

skills.sh update guidance now pins `skills@latest` consistently and explains re-running `add` for new skills. Existing manual Claude updates, upstream/fork duplication warnings, and plugin/editable duplication warnings remain. No Gemini guidance is added, so no two-bucket upstream command or upstream skill count is imported.

## Checks

A one-off acceptance check at the parent-approved documentation seam first failed because selected-route deferral was absent. After editing guidance it passed canonical-block equality against README, explicit deferral, manual update and new-skill guidance, and historical upstream provenance. The check lives at `%TEMP%/issue-113-install-routes/check-guidance.py`; no document-coupled regression framework was added.

`npm run check`, `npm run validate-plugin`, `npm run check-plugin-version`, and `git diff --check` pass. Version stays 1.1.0. The integration branch must run its combined checks again before readiness. No Unity mechanic changes, so Unity Editor verification is not required.
