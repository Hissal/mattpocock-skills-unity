# Issue tracker integrations: GitHub, GitLab and local markdown only

## Fork disposition

Keep upstream's GitHub, GitLab, and local markdown seed templates and the freeform Other route. Additional trackers can be described in the consuming repo's configuration; this fork adds no first-class template for every tracker.

Approved in [fork Issue #108](https://github.com/Hissal/mattpocock-skills-unity/issues/108).

## Historical upstream record

The text below records upstream's rationale and prior requests at [49dd158](https://github.com/mattpocock/skills/blob/49dd158d1076134a641b33efb035946536778336/.out-of-scope/mainstream-issue-trackers-only.md). First-person statements and all prior-request references are upstream history. The fork disposition above governs this fork.

`setup-matt-pocock-skills` offers first-class support for exactly three issue trackers, one per seed template in `skills/engineering/setup-matt-pocock-skills/`:

- **GitHub** (`issue-tracker-github.md`, via `gh`)
- **GitLab** (`issue-tracker-gitlab.md`, via `glab`)
- **Local markdown** (`issue-tracker-local.md`, files under `.scratch/`)

Requests to add a first-class backend for any other tracker are out of scope. That includes Jira, Linear, Azure DevOps, Trello, YouTrack, and new agent-focused trackers.

## Why this is out of scope

Every issue-tracker backend hard-codes a CLI shape into the skills (commands, flags, output parsing). Each new backend is permanent maintenance surface, because it has to keep working as the tool's CLI evolves, and it has to keep being tested against `/to-spec`, `/to-tickets`, `/triage`, and friends. I use GitHub, so that's the backend I can actually keep correct. GitLab is close enough in shape to ride along, and local markdown needs no CLI at all.

Popularity doesn't change this. Jira is widely used and has an official CLI (`acli`), and it still isn't getting a template: it's a backend I don't run, with its own link semantics, workflow states and CLI quirks to keep tested. The cost lands on me whether or not the tool is mainstream.

Every other tracker goes through the **Other** option in `/setup-matt-pocock-skills`: you describe your workflow in a paragraph and the skill records it as prose in `docs/agents/issue-tracker.md`. That file is yours. If you've refined a Jira or Linear workflow that works, keep it in your own repo or publish it, and point the setup at it.

## Prior requests

- [#99](https://github.com/mattpocock/skills/issues/99): "Add dex as an issue tracker backend" (dex was ~3 months old and ~300 stars at the time of the request)
- [#258](https://github.com/mattpocock/skills/issues/258): "Add Jira as first-class issue tracker via Rovo MCP"
- [#1137](https://github.com/mattpocock/skills/issues/1137): "Add Jira as a first-class issue tracker, via Atlassian's official CLI (`acli`)"
