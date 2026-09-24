# Environments

What each environment can run, how to tell which one you are in, and the traps of each.

## Capabilities

| Rung | Connected editor | Resident headless | Headless CLI (editor closed) | Cloud agent (no Unity) | CI runner |
|---|---|---|---|---|---|
| Text checks | Yes | Yes | Yes | Yes | Yes |
| Compile | `unity recompile` | `unity recompile` | Folded into the test or build run | No | Yes |
| Targeted tests, full EditMode | `run_tests` | `run_tests` | `unity test` | No | Yes |
| PlayMode | `run_tests` in PlayMode | `run_tests`; without a graphics device, rendering-dependent tests may fail | `unity test` | No | Yes, GPU caveats |
| Player build | Possible; a batch job is usual | Possible | `unity build` | No | Yes |
| Running C# in the Editor | `run_script`, `eval` | `run_script`, `eval` | `-executeMethod` | No | `-executeMethod` |
| Costs | Nothing extra | A licence seat while it lives | Editor start per run (about a minute on a tiny warm project), a licence seat | Installing an editor plus a licence, which Personal cannot do unattended | The repo's CI minutes |

A connected editor verifies its in-memory state, which can differ from disk (unsaved scenes), and a modal dialog blocks it.

## Detecting the environment

1. **Is an editor connected?** `unity status --format json` lists every editor running Pipeline, GUI or resident headless, with its project path and `state`. `STATUS_NO_INSTANCES` (exit 6) means none. It cannot see an editor without Pipeline, so it is a positive signal only: an editor may still hold the project.
2. **Is the project locked?** A headless run against a project an editor holds refuses in seconds with exit 6 and "already open in a running Editor (PID n)", and changes nothing. That refusal is the lock check: drive the open editor instead (install Pipeline if it lacks it) or hand off.
3. **Can an editor launch here?** `unity` on PATH, the project's editor version installed (`ProjectSettings/ProjectVersion.txt`), the Unity config's allowed environments permitting it, and an active licence (`unity license` lists them). Any missing piece is an environment gap: text checks and hand-off.

Pass `--project-path` on every Pipeline command whenever more than one editor may be open; per the CLI reference, an ambiguous target fails with `AMBIGUOUS_EDITOR`.

## The Pipeline nudge

An editor open on the project without `com.unity.pipeline` forces a hand-off for every Unity rung. Say once per conversation that `unity pipeline install` unlocks the connected route. Installing it is the user's decision: it adds a package to their manifest.

## Safe Mode deadlock

Per Unity's CLI reference (not reproduced here), a GUI editor that opens with C# compile errors boots into Safe Mode: packages do not load, so Pipeline, `unity status` and every connected command fail to connect. Recognise it by the editor process running while `unity status` finds nothing. Recover by reading the `error CS` lines from the Editor log (see [READING-RESULTS.md](READING-RESULTS.md)), fixing the source, and asking the user to restart the editor or leave Safe Mode. Batch mode never enters Safe Mode; it exits on compile errors instead.

## Resident headless editor

Only when the Unity config allows it. Launch the editor binary in batch mode without `-quit` on a project no editor holds (`unity editors --installed --json` gives each editor's binary path). It appears in `unity status` once ready and then serves the same commands as a connected editor.

- Stop it with `EditorApplication.Exit(0)` run through `run_script` (see [RUNNING-CSHARP.md](RUNNING-CSHARP.md)). The call that stops it reports `COMMAND_FAILED` ("Invalid response format") because the editor quits before replying; confirm the stop with `unity status`.
- Name it in the report, and ask before stopping one you did not start.

## Cloud agents and licences

A cloud agent without Unity stops at text checks and hands off. Installing an editor there does not help without a licence: Personal has no documented unattended activation, and service accounts cannot activate. The agent never activates, returns or provisions a licence. A licence failure (CLI exit 4, or exit 6 when the editor could not acquire a licence) is reported as an environment gap.
