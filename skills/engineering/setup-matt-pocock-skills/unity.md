# Unity config

This repo's Unity facts and policies, only where they differ from the Unity skills' defaults: a missing field means the owning skill's default. Written by `/setup-matt-pocock-skills`, which keeps hand-written lines when re-run.

When the repo contradicts a fact here (a moved doc, script, workflow or project folder), trust the repo, say so once, and suggest re-running `/setup-matt-pocock-skills`.

## Project

Owner: `unity`. Always written.

- **Path**: each Unity project or package folder, relative to the repo root (`.` for the root).
- **Shape**: `project`, `package` or `both`.

## Conventions

Owner: `unity`. Pointers to this repo's own Unity docs, never a copy of them.

- `<path>` (`<heading>`, optional): one line naming the Unity work that doc governs.

## Verification

Owner: `unity-verification`.

- **Allowed environments**: which of the connected editor and headless Unity may run checks here.
- **Cold import ok**: whether a check may start a full import in a checkout with no `Library/` without asking first.
- **Resident headless ok**: whether a headless editor may stay running between checks.
- **Rung commands**: a Verification ladder rung, then the repo command, run from the repo root, that performs it. One line per rung.
- **Test filters**: the test filters or categories the test rungs apply.
- **Build**: the player build targets, and the build method or build profile.
- **Authoritative CI workflow**: the workflow file whose result counts as the CI rung.
- **Heavy-run warning**: whether to ask before a heavy run (a full suite, a PlayMode run or a player build) starts.

## Assemblies

Owner: `unity-assemblies`.

- **Naming convention**: the naming scheme a new assembly follows.
- **Layout policy**: which asmdef changes (a new asmdef, a split, code moved out of `Assembly-CSharp`) may be made without proposing them first, and where the new asmdefs go.
- **Project-wide defines**: where this repo keeps project-wide scripting symbols: `csc.rsp`, Player Settings or build profiles.

## Serialization

Owner: `unity-serialization`.

- **Third-party serializer**: a serializer used beside Unity's own (Odin and similar), then the path of this repo's conventions for it.

## Code lifecycle

Owner: `unity-code-lifecycle`.

- **Reload does not matter**: `yes` when statics need not be written as if domain reload is off.
- **Lifecycle API**: the static reset API that new code with no local precedent uses: the lifecycle attributes and `[AutoStaticsCleanup]`, or the pre-6.5 fallbacks.
