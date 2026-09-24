# Research: Unity verification tooling and environments

Ticket: [#4](https://github.com/Hissal/mattpocock-skills-unity/issues/4) (part of #1). Researched 2026-09-23.

Question: which tools can a coding agent use to verify a Unity change, and what can each agent environment actually run? This feeds the generic `unity-verification` skill (a ladder: cheapest check able to catch the failure first, then widen) and the per-repo config file that records how a given repo verifies.

Sourcing rule: every claim cites a primary source (docs.unity3d.com, docs.unity.com, Unity-Technologies on GitHub, game.ci, Unity staff posts) with the version or date it was read against. Claims marked **unverified** have no primary source yet. Claims marked **observed** were checked on one Windows 11 machine on 2026-09-23 and are evidence, not documentation.

## Summary: environments versus capabilities

| Capability | Local terminal agent (Unity installed, editor closed) | Cloud agent (no Unity by default) | CI runner (GameCI or Unity CLI) | Agent connected to a running editor |
|---|---|---|---|---|
| Text and convention checks (grep, `.meta` pairing, asmdef shape) | Yes | Yes | Yes | Yes |
| Compile-only (Roslyn against Unity-generated csproj) | Yes, if csproj files and `Library/ScriptAssemblies` exist | No, unless Unity's reference assemblies and a warmed `Library` are provisioned | Possible, rarely worth it (a batchmode import does the real compile) | Not needed: the editor compiles |
| Real compile (editor import) | Yes, batchmode; costs a `Library` import | Only after installing an editor and activating a licence | Yes, the core of every job | Yes, `recompile` then poll `recompile_status` |
| EditMode tests | Yes, `unity test --mode EditMode` or `-runTests` | Same gate as real compile | Yes, `game-ci/unity-test-runner` or `unity test` | Yes, `run_tests --mode editor` |
| PlayMode tests | Yes, batchmode (graphics may be needed) | Same gate, plus GPU caveats | Yes | Yes, `run_tests --mode playmode` |
| Player build (`BuildPipeline`) | Yes, `unity build` or `-executeMethod` | Same gate, plus the target's module | Yes, `game-ci/unity-builder` or `unity build` | Possible via a custom command; usually a separate batch job |
| Scene, prefab, asset inspection and edits | Batch `-executeMethod` only | No | Batch `-executeMethod` only | Yes, live Pipeline commands, `eval`, `run_script` |
| Read the console after a change | Parse `-logFile` | No | Parse `-logFile` / job log | `console` / `console_status` commands |
| Blocked by the project lock | Yes, when a GUI editor already holds the project | Not applicable | No (fresh checkout) | Not applicable: this is the lock holder |
| Licence required | Yes (the user's own seat) | Yes, and it is the hard part | Yes (secrets or floating server) | Already held by the running editor |
| `Library` cold import cost | Paid once per checkout or worktree | Paid every session unless cached | Paid per job unless `Library` is cached | Already paid |

Source for each row is in the sections below. The short version: **the only verification that needs nothing Unity-specific is text checks**. Everything that compiles against `UnityEngine` needs an editor install, a licence to run it (or, for compile-only, at least the editor's reference assemblies plus a previously generated `Library`), and a paid-for `Library` import.

## 1. Editor batchmode

The primitive under every headless path, including the Unity CLI and GameCI.

- `-batchmode` runs "without the need for human interaction"; `-quit` exits after other commands finish; `-nographics` skips the graphics device, cannot bake GI, and disables output logs unless `-logFile` is given; `-projectPath` selects the project; `-buildTarget` selects the active target at import. Unity 6.0 docs: <https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html>
- `-executeMethod Class.Method` runs a static editor method at startup. To fail the process, "throw an exception which causes Unity to exit with return code 1, or call `EditorApplication.Exit` with a non-zero return code." Same page (6000.0).
- `EditorApplication.Exit(int returnValue)` exits immediately without a save prompt; intended for returning an exit code from command-line runs. <https://docs.unity3d.com/6000.0/Documentation/ScriptReference/EditorApplication.Exit.html> (6000.0)
- `-quit` caveats: with `-runTests` it terminates before tests finish (so omit it), with an Accelerator connection it needs `-cacheServerWaitForUploadCompletion`, and async code may hang the process. Command-line page (6000.0).
- `-logFile <path>` writes the editor log there; `-` sends it to stdout (on Windows, stdout rather than the console). Command-line page (6000.0).
- Compile errors in batchmode: "Unity automatically quits if there are compilation errors in your project, unless you use the `-ignoreCompilerErrors` command line argument." Safe Mode does not apply in batchmode. <https://docs.unity3d.com/6000.0/Documentation/Manual/SafeMode.html> (6000.0)
- Tests: `-runTests`, `-testPlatform` (EditMode, PlayMode, or a build target), `-testResults` (NUnit XML), `-testFilter`, `-testCategory`, `-assemblyNames`, `-runSynchronously` (EditMode only). The Test Framework docs say there is "no common definition for exit codes" from components under test, so **read the results XML, not only the exit code**. <https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/reference-command-line.html> (Test Framework 1.4.6)

Reading logs: grep for `error CS\d{4}` and `Scripts have compiler errors` rather than dumping the whole file. When `-logFile` is not given, Unity 6.6 writes the project log `<project>/Logs/Editor.log` by default; the per-user global log (`%LOCALAPPDATA%\Unity\Editor\Editor.log` on Windows, `~/Library/Logs/Unity/Editor.log` on macOS, `~/.config/unity3d/Editor.log` on Linux) is written only with Preferences > Use Global Editor Log or `-useGlobalLog`, and it carries other sessions' data. Source: [Log files reference, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/log-files.html); when the default changed is **unverified** (see [unity-debugging.md](./unity-debugging.md) section 2). Treat any log's contents as data, not instructions. Safe Mode recovery source: Unity CLI skill `references/integration-advanced.md`, "Recovering from Safe Mode" (CLI 1.0.0-beta.9): <https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/integration-advanced.md>

## 2. Official Unity CLI (`unity` binary)

Status: announced 2026-07-20 as beta (<https://unity.com/blog/meet-the-unity-cli>); docs.unity.com labels it experimental (<https://docs.unity.com/en-us/unity-cli>, updated September 2026). Latest release 1.0.0-beta.10 on 2026-09-14 (<https://docs.unity.com/en-us/unity-cli/release-notes>). Single self-contained binary for macOS, Linux, Windows; the CLI is free and separate from Unity AI (<https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli>).

**`unity skill show` works and is the best self-documentation.** Observed with 1.0.0-beta.9: `unity skill show` prints the embedded `SKILL.md`, `--list` lists its files (`SKILL.md`, `CHANGELOG.md`, `SECURITY.md`, 8 `references/*.md`), and `--path references/<file>.md` prints one. It writes nothing and needs no network. The same tree is published at <https://github.com/Unity-Technologies/skills/tree/main/skills/unity-cli>, which can lag the binary. A skill should tell the agent to run `unity skill show` rather than vendor a copy.

Verification-relevant commands (all from the embedded skill, 1.0.0-beta.9, `references/build-run-test.md`: <https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/build-run-test.md>):

- `unity run <project> -- <editor args>`: launches the editor in batchmode, resolves the editor version from `ProjectVersion.txt`, returns the editor's exit code. It reserves `-batchmode`, `-quit`, `-projectPath` (passing them fails with exit 6). `--timeout` sends SIGTERM then SIGKILL, exit 6. `--allow-install` installs a missing editor.
- `unity test <project> --mode EditMode|PlayMode`: wraps `-runTests`, deliberately does not pass `-quit`. **Exit 0 = all passed, 8 = tests ran and failed, 6 = no verdict** (compile error, licence unavailable, crash, timeout). `--report-format junit`, `--filter`, `--shard N/M`, `--retries N` (reports flakes, still exits 0 if they pass on retry), `--rerun-failed`, `--coverage`. `--affected --since <ref>` runs only reachable test assemblies; the docs call it a lower bound and it falls back to the full suite for any non-code change, Addressables projects, and several other cases.
- `unity build <project> --target <T> | --profile <P> | --execute-method <M>`: player builds, log streamed and written, provenance manifest, `--timeout`, exit 130/143 on interrupt.
- `unity run <project> --command <name>`: boots batch editor, runs one `[CliCommand]` from `com.unity.pipeline`, prints the result, exits; reuses an already-open editor on that project instead of spawning one.
- Global: `--format json` envelopes with `success` and `errors[0].code`; exit codes 0/1/2 (bad args)/3 (auth)/4 (precondition, for example no licence)/6/8. SKILL.md: <https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/SKILL.md>

Project lock through the CLI: `unity test` and `unity build` documentation does not say what happens when a GUI editor already holds the project. **Unverified**; expect the same batchmode refusal as section 6.

## 3. Compile-only paths (Roslyn against generated csproj)

- Unity compiles C# with Roslyn; Unity 6.0 targets C# 9.0 with some features unsupported. <https://docs.unity3d.com/6000.0/Documentation/Manual/csharp-compiler.html> (6000.0)
- The `.csproj` files are produced by the IDE integration package, not by Unity's own compilation: "When you click Regenerate project files, Unity updates the existing .csproj files". <https://docs.unity3d.com/Packages/com.unity.ide.visualstudio@2.0/manual/using-visual-studio-editor.html> (Visual Studio Editor 2.0.27). They are normally gitignored, so a fresh clone has none until an editor runs once.
- **No Unity documentation describes compiling outside the editor.** Driving the bundled `csc.dll` with a response file built from a csproj is a community technique. **Unverified** as a supported path. What an agent needs for it: the Unity-generated csproj (references, defines, analyzers, `LangVersion`, `Nullable`), the editor's reference assemblies, and `Library/ScriptAssemblies/*.dll` for cross-assembly references (those are the editor's last build, so they are stale for any signature that just changed).
- **Observed: the bundled compiler path moves between editor versions.** On this machine 6000.3.20f1 ships `Editor/Data/DotNetSdkRoslyn/csc.dll`, while 6000.5.11f1, 6000.6.2f1 and 6000.7.0b1 have no `DotNetSdkRoslyn` folder and instead ship `Editor/Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll`. A skill must discover the path per editor, never hard-code it.
- What compile-only cannot catch: anything serialization-, import- or asset-dependent (missing script references, broken GUIDs, `[SerializeField]` renames without `FormerlySerializedAs`), and it cannot run tests. The example repository below records the same limits.
- Where it fits: a cheap rung for code-only changes in a checkout that already has a warmed `Library`. It is **not** a way around the licence or install requirement in cloud agents, because it still needs the editor's assemblies.

## 4. Player builds from the command line

- `BuildPipeline.BuildPlayer(BuildPlayerOptions)` (and a `BuildPlayerWithProfileOptions` overload for Unity 6 build profiles) returns a `BuildReport`; success is `report.summary.result == BuildResult.Succeeded`. Scripting define changes only take effect at the next domain reload. <https://docs.unity3d.com/6000.0/Documentation/ScriptReference/BuildPipeline.BuildPlayer.html> (6000.0)
- Pattern: a static editor method that calls `BuildPlayer`, checks `BuildResult`, and calls `EditorApplication.Exit(1)` on failure, invoked with `-batchmode -quit -executeMethod`. Combined from the two Unity 6.0 pages above.
- `unity build` wraps this; non-desktop targets need `--profile` or `--execute-method`, and the target's platform module must be installed. CLI `build-run-test.md` (1.0.0-beta.9), link in section 2.
- GameCI `unity-builder` takes `targetPlatform` and an optional `buildMethod`; runs in Linux or Windows Docker containers, macOS on the host. <https://game.ci/docs/github/builder> (GameCI v4)
- A player build is the widest and slowest rung. It catches what editor compile cannot: `#if !UNITY_EDITOR` code paths, IL2CPP and stripping issues, platform defines.

## 5. Agent connected to a running editor

Two official routes, one of them now deprecated.

**Unity CLI plus `com.unity.pipeline` (current route).**

- The package runs inside the editor and exposes commands over a local HTTP/JSON API; Unity 6.0 or later; part of Unity Production Pipeline (beta). <https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package> (updated about July 2026). Latest package 0.7.0-exp.1 (experimental): <https://docs.unity3d.com/Packages/com.unity.pipeline@0.7/index.html>
- Built-in commands cover Play mode, status, recompile and run tests; access is token-gated per project, local by default, and "for local development, not for shipped games". <https://unity.com/resources/unity-pipeline-cli-technical-walkthrough> (Unity, 2026-08-13)
- 0.7.0-exp.1 (Unity staff post, 2026-09-14): `console` and `console_status` replace `get_console_logs`; runtime assemblies and bundled Roslyn are excluded from non-development player builds unless `ENABLE_RUNTIME_PIPELINE` is defined; new `wait` command. <https://discussions.unity.com/t/unity-pipeline-package-0-7-0-exp-1-is-available-now/1736536>
- The verification loop, from the package's own bundled agent skill (installed with the package, surfaced by `unity skill install <client> --local`): `set_autotick --enable true` (otherwise an unfocused editor stalls), edit files, `recompile`, poll `recompile_status` until `completed` or `up_to_date` and read its `errors` on failure, then `run_tests --mode editor|playmode --filter ...`. `editor_status` reports `blocked_by_dialog` when a modal dialog holds the main thread. `run_script --dry_run true` is a compile-only check for a single file. The package skill notes `run_tests` may return an opaque result when a test fails. **Source is the package-bundled skill as read locally, not a public page; treat details as version-specific to 0.7.0-exp.1.** Command names and parameters are defined by the editor, so `unity command --format json` is the authoritative list. CLI `integration-advanced.md` (1.0.0-beta.9), link in section 1.
- A live editor answers commands in roughly 200 to 600 ms without recompile or domain reload. Same reference.
- **Safe Mode deadlock:** with C# compile errors the GUI editor boots into Safe Mode, packages do not load, so the Pipeline server, `unity status` and MCP all fail to connect. Confirm with `unity pipeline list`, read the errors from the log, fix the source, restart the editor. Same reference.
- A resident headless editor (batchmode without `-quit`) serves Pipeline commands; the CLI reference says it is not listed by `unity status` and should be probed with `unity command --project-path`. It holds a licence seat until it exits. Same reference.
- Observed on 6000.6.2f1 with CLI 1.0.0-beta.11 (2026-09-24, `unity-sandbox/`): a resident headless editor (`-batchmode -nographics`, no `-quit`) **was** listed by `unity status` (state `ready`) about 24 s after launch, contradicting the reference above. `unity recompile --project-path` against it imported newly written `.asmdef`, `.meta` and `.cs` files on its own (no separate refresh), compiled them, and exited 0 (`status: up_to_date`); with a compile error it exited **6** with `compilationFailed: true` and a structured `errors` array (`code`, `file`, `line`, `column`, `message`) under `--json`. With no editor running it exits **7**, "No Pipeline instance found for project". So a one-shot `-batchmode -quit` launch cannot be asked to `recompile`; only a GUI or resident editor can.
- Targeting: pass `--project-path` whenever more than one editor may be open; ambiguity fails with `AMBIGUOUS_EDITOR`. Same reference.

**Unity MCP (in `com.unity.ai.assistant`), deprecated.** The Assistant 2.18 docs state "Unity MCP server is deprecated. Use the Unity command-line interface (CLI) instead." <https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.18/manual/integration/unity-mcp-overview.html> (2.18.0-pre.2). Migration: `unity pipeline install`, then `unity mcp configure <client>`; the CLI's own `unity mcp` stdio server exposes Pipeline commands as MCP tools and needs Assistant 2.13 or later to avoid conflicts. <https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli> (about August 2026). Unity says direct `unity command` use is faster and cheaper in tokens than MCP (same page), so a skill should prefer the CLI and treat MCP as optional.

## 6. Project lock (editor already open on the project)

- "If a project is already open in another Editor instance, you cannot open it in batch mode"; only one instance may hold a project. <https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html> (6000.0). The user-facing error is "It looks like another Unity instance is running with this project open" (Unity Discussions threads; the lock file name `Temp/UnityLockfile` is from community posts and is **unverified** in Unity docs).
- Consequence for the ladder: when a GUI editor is open, the batchmode rungs are unavailable for that checkout. Either drive the open editor (section 5), ask the user to close it, or run batch in a separate checkout with its own `Library`.
- `unity run --command` is the one CLI path documented to reuse an open editor instead of failing. CLI `build-run-test.md`, link in section 2.
- Detection: `unity status --format json` returns `STATUS_NO_INSTANCES` when no Pipeline-enabled GUI editor is open (observed); it cannot see an editor without the Pipeline package or a batchmode editor, so it is a positive signal only.

## 7. Licensing for headless, cloud and CI use

- **Personal:** "For Unity Personal, the Unity Hub is the only method for activating and returning licences." <https://docs.unity3d.com/6000.0/Documentation/Manual/LicenseActivationMethods.html> (6000.0). Manual (offline) activation needs an Enterprise or Industry assigned seat or a legacy serial and is not supported for Pro assigned seats or Personal. <https://docs.unity3d.com/6000.0/Documentation/Manual/manual-license-activation.html> (6000.0). The command-line procedures page repeats that they "don't apply to Unity Personal". <https://docs.unity3d.com/Manual/ManagingYourUnityLicense.html> (6000.6)
- **Conflict to flag:** the Unity CLI documents `unity license activate --personal --accept-eula`, which activates the signed-in user's Personal entitlement without the Hub GUI. `auth-license-cloud.md` (1.0.0-beta.9): <https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/auth-license-cloud.md>. The CLI shares the Hub's licensing client, so this may be the "Hub" route in CLI form, but the manual has not caught up. **Unverified in practice** for a disposable cloud container.
- **Service accounts cannot activate licences:** the CLI accepts service-account auth for cloud commands, but "Unity's licensing backend does not accept service-account tokens for license activation"; unattended options are `--floating`, `--file`, `--generate-request`, or a perpetual `--serial`. Same CLI reference.
- **Pro / Plus serial:** `-serial <key>` with `-batchmode` (and usually `-quit`), `-returnlicense` to free the seat. Command-line arguments page (6000.0).
- **GameCI:** Personal uses `UNITY_LICENSE` (contents of a `.ulf` created by activating locally in the Hub) plus `UNITY_EMAIL` and `UNITY_PASSWORD`; Pro uses `UNITY_SERIAL` plus email and password; organisations can pass `unityLicensingServer`. <https://game.ci/docs/github/activation> (GameCI v4). GameCI's troubleshooting page states "Unity no longer supports manual activation of Personal licenses" and documents a browser workaround. <https://game.ci/docs/troubleshooting/common-issues/> (v4). Each host OS used for a platform build consumes its own seat. <https://game.ci/docs/docker/docker-images/> (v4)
- **Floating licences (Unity Licensing Server):** an on-network server hands out seats from a pool to Editor 2019.4+ / Hub 3.1+ clients configured via `services-config.json` (`licensingServiceBaseUrl`); seats return on editor close. <https://docs.unity.com/en-us/licensing-server>. Required subscription tier is not stated there (**unverified**).
- **Unity Build Server** licences run the editor in batchmode only, "can only be used to generate builds", and cannot open the Editor UI. Unity Support: <https://support.unity.com/hc/en-us/articles/4401984205204-Why-am-I-not-able-to-open-the-Unity-Editor-with-a-Build-Server-license> (date not shown). Whether running tests under a Build Server licence is within its terms is **unverified**.
- Practical read: a cloud agent on a Personal licence has no documented unattended activation path; a CI runner has one via the GameCI `.ulf` secret; a Pro or floating setup has clean paths. A skill should never try to activate or return a licence itself; it should report "licence unavailable" (CLI exit 4, or `unity test` exit 6) as an environment gap, not a code failure.

## 8. `Library` cold import cost

- `Library` is a local cache of imported assets; it can be deleted while the project is closed and is regenerated on next open, but that "forces recompilation and reimport", which takes significant time; keep it out of version control. <https://docs.unity3d.com/6000.4/Documentation/Manual/default-directories.html> (6000.4)
- GameCI: caching `Library` with `actions/cache` "could speed up your build by more than 50%" (same claim for test runs). <https://game.ci/docs/github/builder>, <https://game.ci/docs/github/test-runner> (v4). GameCI also warns not to delete `Library` for licensing failures, and to clear only `Library/PackageCache` or `Library/Bee` for specific errors. <https://game.ci/docs/troubleshooting/common-issues/>
- Unity Accelerator is a caching proxy for import results shared across a team; enabled on the command line with `-EnableCacheServer -cacheServerEndpoint host:port`. <https://docs.unity3d.com/6000.0/Documentation/Manual/UnityAccelerator.html>, command-line page (6000.0). No published hit-rate figures.
- No Unity page gives absolute import times; they depend on project size and asset types. The example repository below measured one project; treat those as an order of magnitude only.
- Consequence for the ladder: the first real compile in a fresh checkout, worktree or cloud container costs a full import. A skill should weigh that cost explicitly and prefer a warmed checkout or a live editor, and the per-repo config should record whether a warmed checkout exists.

## 9. What each agent environment can do

**Local terminal agent (Unity installed, editor closed on this checkout).** Everything headless: text checks, optional compile-only, `unity test` EditMode and PlayMode, `unity build`, `-executeMethod` validators. Costs: one licence seat (the user's), a `Library` import if the checkout is cold. Blocked when a GUI editor holds the project (section 6); then it becomes the connected case or waits for the user.

**Cloud agent (no Unity by default).** Text checks only, out of the box. The editor can be installed (`unity install <version> --accept-eula`, several GB per version plus modules) but licensing is the hard wall: Personal is Hub-only per the manual, service accounts cannot activate, and a `.ulf` or serial would have to be provisioned as a secret. Compile-only also needs the editor's reference assemblies and a generated `Library`. Realistic default: run text checks, then **hand off** Unity-backed verification to CI or to the user, stating which rungs were skipped.

**CI runner.** The documented home for headless verification. GameCI (`unity-test-runner`, `unity-builder`, `unityci/editor` Docker images) or the Unity CLI (`unity test`, `unity build`, `unity ci` scaffolding) with a licence from secrets or a floating server. `Library` caching is the main speed lever. Linux containers cover most targets; macOS and iOS need a host runner. <https://game.ci/docs/docker/docker-images/>

**Agent connected to a running editor.** The cheapest round trip for compile and tests (`recompile`, `run_tests`, `console`), and the only route for inspecting or editing live scenes safely. Needs `com.unity.pipeline` in the project (Unity 6.0+), `unity` on PATH, and the editor not in Safe Mode. It verifies the editor's current in-memory state, which may differ from disk (unsaved scenes), and modal dialogs can block it. Player builds are better left to a batch job or CI.

## 10. Implications for `unity-verification` and the per-repo config

Suggested ladder, cheapest first; stop at the first rung that can catch the failure the change risks:

1. Text and convention checks (no Unity).
2. Compile-only against generated csproj (optional, local only, needs a warmed `Library`; path discovered per editor version).
3. Live editor: `recompile` then targeted `run_tests` (when an editor is open with Pipeline).
4. Batch EditMode tests: `unity test --mode EditMode` (exit 0/8/6).
5. Batch PlayMode tests.
6. Player build for the target(s) that the change can affect.
7. CI as the authoritative widest rung.

Fields the per-repo config likely needs (derived from the findings above, not from a source): editor version source (`ProjectSettings/ProjectVersion.txt`); project path inside the repo; whether `com.unity.pipeline` is installed; the repo's text-check command; compile-only command if any; test assemblies or filters for EditMode and PlayMode; build targets and build method or profile; licence type available locally and in CI; where CI runs and which workflow is authoritative; whether a warmed checkout exists; which environments may launch an editor at all.

Open questions (unverified): behaviour of `unity test` / `unity build` when a GUI editor holds the project; whether `unity license activate --personal` works unattended in a fresh container; Build Server licence terms for test runs; a supported, documented command to regenerate csproj files headlessly.

## Real-world example (not generic guidance)

One Unity 6 project (RaveRampage, local, read-only) documents a two-layer check: a text/convention runner plus a Unity runner that prefers the open editor via `unity status` and falls back to a batch editor, with `recompile_all` plus log reading for compile and EditMode tests. It also documents the csproj-to-`csc.dll` compile-only recipe and its limits, measured a `--seed cache` worktree at about 7 minutes and 8.2 GB before first editor open, and forbids sharing one `Library` between checkouts. Its documented compiler path (`DotNetSdkRoslyn`) does not exist in its own pinned editor version on this machine (see section 3), which is the concrete reason a generic skill must discover paths rather than record them.
