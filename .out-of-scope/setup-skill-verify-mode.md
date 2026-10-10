# Verify/Check Mode for `setup-matt-pocock-skills`

## Fork disposition

Use the existing setup invocation with a natural-language verification request instead of introducing a separate flag or skill.

Approved in [fork Issue #108](https://github.com/Hissal/mattpocock-skills-unity/issues/108).

## Historical upstream record

The text below records upstream's rationale and prior requests at [49dd158](https://github.com/mattpocock/skills/blob/49dd158d1076134a641b33efb035946536778336/.out-of-scope/setup-skill-verify-mode.md). First-person statements and all prior-request references are upstream history. The fork disposition above governs this fork.

This project will not add a dedicated verify/check mode (or a separate verify skill) for `setup-matt-pocock-skills`.

## Why this is out of scope

A second skill (or a `--verify` flag) for checking whether `docs/agents/*.md` artifacts still match the seed-template schema would duplicate work the existing setup skill already handles in conversation.

The intended workflow is: **run `/setup-matt-pocock-skills` and tell it to verify your current setup.** The skill is prompt-driven, so the maintainer can scope it to a verification pass ("don't rewrite anything, just check my existing files against the current seed templates and report drift") without needing a separate code path. Adding a flag or a sibling skill would split the surface area of a feature that's already expressible through the natural-language entry point.

Keeping configuration management to a single skill also avoids the maintenance cost of two skills drifting from each other when seed templates evolve.

## Prior requests

- [#106](https://github.com/mattpocock/skills/issues/106): Feature request: verify/check mode for setup-matt-pocock-skills
