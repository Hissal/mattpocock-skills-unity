# Matt Pocock Skills

A collection of agent skills (slash commands and behaviors) loaded by Claude Code. Skills are organized into buckets and consumed by per-repo configuration emitted by `/setup-matt-pocock-skills`.

## Language

**Issue tracker**:
The tool that hosts a repo's issues: GitHub Issues, Linear, a local `.scratch/` markdown convention, or similar. Skills like `to-tickets`, `to-spec`, and `triage` read from and write to it.
_Avoid_: backlog manager, backlog backend, issue host

**Issue**:
A single tracked unit of work inside an **Issue tracker**: a bug, task, spec, or slice produced by `to-tickets`.
_Avoid_: ticket (use only when quoting external systems that call them tickets, or for a **Decision ticket**, see below)

**Decision ticket**:
A `wayfinder` unit: a child **Issue** of a `wayfinder:map` holding a *question* whose resolution is a decision, not a slice of a build to execute. The **decision** qualifier is what keeps it distinct from an implementation ticket; `wayfinder` introduces the term, then uses "ticket".

**Triage role**:
A canonical state-machine label applied to an **Issue** during triage (e.g. `needs-triage`, `ready-for-afk`). Each role maps to a real label string in the **Issue tracker** via `docs/agents/triage-labels.md`.

**Upstream skill**:
A skill that also exists in `mattpocock/skills`. Its Unity changes stay small edits in place; anything larger moves into a **Unity skill** it points at.
_Avoid_: original, base skill

**Unity repo**:
A repo holding a Unity project or a UPM package: the only kind of repo the Unity changes apply to.
_Avoid_: Unity project (that is one of the two shapes, not the whole)

**Unity skill**:
A skill under `skills/unity/` that teaches one area of Unity mechanics (serialization, testing, ...), true for any Unity repo. It never holds facts about a specific repo.
_Avoid_: Unity primitive, Unity module

**Unity router**:
The `unity` skill: the one entry point **Upstream skills** call for Unity knowledge. It routes to the right **Unity skill** and to the repo's **Unity config**.

**Unity config**:
`docs/agents/unity.md` in a consuming repo, written by setup: how *this* repo compiles, tests, and validates, and where its own Unity conventions live.
_Avoid_: unity setup, discovery

## Relationships

- An **Issue tracker** holds many **Issues**
- An **Issue** carries one **Triage role** at a time
- A **Decision ticket** is an **Issue** (a child of a `wayfinder:map`)
- An **Upstream skill** reaches Unity knowledge only through the **Unity router**
- The **Unity router** routes to **Unity skills** for mechanics; it holds no mechanics itself
- The **Unity router** and each **Unity skill** read the **Unity config** for repo facts, falling back to generic defaults when it is absent

## Flagged ambiguities

- "backlog" was previously used to mean both the *tool* hosting issues and the *body of work* inside it. Resolved: the tool is the **Issue tracker**; "backlog" is no longer used as a domain term.
- "backlog backend" / "backlog manager". Resolved: collapsed into **Issue tracker**.
