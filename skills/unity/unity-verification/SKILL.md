---
name: unity-verification
description: Unity verification rules. Use when checking that a Unity change compiles or works, running Unity tests, building a player, running C# in the Editor, or when Unity cannot run here or the editor is locked.
---

# Unity verification

This repo's Unity config (`docs/agents/unity.md`), if present, overrides these defaults.

This skill decides which check runs, where, and how to read its result. What to test is `unity-testing`'s call; this skill runs it. For `unity` CLI syntax, run `unity skill show` and `unity <command> --help`: the CLI is beta and moves every few weeks, so the commands named in this skill mark the traps, and their current form comes from the CLI.

## The Verification ladder

1. Text checks
2. Compile
3. Targeted tests
4. Full EditMode suite
5. PlayMode
6. Player build
7. CI

**Rung rule:** find the lowest rung that can catch the failure the change risks, run every cheaper rung before it as a gate, and stop there. A change is verified up to the highest rung it passed.

- **Compile** is a gate of each batch run, not a separate recurring check: a headless test run compiles first and fails on a compile error. With a connected editor, `unity recompile` is cheap enough to run more often.
- **Player build** runs only when the change can break a build-only path (`#if` and `UNITY_EDITOR` branches, `UnityEditor` in a runtime assembly, stripping or IL2CPP, platform defines), or when the user asks.
- **Rung commands**: when the Unity config maps a rung to a repo command, run that command for the rung instead of the generic CLI path. A rung with no command falls back to the default. A compile-only (Roslyn) check is not a default rung; it runs only when a rung command names one.

## Where Unity runs

Take the first environment available, in this order. [ENVIRONMENTS.md](ENVIRONMENTS.md) has detection, the capability table and the Safe Mode deadlock.

1. **Connected editor**: an open editor with `com.unity.pipeline`, driven through `unity recompile` and `unity command --project-path <project>`. Fastest, and it holds the project lock, so it is the only route while the user's editor is open. Unity MCP is deprecated in favour of this CLI route; use it only where the CLI cannot run.
2. **Headless**: `unity test`, `unity build`, `unity run`, when an editor is installed, no editor holds the project, the Unity config's allowed environments permit launching one, and a licence is available.
3. **Neither**: run the text checks, then hand off: name what the user or CI should run.

Hard rules, in every environment:

- Leave the user's editor running. Only the user closes it.
- Launch batch mode only against a project no editor holds. A project that is already open means driving that editor, or handing off.
- Leave licences alone. A licence failure is an environment gap, reported as one, never a code failure; the user activates or returns licences.

An open editor without Pipeline gets one mention per conversation that `unity pipeline install` unlocks the connected route, then carries on down the list.

**Resident headless editor** (batch mode without `-quit`, serving Pipeline commands): only when the Unity config allows it. Otherwise each headless check is one CLI launch. When you start one, name it in the report and stop it when done; ask before stopping one you did not start.

**Cold import**: before the first run in a checkout with no `Library/`, ask the user once (a full import can take many minutes), unless the Unity config says cold imports are fine. Each checkout keeps its own `Library/`, never a shared one.

## Reading results

A result is green only when it names a nonzero test count and zero failures, from a compile that finished clean. Exit codes and envelopes lie in both directions: `unity test` exits 0 when a filter matched nothing, a connected `run_tests` exits 0 on a failing test, and a connected run started before its compile finished tests the old code. Read [READING-RESULTS.md](READING-RESULTS.md) before trusting any Unity result.

Running C# in the Editor (a validation script, a probe, a scope script another Unity skill ships): see [RUNNING-CSHARP.md](RUNNING-CSHARP.md).

## CI

Leave pushing and triggering CI to the user. Name the rungs CI would cover and which workflow runs them (the Unity config's authoritative CI workflow, else "CI, if the repo has one"); those rungs stay unvalidated. If the user already pushed, read the run's result from the CI host (`gh run view` on GitHub Actions) and report it as that rung.

## Report

Every verification ends with a report:

- **Ran**: each rung run, with the environment and the result (for tests, the counts).
- **Skipped**: each rung the risk needed that did not run, with the reason: an environment gap (no editor, locked project, no licence, cold import declined, CI not triggered) or not needed for this risk.
- **Unvalidated**: say "verified up to rung N", then list everything the risk needed above N as unvalidated.
- **Manual checks**: what no rung can judge (feel, visuals, audio, timing), as concrete steps for the user.

## Slicing work into tickets

A ticket's acceptance criteria name the highest rung it needs and whether that rung needs a connected editor. What runs where: text checks anywhere, including a cloud agent; compile and tests with a local editor, headless or connected; driving live scenes only with a connected editor; judging feel, visuals or a device only with a human (those tickets take the `ready-for-human` triage role). An agent claiming a ticket first checks it can reach the named rung, and passes the ticket up if it cannot, rather than claiming it done.
