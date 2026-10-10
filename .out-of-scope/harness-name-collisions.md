# Renaming skills to dodge harness built-in commands

## Fork disposition

Follow upstream's namespaced invocation approach instead of renaming shared skills or adding aliases for collisions. This fork's Claude Code plugin uses `/mattpocock-skills-unity:research` and `/mattpocock-skills-unity:code-review`.

Approved in [fork Issue #108](https://github.com/Hissal/mattpocock-skills-unity/issues/108).

## Historical upstream record

The text below records upstream's rationale and prior requests at [49dd158](https://github.com/mattpocock/skills/blob/49dd158d1076134a641b33efb035946536778336/.out-of-scope/harness-name-collisions.md). First-person statements and all prior-request references are upstream history. The fork disposition above governs this fork.

Skill names are not changed because a harness ships a built-in command or skill with the same name (`/prototype`, `/research`, `/code-review`, and so on). Requests to rename a skill, or add an alias, to avoid a collision are out of scope.

## Why this is out of scope

Harnesses add built-ins all the time, and each harness picks its own names. Renaming every time one of them lands on a name we already use would mean chasing a moving target across Claude Code, Copilot, Devin and the rest, and breaking every user's muscle memory and every cross-reference between skills each time.

How a harness resolves two things with the same name is the harness's behaviour, not the skill's. The skill text is correct either way.

The way through is namespaced invocation. The Claude Code plugin exposes every skill under its plugin name, so you can always reach ours explicitly:

```
/mattpocock-skills:research
/mattpocock-skills:code-review
```

If you installed with skills.sh, the files are yours: rename the folder and the `name:` field in your copy.

If a harness gives you no way to reach a namespaced or renamed skill at all, report that to the harness.

## Prior requests

- [#423](https://github.com/mattpocock/skills/issues/423): "/code-review shadows Claude Code own review skill"
- [#483](https://github.com/mattpocock/skills/issues/483): "/code-review name clash with Claude Code built-in"
- [#857](https://github.com/mattpocock/skills/issues/857): "Rename the research skill to avoid conflict with Copilot's built-in /research command"
- [#1019](https://github.com/mattpocock/skills/issues/1019): "Shorthand `/prototype` conflicts with hidden new Claude Code built-in skill `/prototype`"
- [#1037](https://github.com/mattpocock/skills/issues/1037): "Skill name overlap with devin cli inbuilt commands"
