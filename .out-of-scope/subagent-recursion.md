# Guards against sub-agents recursing into skills

## Fork disposition

Follow upstream's reliance on harness recursion controls instead of adding per-skill depth or leaf guards.

Approved in [fork Issue #108](https://github.com/Hissal/mattpocock-skills-unity/issues/108).

## Historical upstream record

The text below records upstream's rationale and prior requests at [49dd158](https://github.com/mattpocock/skills/blob/49dd158d1076134a641b33efb035946536778336/.out-of-scope/subagent-recursion.md). First-person statements and all prior-request references are upstream history. The fork disposition above governs this fork.

Skills do not add guards against sub-agents recursively invoking skills or spawning nested agents (a `/code-review` sub-agent re-invoking `/code-review`, a `/research` agent spawning another agent). Requests to add leaf-agent instructions, depth limits or "do not invoke this skill" lines are out of scope.

## Why this is out of scope

The harness should be responsible for stopping infinite subagent loops, not skills. The harness decides which tools and skills a sub-agent can reach and how deep nesting can go, so a limit set there holds for every skill at once. A guard written into one skill's prose covers only that skill, costs tokens on every run, and still depends on the model choosing to obey it.

If a sub-agent recurses or fans out without bound, report it to the harness.

## Prior requests

- [#530](https://github.com/mattpocock/skills/issues/530): "research skill: spawned background agent can recursively re-spawn another agent (uncontrolled nesting)"
- [#573](https://github.com/mattpocock/skills/issues/573): "code-review sub-agents recursively invoke /code-review and fan out" (PR [#1186](https://github.com/mattpocock/skills/issues/1186), "code-review: make the Standards and Spec sub-agents leaf reviewers", closed)
