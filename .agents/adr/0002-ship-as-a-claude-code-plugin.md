# Ship the skill set as a Claude Code plugin; gate further routes on fork evidence

This ADR retains upstream's original investigation as historical evidence. Its tested versions and official-marketplace listing describe upstream, not this fork. The current fork decision appears in the 2026-10-10 update below.

## Historical upstream evidence

These skills have always been installable via [skills.sh](https://skills.sh/mattpocock/skills) (`npx skills add mattpocock/skills`), which copies editable skill files into a user's project across Claude Code, Codex, and other Agent-Skills-standard harnesses. A recurring request is a **plug-and-play** distribution: subscribe to the set as a read-only, always-current bundle you don't edit, rather than a fork you own. That is exactly what native plugin systems provide.

We ship a native **Claude Code plugin** and, for now, **defer** a native **Codex plugin**. The split is forced by how each ecosystem's plugin manifest selects skills, against this repo's bucketed layout.

## The constraint: bucketed skills vs. single-path selection

Skills live in bucket folders under `skills/`: `engineering/` and `productivity/` were **promoted** (shipped) in that upstream investigation; `misc/`, `personal/`, `in-progress/`, and `deprecated/` are **not**. A plugin must expose only the promoted set, which spans two of those bucket folders.

- **Claude Code**: `.claude-plugin/plugin.json` accepts `skills` as an **array of explicit skill-directory paths**. We list the promoted skills one by one, exclude everything else with zero ambiguity, and add `.claude-plugin/marketplace.json` so the repo is its own single-plugin marketplace. Verified end to end: `claude plugin validate . --strict` passes, and `marketplace add` → `install` resolves all promoted skills.

- **Codex**: `.codex-plugin/plugin.json` accepts `skills` only as a **single path string** (arrays are rejected with `missing or invalid plugin.json`), and Codex discovers `SKILL.md` files recursively under it. There is no way to name two bucket folders, or to curate a subset, from one path. Two escape hatches were tested and rejected:
  - Pointing at `./skills/` would also ship `deprecated/`, `in-progress/`, `personal/`, and `misc/`: retired, draft, and personal skills we deliberately don't promote.
  - A curated flat directory of **symlinks** into the buckets does not survive install: Codex copies the plugin tree into its cache and **drops symlinks**, so the skills arrive empty.

The only robust ways to give Codex a single promoted-only path are (a) **restructure** so `skills/` contains only promoted skills (moving the non-promoted buckets out, a large blast radius across `CLAUDE.md`, `scripts/link-skills.sh`, the bucket READMEs, and the local dev workflow that relies on `in-progress/` and `personal/`), or (b) **commit duplicate copies** of promoted skills into a flat directory (a sync burden and a second source of truth). Both are structural decisions, not something to bundle into shipping the Claude plugin. This is very likely the original, half-remembered reason a plugin wasn't shipped earlier: the manifest formats didn't cleanly express a curated subset of a bucketed repo.

## Decision

- Ship the **Claude Code plugin** now (`.claude-plugin/plugin.json` + `.claude-plugin/marketplace.json`), curated to the promoted set, as the headline v1.2 deliverable.
- Keep **skills.sh** as the universal installer: it already serves Codex and other harnesses today, so no Codex user is left without an install path.
- **Defer** the native Codex plugin until we decide between restructuring `skills/` to promoted-only vs. committing a generated flat copy. Revisit when Codex either supports a `skills` array / include-list or preserves symlinks on install.

## Invariants this creates

- Every promoted skill has an entry in `.claude-plugin/plugin.json`'s `skills` array (this already stood as a `CLAUDE.md` rule; it now also gates the plugin's contents).
- `.claude-plugin/plugin.json`'s `version` tracks `package.json`'s version: bump both together on release. Claude uses the plugin `version` to decide when installed users see an update.

## Update, 2026-08-05

`mattpocock-skills` was accepted into **Claude Code's official marketplace** (configured name `claude-plugins-official`, source repo `anthropics/claude-plugins-official`), which upstream recorded as present by default. `claude plugins install mattpocock-skills` is now the documented route, and the `marketplace add` → `install` path above is superseded. The install wording lives in [.agents/install-block.md](../install-block.md).

The official listing points at upstream's git URL and reads `.claude-plugin/plugin.json` directly, so it does not depend on `.claude-plugin/marketplace.json`. That file is retained only as a fallback for installing the repo directly (an unreleased commit, or a fork).

Verified 2026-08-05, on Claude Code 2.1.222, against the live listing:

- `claude plugins install mattpocock-skills` resolves with no marketplace added first, and reports `mattpocock-skills@claude-plugins-official`.
- `claude plugin details mattpocock-skills` then reports version 1.2.0 and loads the promoted skills.
- The listing's `source` is `{"source": "url", "url": "https://github.com/mattpocock/skills.git", "sha": …}`: the **sha is pinned**, so a release reaches installed users when that pin moves, not the moment we tag. At the time of writing the pin sits two commits behind `main`, which is why it lists 22 skills rather than the 24 in `plugin.json`.
- The in-session `/plugin install mattpocock-skills` was **not** exercised: `/plugin` is unavailable in headless (`claude -p`) sessions. It runs the same resolver as the CLI, and the documented example form is `/plugin install <name>@claude-plugins-official`.

## Fork update, 2026-10-10

The fork ships its existing Claude Code plugin through repository `Hissal/mattpocock-skills-unity`, marketplace `hissal`, and plugin `mattpocock-skills-unity`. Upstream's official-marketplace instructions do not supersede this fork's marketplace route. The current promoted set spans engineering, productivity, and Unity, curated through explicit paths in `.claude-plugin/plugin.json`. Keep that manifest model without restructuring buckets or committing duplicate skill copies.

[Decision #111](https://github.com/Hissal/mattpocock-skills-unity/issues/111) selects Codex, Copilot CLI, and VS Code for direct fork verification. Each new supported route requires installation, intended invocation availability across the promoted set, exclusion of non-promoted skills, and demonstrated automatic updating. An initial install or source review alone does not satisfy that decision. Required one-time update opt-ins must be disclosed if demonstrated.

Codex 0.162.1 installed the fork's existing Claude-format marketplace and exposed its 35 declared promoted skills through `skills/list`. This supersedes the old single-path-manifest limitation for that tested installation mechanism; it does not establish complete invocation behavior or startup updates. Copilot CLI 1.0.95 installed the fork, but inherited global skills obscured plugin skill availability and its plugin loader reported a pre-existing YAML error. Neither automatic-update claim was demonstrated. VS Code 1.141.0 was identified and isolated CLI directories checked, but its installation command and daily updates were not exercised.

All three new supported-route and automatic-update claims therefore remain Unvalidated. Retain the current Claude Code route and skills.sh alternative with explicit manual update instructions. Gemini guidance is not added in this sync. If added later, it must include engineering, productivity, and Unity.

Exact commands, evidence boundaries, and reasons for deferral live in the [fork verification record](../install-routes-2026-10-10.md). Installation wording remains canonical in [install-block.md](../install-block.md).
