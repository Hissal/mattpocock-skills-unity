# Coding standards

Judgement rules for a **Unity delta** (see `GLOSSARY.md`) and the files around it. `CLAUDE.md` and `npm run check` own the mechanical rules; these are the ones a reviewer has to weigh. Two companions own their own rules, and this file only points at them:

- A docs page for a skill with a Unity delta: [.agents/writing-docs.md](./.agents/writing-docs.md), under **Unity deltas**.
- A file in `research/`: [research/README.md](./research/README.md).

## The delta line

A Unity delta that reaches Unity knowledge is one sentence in the upstream skill, as its own line or a bullet in an upstream list, in this shape:

> In a Unity repo, call the Skill tool with "unity" for <need phrase>.

The need phrase may run to a short list ("for X, and for Y"), and a colon may follow it with what the skill does with the answer. A skill that briefs a sub-agent hands the same call on: it tells the sub-agent to call the Skill tool with "unity" for <need phrase>.

## The need phrase

The `unity` router routes by its needs table (`| Need | Routes to |` in [skills/unity/unity/SKILL.md](./skills/unity/unity/SKILL.md)), so a delta's need phrase reads like the row it routes through: the same need, in close to the same words. Word-for-word is not the bar, since a row is a terse table label and a delta is prose:

| Delta | Row | |
|---|---|---|
| "how often to run the tests and checks" (`implement`) | "running the tests, or how often to" | close: same need, prose form |
| "building a feedback loop for a Unity bug, instrumenting or profiling it" (`diagnosing-bugs`) | "reproducing a Unity bug or building a feedback loop for it, instrumenting or profiling it, ..." | close: the delta names part of a wider row |
| "keeping the scan fast in Unity" (hypothetical) | "keeping asset churn out of a hotspot scan" | loose: a different need the router cannot match |

A delta whose need has no row gets a new row in the same change.

## The router row

A row stays as short as its neighbours in the table: it names the need, never the delta's lists or details. Those live in the delta, or in the Unity skill the row routes to. A row can serve direct invocation with no delta behind it.

## One home per rule

Each list or rule has one home; a router row, docs page, changeset or research file that needs it points there instead of restating it.
