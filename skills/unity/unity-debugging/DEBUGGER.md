# Attaching a debugger

An IDE debugger is a human step: no IDE documents a way for an agent to start a debug session, set breakpoints or step without its GUI. Ask for one only when the REPL cannot reach the state, and hand the human the exact steps below for their IDE. While a human sits at a breakpoint the Editor stops responding, so expect connected commands that need the main thread to stall until they continue.

## What the agent prepares

- **Throwaway code a breakpoint should bind in**: run it with `run_script --pdb true`, which per Pipeline's docs emits a portable PDB so breakpoints bind in it (breakpoint binding has not been tried here).
- **A player to debug**: a Development Build with Script Debugging, through `unity-verification`'s player build rung. The player log then carries `Starting managed debugger on port <n>` and a `Multi-casting "[IP] ... [Port] ... [Debug] 1 ..."` line: read the IP and port from it for the human.
- **The Editor's code optimization** must be Debug to attach. Ask the user to switch it (the bug icon in the status bar, or Preferences > General > Code Optimization On Startup); it slows Play mode, so leave the choice and the switch back to them.
- **`-wait-for-managed-debugger`** (or Wait For Managed Debugger in the build) only once the human is ready to attach: the process waits before running any script.

## Steps for the human

**Rider**: for the Editor, pick the "Attach to Unity Editor & Play" run configuration and debug it. For a player or device, Run > Attach to Unity Process and pick it (or enter the IP and port). IL2CPP players attach the same way when built with Development Build and Script Debugging.

**Visual Studio** (Tools for Unity): for the Editor, "Attach to Unity" (F5), or "Attach to Unity and Play". For a player, Debug > Attach Unity Debugger, and Input IP for one not listed.

**VS Code** (Microsoft's Unity extension, with Unity's Visual Studio Editor package 2.0.20 or later): F5 attaches to the Editor. For a player, run the "Attach Unity Debugger" command, or add a `launch.json` entry with `"type": "vstuc"`, `"request": "attach"` and `"endPoint": "<ip>:<port>"`.

Then set the breakpoint, reproduce, and tell the agent what the debugger shows.

## Limits

- Managed plugins without a `.pdb` next to their `.dll` cannot be stepped.
- Script debugging leaks a little memory per thread: turn it off when chasing a leak.
- iOS debugs over TCP only, Android over USB or TCP; Web cannot be debugged this way.
