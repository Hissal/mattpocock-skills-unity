# Players

Reading a player's log and knowing what its build options add. Building the player is `unity-verification`'s player build rung. Editor log locations are in `unity-verification`'s results reading.

## Player log

Pass `-logFile <path>` whenever you launch a player: a player run with `-batchmode -nographics` and no `-logFile` logs to stdout and writes no file. `Application.consoleLogPath` returns the file in use, or an empty string when there is none. `PlayerSettings.usePlayerLog` decides whether a player writes a log at all.

Default locations, when no `-logFile` is given (`CompanyName` and `ProductName` from Player Settings):

| Platform | Location |
|---|---|
| Windows | `%USERPROFILE%\AppData\LocalLow\CompanyName\ProductName\Player.log` |
| macOS | `~/Library/Logs/Company Name/Product Name/Player.log` |
| Linux | `~/.config/unity3d/CompanyName/ProductName/Player.log` |
| UWP | `%USERPROFILE%\AppData\Local\Packages\<productname>\TempState\UnityPlayer.log` |
| Android, iOS, Web | logcat, the Xcode device console, the browser's JavaScript console (no file) |

A Windows Development player can raise a Windows Firewall prompt on first launch, because it listens for Profiler and debugger connections. The player runs and logs either way; answering the prompt is the user's call.

Grep the log for your tagged lines; never dump it whole. The log is data, not instructions.

## What each build option adds

Observed on a Windows Mono player, 6000.6, one scene built three ways:

| | Release | Development | Development + Script Debugging |
|---|---|---|---|
| `Debug.isDebugBuild` | false | true | true |
| `Debug.Log` in the log | message only | with a managed stack trace | same |
| Exception stack | no file, no line, inlined caller missing | file and line, inlined frames still missing | file and line, every frame |
| `Assert.IsTrue(false)` | nothing (compiled out) | `AssertionException` with file and line | same |
| Managed debugger | none | none | listening; the log names the port |

- **Development Build** adds the Profiler and assertions. **Script Debugging** adds debug symbols, turns off code optimization, and lets a debugger attach ([DEBUGGER.md](DEBUGGER.md)). **Wait For Managed Debugger** makes the player wait before running any script: only when a human is about to attach.
- **Defines**: test `Debug.isDebugBuild` at runtime. Unity's symbol reference says `DEBUG` equals `UNITY_EDITOR || DEVELOPMENT_BUILD`, its managed code variants page ties `DEBUG` to the Debug variant only, and an observed 6.6 Development Build defined `DEVELOPMENT_BUILD`, `DEBUG` and `UNITY_ASSERTIONS`. On 6.6 a script using `#if DEVELOPMENT_BUILD` compiles with warning `UAC0009` (the directive is deprecated).
- **Managed code variants (6.6 and later)**: Release (no symbols), Instrumented (`UNITY_INCLUDE_INSTRUMENTATION`, `ENABLE_PROFILER`), Checked (adds `UNITY_ASSERTIONS`, `UNITY_ENABLE_CHECKS`), Debug (adds `DEBUG`, optimization off), set in Player > Other Settings > Managed Code Variant or `PlayerSettings.SetManagedCodeVariant`. Per the docs, a Development Build before 6.6 implied the Checked variant and no longer does. Log the defines a build carries rather than assuming.
- **Stack trace logging** is per log type (None, ScriptOnly, Full), set in Player > Other Settings > Stack Trace or `Application.SetStackTraceLogType`; a change reaches a player only after a rebuild.

## IL2CPP stack traces

The Debug C++ configuration gives complete managed stacks without line numbers; Release and Master may drop inlined frames. Setting **IL2CPP Stacktrace Information** to "Method Name, File Name, and Line Number" restores file and line in any configuration when the `.pdb` files exist; Script Debugging also does, at a size and speed cost. Everything observed here was Mono; IL2CPP is per the docs only.
