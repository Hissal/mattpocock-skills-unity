# The canonical install block

One install story, one wording. `README.md`, `.changeset/*`, and every page under `docs/` must say **this** and nothing else. Change it here first, then propagate.

This fork is not in Claude Code's official marketplace. `.claude-plugin/marketplace.json` makes the repo its own single-plugin marketplace (marketplace name `hissal`, plugin name `mattpocock-skills-unity`), and that is the documented Claude Code route. Upstream's `mattpocock-skills` listing in the official marketplace is upstream's, not this fork's.

## Claude Code: the plugin

<canonical-block name="claude-code">

```bash
claude plugin marketplace add Hissal/mattpocock-skills-unity
```

```bash
claude plugin install mattpocock-skills-unity@hissal
```

Or, from inside a session:

```
/plugin marketplace add Hissal/mattpocock-skills-unity
/plugin install mattpocock-skills-unity@hissal
```

The repo is its own marketplace, so you add it once. Third-party marketplaces don't auto-update by default: `claude plugin update mattpocock-skills-unity@hissal` pulls a new version.

</canonical-block>

## Codex, and other agents: skills.sh

The plugin is Claude Code only. Everywhere else, [skills.sh](https://skills.sh) copies editable skill files into the project. Use the whole-set form on `README.md`:

<canonical-block name="skills-sh-whole-set">

```bash
npx skills@latest add Hissal/mattpocock-skills-unity
```

Pick the skills you want, and which coding agents to install them on. **The installer lets you choose which skills to take: make sure `setup-matt-pocock-skills` is one of them.**

</canonical-block>

…and the single-skill form wherever one skill is named on its own. Note that **`docs/` pages are not a consumer of this block**. See [writing-docs.md](./writing-docs.md).

<canonical-block name="skills-sh-one-skill">

```bash
npx skills@latest add Hissal/mattpocock-skills-unity --skill=<name>
```

```bash
npx skills@latest update <name>
```

</canonical-block>

`skills@latest` is the pinned spelling in all three.

## The two routes are exclusive

The plugin is a managed, read-only bundle you subscribe to. skills.sh writes files you own and edit. Installing both leaves the user with every skill twice: always say "pick one".

## Upstream and fork are exclusive too

Skill names match upstream's, so installing this fork beside upstream's `mattpocock-skills` (by either route) also leaves every skill twice. Always tell the user to uninstall upstream's copy first.
