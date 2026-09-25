# Research: Unity debugging mechanics

Ticket: [#23](https://github.com/Hissal/mattpocock-skills-unity/issues/23), part of [#1](https://github.com/Hissal/mattpocock-skills-unity/issues/1). Feeds `unity-debugging` and the Unity delta of `diagnosing-bugs` ([#16](https://github.com/Hissal/mattpocock-skills-unity/issues/16)). Researched 2026-09-24.

Sourcing rule: every claim cites a primary source (docs.unity3d.com, docs.unity.com, the CLI's embedded skill, IDE vendors' own docs) with the version or date read against. **Observed** means checked on one Windows 11 machine on 2026-09-24 with Unity 6000.6.2f1, Unity CLI 1.0.0-beta.11 and `com.unity.pipeline` 0.7.0-exp.1, in the gitignored `unity-sandbox/` (URP blank, Mono backend); it is evidence, not documentation. **Unverified** means neither documented nor observed.

Already recorded elsewhere, cited rather than repeated: batchmode flags, `-logFile`, the project lock, the connected-editor verification loop (`set_autotick`, `recompile`, `recompile_status`, `run_tests`), Safe Mode deadlock and CLI exit codes are in [unity-verification.md](./unity-verification.md) sections 1, 2, 5 and 6. Test-side logging (`LogAssert`, a logged error failing a test) and the Performance Testing package are in [unity-testing.md](./unity-testing.md). Domain reload, Enter Play Mode options and static reset attributes are in [unity-assemblies.md](./unity-assemblies.md) section 8. `.meta` GUIDs, `FormerlySerializedAs` and missing scripts as a serialization matter are in [unity-serialization.md](./unity-serialization.md) sections 1 and 3.

## Summary: what an agent most needs

1. **A connected editor can run the whole reproduce loop without a human.** `editor_play`, `editor_pause` (a toggle), `editor_stop`, `editor_status` (`playMode: stopped | playing | paused`), `wait_for` on a static or instance member, `console` / `console_status` for logs with stack traces, and `eval` / `run_script` against live Play-mode state. There is no step command; `eval "UnityEditor.EditorApplication.Step()"` single-steps a paused editor (observed).
2. **A headless editor can enter Play mode.** A resident `-batchmode -nographics` editor entered Play mode, ticked frames, ran `Start`/`Update`, and served every command above (observed). It cannot capture the Game view ("No GPU available"), frames run uncapped (about 7,000 per second on an empty scene), and entering Play mode can log "No graphic device is available..." errors that are noise, not the bug.
3. **The ticket's `wait` is really `wait_for`** (plus `wait_status`, `wait_cancel`). A synchronous `wait_for` holds the command queue, so use `async=true` whenever the condition depends on another command.
4. **Release players still log `Debug.Log`, but without file and line.** A Development Build adds file:line to managed stack traces and a stack trace on every `Debug.Log`; Script Debugging additionally turns off inlining so every frame appears (all observed on Mono). Asserts vanish from release builds (`[Conditional("UNITY_ASSERTIONS")]`).
5. **Unity 6.6 split "Development Build" from managed code variants**, and its own pages disagree on what defines `DEBUG` (section 3). Test `Debug.isDebugBuild` at runtime rather than trusting a define.
6. **An agent cannot drive an IDE debugger unattended.** Rider, Visual Studio and VS Code all attach through their GUI. What an agent can do: switch code optimization, launch with `-wait-for-managed-debugger` only when a human will attach, read the debugger port from a development player's log, and prefer logs, `wait_for` and `eval` for unattended work.
7. **An unfocused GUI editor stalls Play mode at frame 1** even with `set_autotick` on; `editor_pause` plus `EditorApplication.Step()` in one `eval` advances frames regardless (section 7).
8. **Editor timings are not Player timings.** Play mode shares the Editor's main thread, runs Editor-only checks, and in Debug code optimization runs slower. Use `ProfilerRecorder` or `Stopwatch` for relative comparisons, and a player build for absolute numbers.

## 1. Connected editor: Play-mode control and observation

Command names and parameters below are from `unity command --format json` against a live 6000.6.2f1 editor with `com.unity.pipeline` 0.7.0-exp.1 (151 commands; the server reported its own version as `0.0.1`). Descriptions are cross-checked against the package manual: [Editor lifecycle & observability commands](https://docs.unity3d.com/Packages/com.unity.pipeline@0.7/manual/commands/editor-lifecycle-and-observability.html), [scripts](https://docs.unity3d.com/Packages/com.unity.pipeline@0.7/manual/commands/scripts.html), [runtime](https://docs.unity3d.com/Packages/com.unity.pipeline@0.7/manual/commands/runtime.html) (0.7, read 2026-09-24). The manual's lifecycle page does not list `console`, `console_status` or `wait_for`; the live catalog is the authority, as [unity-verification.md](./unity-verification.md) section 5 already says.

| Command | Parameters | Returns / notes |
|---|---|---|
| `editor_play` | none | `"Entered play mode"`, about 0.7 s round trip on an empty project (observed) |
| `editor_pause` | none | Toggle: `"Play mode paused"` / `"Play mode unpaused"` |
| `editor_stop` | none | `"Exited play mode"` |
| `editor_status` | none | `{status, compiling, domainReloadInProgress, playMode, lastHeartbeat, projectPath, unityVersion}`; `status` is `ready` or `playing`, `playMode` is `stopped`, `playing` or `paused` (observed) |
| `wait_for` | `condition` `{member, target?, findType?, op, value}` with `op` in `equals, notEquals, greaterThan, lessThan, contains, changed`; `timeout_s` (30, max 600); `poll_interval_ms` (100); `on_met` `{capture, pause}`; `return_history`; `tolerate_missing`; `async` | Result has `state`, `met`, `timedOut`, `initialValue`, `elapsedMs`, `framesObserved`. `on_met.pause` pauses in the same frame the condition first holds |
| `wait_status` / `wait_cancel` | `wait_id` | For `async=true` waits |
| `console` | `tail` (100), `level` (`log`, `warn`, `error`, a minimum), `since` (cursor), `since_session` | Entries with `seq`, `timestampUtc`, `level`, `logType` (`Log`, `Warning`, `Error`, `Exception`, `Assert`), `message`, `stackTrace`; plus `cursor`, `session`, `counts`, `groundTruth` |
| `console_status` | none | Counts, cursor and `compilationFailed` without pulling entries |
| `clear_console` | none | Clears both the captured buffer and the Editor console |
| `eval` / `eval_file` | `code` or `file`, `timeout` (5000 ms) | Roslyn-compiled C# on the main thread; result has `result`, `output`, `diagnostics` |
| `run_script` | `file`, `entry`, `args`, `mode` (`ephemeral` or `hotpatch`), `defines`, `pdb`, `timeout_ms` (30000), `dry_run` | Compiles a `.cs` in memory with no domain reload; the file may live outside `Assets/` so writing it triggers no import. `pdb=true` emits a portable PDB "so breakpoints bind and exception stack traces map to file:line" |
| `get_performance_stats` | none | `render` (draw calls, batches, triangles), `memory` (allocated, reserved, Mono heap), `frameTiming` (`cpuFrameTimeMs`, `gpuFrameTimeMs`, `cpuMainThreadFrameTimeMs`) |
| `capture_game_view` / `screenshot` | size, camera, `source` (`camera` or `screen`) | Fails in batchmode: "No GPU available (batchmode/headless); cannot capture." (observed) |
| `set_autotick` | `enable`, `interval_ms` (16), `persist` | Keeps an unfocused GUI editor ticking; see [unity-verification.md](./unity-verification.md) section 5 |

`editor_status` and the Play-mode commands require the main thread (`mainThreadRequired: true` in the schema); `console`, `console_status` and `audit_status` do not. Per the Unity manual, an editor "becomes unresponsive at breakpoints" when an IDE debugger is attached ([Debug C# code in Unity, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/managed-code-debugging.html)), so main-thread commands will stall while a human sits at a breakpoint; whether `console` still answers then is **unverified**.

`eval`, `eval_file` and `run_script` share one capability tag (`scripts/eval`), and `run_script` "is disabled wherever the eval family is disabled" ([scripts, 0.7](https://docs.unity3d.com/Packages/com.unity.pipeline@0.7/manual/commands/scripts.html)). Every eval is logged locally to `<project>/Library/Pipeline/eval-usage.jsonl` (API fingerprints only; source only if opted in) ([lifecycle page, 0.7](https://docs.unity3d.com/Packages/com.unity.pipeline@0.7/manual/commands/editor-lifecycle-and-observability.html)).

### Observed Play-mode loop (headless resident editor)

Setup: `Unity.exe -batchmode -nographics -projectPath unity-sandbox -logFile <file>` with no `-quit`; `unity status` listed it as `ready` (matching the observation already in [unity-verification.md](./unity-verification.md) section 5). Enter Play Mode options in the sandbox were `DisableDomainReload | DisableSceneReload`.

- `editor_play`, then `eval` returned `Application.isPlaying == true` and `Time.frameCount` advancing by about 25,000 frames in 3.5 s. Headless Play mode is not frame-limited, so frame-count-based logic runs far faster than on a device.
- `eval` can reach `Assembly-CSharp` types and create objects in Play mode: `new GameObject("Probe").AddComponent<DebugProbe>()` ran the component's `Start` and `Update`.
- `wait_for` on the static `DebugProbe.Fired` returned `met` with `elapsedMs: 7`.
- `console --level warn` returned the `Debug.LogError` and the thrown exception, each with a `stackTrace` naming `Assets/DebugProbe/DebugProbe.cs:21` and `:22`. An exception thrown in `Update` did not stop Play mode.
- `editor_pause` froze `Time.frameCount`; `eval "UnityEditor.EditorApplication.Step(); ..."` advanced exactly one frame while paused; `editor_status` reported `playMode: paused`.
- `editor_stop` destroyed objects created in Play mode (`GameObject.Find("Probe") == null` afterwards) but statics survived: on the next Play session `StaticCounter` was 2 and a one-shot `Fired` flag was still `true`, so the error never reproduced a second time. This is the second-Play trap from [unity-assemblies.md](./unity-assemblies.md) section 8, seen live.
- After Play-mode entries the console held `No graphic device is available to initialize the view.` and `...to show the window.` at **error** level. An agent filtering `--level error` in a `-nographics` editor must discount these.

### Player-side Pipeline

The runtime server "only exists in development builds"; its assemblies carry the define constraint `UNITY_EDITOR || DEVELOPMENT_BUILD || ENABLE_RUNTIME_PIPELINE`, and it also needs `enableInBuilds` in Project Settings > Pipeline > Runtime (default `false`, observed via `get_runtime_pipeline_settings`). When running it binds a port in 7900 to 7949 and requires a bearer token from `.unity-pipeline-runtime-port`; target it with `unity command <name> --runtime <pattern>`. [Runtime setup, 0.7](https://docs.unity3d.com/Packages/com.unity.pipeline@0.7/manual/runtime-setup.html); CLI `integration-advanced.md` (1.0.0-beta.11, `unity skill show --path references/integration-advanced.md`). Driving a player this way was **not** tried here.

## 2. Log files

Locations from [Log files reference, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/log-files.html):

| Log | Location |
|---|---|
| Editor (default in 6.6) | `<project>/Logs/Editor.log`, project-specific |
| Editor, global (only with Preferences > Use Global Editor Log or `-useGlobalLog`) | Windows `%LOCALAPPDATA%\Unity\Editor\Editor.log`; macOS `~/Library/Logs/Unity/Editor.log`; Linux `~/.config/unity3d/Editor.log` |
| Editor, JSON Lines (opt-in logging framework) | `<project>/Logs/Editor.jsonl` |
| Player, Windows | `%USERPROFILE%\AppData\LocalLow\CompanyName\ProductName\Player.log` |
| Player, macOS | `~/Library/Logs/Company Name/Product Name/Player.log` |
| Player, Linux | `~/.config/unity3d/CompanyName/ProductName/Player.log` |
| Player, UWP | `%USERPROFILE%\AppData\Local\Packages\<productname>\TempState\UnityPlayer.log` |
| Player, Android / iOS / Web | logcat / Xcode device console / browser JavaScript console (no file) |
| Package Manager | `<project>/Logs/upm.log` |
| Editor crashes, Windows | `%TMP%\Unity\Editor\Crashes` |

- In 6.6 the manual says the Editor writes the project log by default and the global log only on request; [unity-verification.md](./unity-verification.md) section 1 was corrected to match. The page for 6000.0 was not compared (**unverified** when the default changed).
- `-logFile <path>` overrides both; `Application.consoleLogPath` returns the path in use, or an empty string where the platform has no log file ([consoleLogPath, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Application-consoleLogPath.html)). Observed: in a resident editor started with `-logFile`, `consoleLogPath` returned that file.
- `PlayerSettings.usePlayerLog` ("Write a log file with debugging information") controls whether a player writes its log at all ([usePlayerLog, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/PlayerSettings-usePlayerLog.html)); it was on in the sandbox.
- Observed, Windows release player: `probe.exe -batchmode -nographics` with no `-logFile` wrote the log to **stdout** and created no `Player.log`, and `consoleLogPath` was empty. With `-logFile <path>` the log went to that file. So when an agent launches a player headless, pass `-logFile` explicitly. The default `Player.log` path was observed later with a plain windowed launch (section 7, check 5); a windowed `-nographics` launch had hung before logging.
- Console > Open Player Log / Open Editor Log exist in the GUI ([log files reference, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/log-files.html)).

## 3. What Development Build and Script Debugging add

Documented (6000.6):

- **Development Build** includes the Profiler and assertions and defines `DEVELOPMENT_BUILD`, which "is marked for deprecation"; Autoconnect Profiler and Deep Profiling need it. **Script Debugging** "Includes debug symbols in managed assemblies, generates unoptimized code, and defines the `DEBUG` scripting symbol", equivalent to the Debug managed code variant. **Wait for Managed Debugger** makes the player wait "before it executes any script code". [Build Profiles reference, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/build-profiles-reference.html)
- **Managed code variants are new in 6.6**: Release (no symbols), Instrumented (`UNITY_INCLUDE_INSTRUMENTATION`, `ENABLE_PROFILER`), Checked (adds `UNITY_ASSERTIONS`, `UNITY_ENABLE_CHECKS`), Debug (adds `DEBUG`, code optimization off). "In Unity versions before 6.6, selecting the Development Build option ... produced a Player build that included the equivalent of the Checked managed code variant. This is no longer the case." Set via Player > Other Settings > Managed Code Variant or `PlayerSettings.SetManagedCodeVariant`. [Adding diagnostics to C# code, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/managed-code-variants.html)
- **Conflict to flag:** the [Scripting symbol reference, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/scripting-symbol-reference.html) says `#if DEBUG` "is equivalent to `#if UNITY_EDITOR || DEVELOPMENT_BUILD`", which contradicts the variants page. The Pipeline runtime docs follow the first reading ("DEBUG is defined for standalone Development Builds only"). Observed below: a Development Build without Script Debugging **did** define `DEBUG`, matching the symbol reference.
- `Debug.isDebugBuild` is true in development players and "always returns true" in the Editor ([isDebugBuild, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Debug-isDebugBuild.html)). The symbol reference recommends it over `DEVELOPMENT_BUILD`, because a Windows build can switch development mode by swapping `UnityPlayer.dll` without recompiling scripts.
- Stack trace logging is per log type (None, ScriptOnly default, Full), set in the Console menu, Player > Other Settings > Stack Trace, or `Application.SetStackTraceLogType`; changes need a rebuild to reach a player, and Unity advises not to ship with it on. [Stack trace logging, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/stack-trace.html)
- IL2CPP: Debug C++ configuration gives complete managed stacks but no line numbers; Release/Master may drop inlined frames. Setting **IL2CPP Stacktrace Information** to "Method Name, File Name, and Line Number" restores file:line in any configuration when `.pdb` files exist; Script Debugging also does, at a size and speed cost. [IL2CPP managed stack traces, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/il2cpp-managed-stack-traces.html)

Observed, Windows standalone, Mono, 6000.6.2f1, one probe scene built three ways through the Pipeline `build` command (`BuildOptions`, not a Build Profile; the project's Managed Code Variant was Release):

| | Release | `Development` | `Development` + `AllowDebugging` |
|---|---|---|---|
| Build time (warm) | 96 s (first) | about 35 s | 35 s |
| `Debug.isDebugBuild` | false | true | true |
| Defines seen by the script | none of the probed | `DEVELOPMENT_BUILD DEBUG UNITY_ASSERTIONS` | same |
| `Debug.Log` in the log | yes, message only | yes, with managed stack trace | same |
| Exception stack | `at PlayerProbe.Update () [0x00014] in <9bdee7...>:0` (no file, no line, inlined caller missing) | file and line, inlined `Nested()` frame missing | file and line, every frame present |
| `Assert.IsTrue(false)` | nothing (compiled out) | `AssertionException` logged with file:line | same |
| Debugger | none | none | "Starting managed debugger on port 56428" plus the multicast target line in the log |
| Unassigned serialized field | `NullReferenceException` | `NullReferenceException` | same |

## 4. Attaching a managed debugger

Documented:

- Editor: configure the IDE, set code optimization to Debug, attach, then enter Play mode. Debug "Allows attaching to an external debugger, but code runs slower in Play mode"; Release is faster but cannot be attached. Switch via the status-bar bug icon, Preferences > General > Code Optimization On Startup, the `CompilationPipeline.codeOptimization` API, or `-debugCodeOptimization` / `-releaseCodeOptimization` on the command line. `-wait-for-managed-debugger` makes a batchmode Editor or Player wait for a debugger before running. Works on Mono and IL2CPP, all platforms except Web. [Debug C# code in Unity, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/managed-code-debugging.html)
- Player: Development Build plus Script Debugging, then attach by IP and port. The IDE discovers instances the same way the Profiler does; the player log contains a `Multi-casting "[IP] ... [Port] ... [Debug] 1 ..."` line naming the address to use. Debug info comes from `.pdb` files next to each `.dll`; managed plugins without a `.pdb` cannot be stepped. Script debugging leaks small amounts of memory per thread, so turn it off when chasing leaks. [Troubleshooting debugging, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/managed-debugging-troubleshooting.html)
- iOS debugs over TCP only (port 56000 in Unity's example), Android over USB or TCP. Same Unity page.
- **Visual Studio:** "Attach to Unity" (F5) or "Attach to Unity and Play" for the Editor; Debug > Attach Unity Debugger for players, with an Input IP button for unlisted ones. [Using Visual Studio Tools for Unity](https://learn.microsoft.com/en-us/visualstudio/gamedev/unity/get-started/using-visual-studio-tools-for-unity) (updated 2026-04-24)
- **VS Code:** Microsoft's Unity extension plus Unity's Visual Studio Editor package 2.0.20 or later; F5 attaches to the Editor, the "Attach Unity Debugger" command to players; `launch.json` entries use `"type": "vstuc", "request": "attach", "endPoint": "<ip>:<port>"`. [Unity Development with VS Code](https://code.visualstudio.com/docs/other/unity) (page dated 2023-08-04)
- **Rider:** "Attach to Unity Editor & Play" run configuration; Run > Attach to Unity Process for players and devices; IL2CPP players attach the same way when built with Development Build and Script Debugging. [Debug Unity applications, Rider 2026.2 help](https://www.jetbrains.com/help/rider/Debugging_Unity_Applications.html) (2026-08-13)

What an agent can and cannot do unattended:

- **Can:** read and set the Editor's code optimization (observed: a batchmode editor reported `CompilationPipeline.codeOptimization == Debug`); build a development player with `AllowDebugging`; read the debugger port and IP from the player log; compile throwaway code with `run_script --pdb true` so a human's breakpoints bind in it.
- **Cannot, per the vendors' docs:** start an IDE debug session, set breakpoints or step without the IDE's GUI. None of the three documents a headless or command-line debugger client. Mono's soft-debugger wire protocol is visible in the player log (`--debugger-agent=transport=dt_socket,...,server=y,suspend=n`, observed), but no Unity or vendor page supports scripting it, so a custom client is **unverified**.
- **Must not:** pass `-wait-for-managed-debugger` or build with Wait For Managed Debugger when no human will attach; the process waits before running any script code.
- Open question: JetBrains ships an MCP server with Rider; whether it exposes debugger control is **unverified** (it was not reachable in this session).

## 5. Minimal performance measurement

- **`ProfilerRecorder`** reads Profiler markers and counters "in Editor and Player builds, including Release Players"; `ProfilerRecorderHandle.GetAvailable` lists metrics; it is `IDisposable`. Unity's example samples `"Main Thread"` (category Internal) and memory counters such as `"GC Reserved Memory"`. [ProfilerRecorder, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.html)
- **`ProfilerMarker`** `Begin`/`End` are `[Conditional]` and "have zero overhead in non-Development (Release) builds"; markers show in the CPU Profiler and can be read by a recorder. [ProfilerMarker, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Profiling.ProfilerMarker.html). In 6.6 instrumentation code paths are tied to `UNITY_INCLUDE_INSTRUMENTATION` (Instrumented variant and above) ([managed code variants, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/managed-code-variants.html)). How that interacts with the "Release Players" claim above is **unverified**.
- Observed via `run_script` in Edit mode: a recorder on a custom marker read `Count == 0` and `LastValue == 0` inside the same editor update, while `CurrentValue` held the accumulated 7.37 ms (ns units) that a `Stopwatch` measured as 7.38 ms. Built-in `"Main Thread"` and `"Total Used Memory"` recorders read 0 on their first update. So sample across frames (a Play-mode component or several editor updates) before reading `LastValue`, or read `CurrentValue` for a same-frame total.
- **`Stopwatch`** in an EditMode test or an `-executeMethod` method measures pure C# fine; the Performance Testing package (`Measure.Method`, warmup, iterations) is the structured version, see [unity-testing.md](./unity-testing.md).
- `get_performance_stats` gives a one-shot read of frame time and memory from a connected editor (section 1).
- Profiler command line: `-profiler-enable`, `-profiler-log-file <file.raw>`, `-profiler-capture-frame-count <n>` (players only), `-profiler-maxusedmemory`. [Profiler command line arguments, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/profiler-command-line-arguments.html)

Why Editor timings differ from Player timings (all 6000.6):

- "Play mode runs in the same application and main thread as the Editor", so UI, Inspectors, Scene view rendering and asset management affect measurements, and Play-mode profiling "doesn't give you an accurate reflection" of a device. [Collect performance data introduction](https://docs.unity3d.com/6000.6/Documentation/Manual/profiling-collect-data-introduction.html)
- The Profiler files Editor work under `EditorLoop` and Play-mode work under `PlayerLoop`; some markers exist only in the Editor (`GetComponentNullErrorWrapper`, `CheckConsistency`, `CheckAllowDestructionRecursive`, prefab work). Deep Profiling in Play mode slows every scripting call, Editor calls included. [Play mode and Editor samples](https://docs.unity3d.com/6000.6/Documentation/Manual/profiler-play-edit-samples.html)
- Editor code optimization in Debug mode "runs slower in Play mode" ([managed code debugging](https://docs.unity3d.com/6000.6/Documentation/Manual/managed-code-debugging.html)); the Editor always has `UNITY_ASSERTIONS`, `UNITY_ENABLE_CHECKS` and instrumentation defined ([symbol reference](https://docs.unity3d.com/6000.6/Documentation/Manual/scripting-symbol-reference.html)).
- A headless editor is not frame-limited (observed above), so per-frame numbers from it say nothing about a vsync-capped device.

## 6. Symptom signatures per bug class

Exact messages are observed on 6000.6.2f1 unless a doc is cited.

**Missing script or lost reference (serialization).** Mechanism in [unity-serialization.md](./unity-serialization.md) section 1.

- Deleting a component's `.cs` and `.meta` left the scene YAML's `m_Script` GUID in place; `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount` returned 1 ([API, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/GameObjectUtility.GetMonoBehavioursWithMissingScriptCount.html); `RemoveMonoBehavioursWithMissingScript` removes them).
- Opening that scene in Edit mode logged nothing at warning level or above. Entering Play mode with scene reload on logged the warning `The referenced script (Unknown) on this Behaviour is missing!` with an empty stack trace. With scene reload off, no warning appeared. So a missing script can be silent until the scene is loaded fresh; detect it with the API above rather than the console.
- Unassigned serialized reference, in the Editor (Edit or Play mode, object loaded from a scene): `UnassignedReferenceException: The variable target of Holder has not been assigned.` followed by "You probably need to assign the target variable of the Holder script in the inspector." The same field on a component made with `AddComponent` at runtime threw a plain `NullReferenceException`, and so did the field in every player build (section 3). So "unassigned" in the Editor becomes an ordinary NRE in a player.
- Destroyed object: `MissingReferenceException: The object of type 'UnityEngine.Transform' has been destroyed but you are still trying to access it.` The reference compared `== null` as true while `(object)ref == null` was false (Unity's overloaded equality).

**Works in the Editor, fails in the Player.**

- `#if UNITY_EDITOR` code is omitted entirely from player compilation ([conditional compilation, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/platform-dependent-compilation.html)); `UnityEditor` APIs such as `AssetDatabase` are Editor-only (see [unity-assemblies.md](./unity-assemblies.md) section 4). A build that compiles can still behave differently where the Editor branch did setup work.
- Stripping: the Unity linker removes code it cannot see being reached; reflection-only use is the usual victim; preserve with attributes or `link.xml`. [Managed code stripping, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/managed-code-stripping.html)
- IL2CPP: no `System.Reflection.Emit`; reflection-only serialization and runtime-constructed generics (`MakeGenericType`, `MakeGenericMethod`) may lack generated code; exception-filter side effects run in a different order than on Mono; Web has no managed threads. [IL2CPP limitations, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/scripting-restrictions.html)
- Asserts and `[Conditional]` diagnostics disappear in release builds (section 3), so code with side effects inside an assert changes behaviour.
- Unassigned fields turn from `UnassignedReferenceException` into `NullReferenceException` (above).
- Release stack traces have no file or line and can skip inlined frames (section 3); reproduce in a Development Build before reading a stack.

**Import settings.** `Texture2D.GetPixels` and `EncodeToPNG` need `Texture.isReadable`, which "By default ... is false for texture assets that you import" (Read/Write Enabled, or `TextureImporter.isReadable`); textures created from script default to readable. Particle System Shape Module and Terrain Paint Detail need it or the build fails. [Texture.isReadable, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Texture-isReadable.html). The connected editor's `get_import_settings` / `set_import_settings` read and change these (catalog, 0.7.0-exp.1). The exact exception text for an unreadable texture was not observed (**unverified**).

**Execution order and frame timing.** "In general, you can't rely on the order in which the same event function is invoked for different GameObjects", including instances of one script ([Event function execution order, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/execution-order.html)). Script Execution Order settings and `[DefaultExecutionOrder]` set order between types, the Editor setting wins over the attribute, equal orders are "not deterministic", and neither affects `[RuntimeInitializeOnLoadMethod]`, `OnDisable` or `OnDestroy` ([Script execution order, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/script-execution-order.html)). The order between resumed coroutines and async tasks "is not guaranteed" (execution order page). `Destroy` is deferred to the end of the frame ([unity-testing.md](./unity-testing.md)). A bug that depends on these shows up as flakiness across runs or machines rather than a fixed failure.

**Second Play session.** With domain reload off, statics and static event subscriptions persist between Play sessions; see [unity-assemblies.md](./unity-assemblies.md) section 8 for the reset attributes and the 6.6 default. Observed signature (section 1): a bug that reproduces on the first Play and not the second (or the reverse), a counter that starts above zero, a one-shot flag already set. Check `EditorSettings.enterPlayModeOptions` first when a repro is not repeatable.

## Open questions (unverified)

- Which 6.6 page is right about `DEBUG`; whether a Build Profile build and a `BuildOptions` build differ here.
- Whether `console` answers while the Editor is stopped at an IDE breakpoint.
- Whether `ProfilerMarker` data reaches a `ProfilerRecorder` in a 6.6 Release-variant player.
- Whether Rider's MCP server can drive its debugger.
- The same probes on IL2CPP, macOS and Linux; everything observed here was Mono on Windows.

## 7. Sandbox checks for the skill

For [#35](https://github.com/Hissal/mattpocock-skills-unity/issues/35), the six checks designed in [#24](https://github.com/Hissal/mattpocock-skills-unity/issues/24). **Observed** on 2026-09-25, same machine and versions as above, through a GUI editor opened with `unity open` (connected through Pipeline, `set_autotick` on, Enter Play Mode Options on with domain and scene reload off), with probes in `Assets/_t35/` removed afterwards.

**An unfocused GUI editor does not advance Play mode.** With the editor window in the background (`InternalEditorUtility.isApplicationActive == false`), `editor_play` entered Play mode but `Time.frameCount` stayed at 1 for seconds, so a `wait_for` on a frame-driven symptom timed out. `set_autotick` (on, and re-armed before each Play), `PlayerSettings.runInBackground`, `Application.runInBackground` in Play and `EditorApplication.QueuePlayerLoopUpdate()` did not change it, and `editor_focus` ("Editor focused via DockArea") did not bring the window to the foreground. With the window focused, every run advanced (about 450 to 520 frames in 2 s). What works unfocused: `editor_pause`, then one `eval` calling `EditorApplication.Step()` N times advances exactly N frames synchronously (`1->101` for 100 steps). The earlier headless observation (section 1) had no such stall.

1. **Play loop script, red then green, second-Play flag: confirmed.** A scratch-dir bash script (enter Play, step frames when unfocused, `wait_for` a static `Done` flag, `console --since <cursor>` grepped for the symptom with the graphics-device errors dropped, `editor_stop`) exited 1 on the bug in three of three runs, and 0 after the fix. With domain reload off it runs twice: a one-shot static (a flag set on the first Play and never reset) gave red then green, exit 3. Run straight after earlier Plays without a reload, the same one-shot bug gave green twice, because the static was already set: the script therefore calls `EditorUtility.RequestScriptReload()` first so run 1 is a true first Play (after which the probe's statics read 0), and then flagged red/green in two of two runs. Without Unity focus the script falls back to stepping; a timeout (`met: false`) exits 2 as no verdict, never green.
2. **`wait_for` async with `on_met.pause`: confirmed with a caveat; `console --since`: confirmed.** `wait_for` polls every `poll_interval_ms` (default 100), not every frame (5 observations in 724 ms): `op: equals 60` on a per-frame counter skipped past 60 and ran until Play exited (`state: interrupted`, "Wait interrupted by a domain reload or exiting play mode."), and `greaterThan 59` paused at frame 78. With `poll_interval_ms 0` or `1` it paused at exactly frame 60, the frame the condition first held; `16` paused at 61. `console --since <cursor>` returned only entries with a higher `seq` than the cursor taken before Play. The response's `session` names the console capture session, not the Play session.
3. **`run_script` on a file outside the project: confirmed.** An absolute path to a file in the OS temp scratch dir compiled in memory (124 ms), reached `Assembly-CSharp` types, and ran; no `.meta` or asset appeared anywhere. A throw came back as `success: false`, `error: "Runtime Error"` and `errorDetails` naming the scratch file and line. File and line also appeared without `--pdb true` in this run, so what `--pdb` adds is the documented breakpoint binding, which needs a human debugger and was not tried.
4. **`ProfilerRecorder` across Play frames: confirmed, with real frames only.** A recorder started in a component's `Start` on a custom marker used in `Update` read `LastValue = 150900` (ns) after about 280 real frames. Frames advanced by `EditorApplication.Step()` left it at `LastValue = 0`, `Count = 1`, so step-driven frames are not usable for recorder measurements.
5. **Windows Development player logs: confirmed, default path observed.** A `BuildOptions.Development` Windows player (built in 28 s through `run_script` calling `BuildPipeline.BuildPlayer`) launched `-batchmode -nographics -logFile <file>` wrote both tagged lines to that file: the `Debug.Log` with a managed stack, and the exception with `T35PlayerProbe.cs:16` (the inlined `Nested()` frame missing, as in section 3). The boot memory setup still went to stdout. Launched windowed with no `-logFile`, it wrote `%USERPROFILE%\AppData\LocalLow\DefaultCompany\unity-sandbox\Player.log` (company and product from Player Settings), and `Application.consoleLogPath` returned that path. That closes the first open question below. The Development player raised a Windows Firewall prompt ("Do you want to allow public and private networks to access this app?") on the user's desktop; both runs had already logged and exited while it was open.
6. **`Debug-<tag>/` under a self-ignoring prototype folder: confirmed with a correction.** A repro folder `Debug-t35b/` (a component logging `[DEBUG-t35b]`, a scene, their `.meta` files) under a folder holding a `*` and `!.gitignore` `.gitignore`: `grep -rl t35b Assets Logs` found only the component's source and `Logs/Editor.log`, not the scene (its content does not carry the tag) nor any `.meta`. Adding a path search, `find Assets -path '*Debug-t35b*'`, found the folder, its `.meta`, the scene, the script and both `.meta` files. Removal by `unity-prototyping`'s procedure (every `guid:` in the folder's `.meta` files grepped outside it: 0 hits; delete with `.meta`; `unity recompile`) compiled clean with no new console warnings.

Also seen: compiling a script with `#if DEVELOPMENT_BUILD` on 6000.6.2f1 logs `warning UAC0009: DEVELOPMENT_BUILD preprocessor directive has been deprecated`, pointing at the managed code variant symbols and `Debug.isDebugBuild`.
