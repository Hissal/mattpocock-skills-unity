# Lazy-loading `writing-for-agents` in skills that judge agent-facing writing

## Fork disposition

Retain upstream's unconditional `writing-for-agents` load in `retro`, including runs that only read and report.

Approved in [fork Issue #108](https://github.com/Hissal/mattpocock-skills-unity/issues/108).

## Historical upstream record

The text below records upstream's rationale and prior requests at [49dd158](https://github.com/mattpocock/skills/blob/49dd158d1076134a641b33efb035946536778336/.out-of-scope/lazy-load-writing-for-agents.md). First-person statements and all prior-request references are upstream history. The fork disposition above governs this fork.

`retro` loads `writing-for-agents` on every run, not only before it writes. Requests to defer that load until the skill edits a skill, an `AGENTS.md`/`CLAUDE.md` or a doc are out of scope.

## Why this is out of scope

`writing-for-agents` is more than a style guide. It defines what a no-op instruction is and what makes a good skill, and `retro` uses those definitions to judge the steering files it reads, before it writes anything. A retro that only reads and reports still needs it. Load it lazily and the No-ops and Skills candidates get judged with no yardstick.

## Prior requests

- [#1238](https://github.com/mattpocock/skills/issues/1238): "retro: load writing-for-agents only before writing" (closed)
