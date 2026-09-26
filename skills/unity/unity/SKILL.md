---
name: unity
description: Unity knowledge entry point. Use when another skill asks for Unity knowledge, when looking up the Unity API or Manual docs, or in a Unity repo when no `unity-*` skill fits the task.
---

# Unity

The entry point for Unity knowledge. It finds the Unity repo, reads its **Unity config**, and routes the caller's **need** (the moment it reaches for Unity knowledge, such as "how to run the tests") to the Unity skills that answer it. It holds no Unity mechanics: each fact lives in exactly one Unity skill.

Invoked directly with no stated need, take the task in front of you as the need.

## 1. Detect

A Unity repo holds a Unity project (a folder containing `ProjectSettings/ProjectVersion.txt`) or a UPM package (a folder whose `package.json` has a top-level `unity` field). Look in this order and stop at the first tier that finds one:

1. The project path(s) recorded under **Project** in the Unity config (`docs/agents/unity.md`), when it exists. A recorded path that no longer holds a project or package is stale: say so and carry on down the list.
2. The repo root.
3. A glob below the root for both markers, skipping `.git/`, `Library/`, `Temp/`, `Logs/`, `obj/` and `node_modules/` (Unity's `Library/PackageCache` is full of package manifests that are not this repo's).

Every hit in the winning tier counts. The repo's **shape** is `project`, `package`, or `both`.

Nothing found: reply `Not a Unity repo.` and return. The calling skill carries on exactly as it would without this step.

## 2. Read the Unity config

Read `docs/agents/unity.md`. Its facts and policies override the Unity skills' defaults, and its **Conventions** pointers name the repo's own Unity docs: read the ones whose scope covers the need.

No config: carry on with defaults. State in one line which repo facts you assumed (the project path(s) and shape from step 1, no repo conventions, every Unity skill's own defaults), and tell the user to run `/setup-matt-pocock-skills` to record them. Tell them once per conversation; on later calls, state the assumptions only.

## 3. Route

Match the need against the needs table. Every row whose need fits is a match. Call the Skill tool once for each distinct Unity skill named across the matched rows, in first-listed order: a skill two rows name is still one call.

| Need | Unity skills |
|---|---|
| running the tests, or how often to | unity-testing, unity-verification |
| checking a change compiles or works before calling it done | unity-verification |
| building a player | unity-verification |
| running C# in the Editor | unity-verification |
| Unity cannot run here, or the editor is locked | unity-verification |
| writing or changing a test | unity-testing |
| setting up tests where none exist | unity-testing |
| choosing which tests cover a change, or sketching test seams for a feature | unity-testing |
| a test failing only under Unity | unity-testing |
| reviewing a change to tests | unity-testing |
| creating, moving or deleting a script folder, `.asmdef` or `.asmref` | unity-assemblies (+ unity-serialization when moving) |
| editing assembly references or scripting defines | unity-assemblies |
| Editor-only code, or `UnityEditor` in runtime code | unity-assemblies |
| a compile error about a type that exists, or a Player build failing where the Editor compiles | unity-assemblies |
| reviewing a change to asmdefs, defines or the Editor/runtime split | unity-assemblies |
| designing a module's seams, or where the engine sits as a dependency | unity-testing, unity-assemblies |
| changing a serialized field, type or enum | unity-serialization |
| moving, renaming or deleting an asset or script | unity-serialization |
| editing scene, prefab, `.asset` or `.meta` text | unity-serialization |
| reviewing a change to serialized types or Unity assets | unity-serialization |
| resolving a conflict in a Unity asset | unity-serialization |
| slicing Unity work into tickets | unity-serialization, unity-verification |
| keeping asset churn out of a hotspot scan | unity-serialization |
| adding or changing static state, a singleton or a static event | unity-code-lifecycle |
| writing code that runs on load, on a code reload, or on entering or exiting Play mode | unity-code-lifecycle |
| state leaking between Play sessions, or a bug only on the second Play | unity-code-lifecycle |
| reviewing a change that adds or changes statics or load code | unity-code-lifecycle |
| reproducing a Unity bug or building a feedback loop for it, instrumenting or profiling it, or measuring a performance regression | unity-debugging |
| where a bug's regression test goes | unity-testing |
| where a prototype lives, how it runs and is removed, and whether an HTML one fits | unity-prototyping |
| scanning Unity code for architectural friction | unity-testing, unity-assemblies, unity-code-lifecycle, unity-serialization |

Rows land with the Unity skill that owns them, so the table names only skills that exist.

## 4. No match

When no row fits and the need is not a docs lookup (step 5), return the config facts: the project path(s) and shape, the conventions pointers, and any policy the config sets (or the assumed defaults from step 2). Then name the need, as `No Unity skill covers: <need>.`, so the gap is visible to the caller and the user.

## 5. Unity docs

For an API or Manual fact no Unity skill states, read the docs for the repo's Unity version, not the latest. `unity docs --url <Class or Class.Member>` (add `--manual <page-slug>` for the Manual) prints the version-matched page URL; fetch that. It reads the version only from the current directory, so run it from the project folder found in step 1, or pass `--editor-version` (a package's version is its `package.json` `unity` field). It never checks the page exists: on a 404, fix the name. An API page is the full type name minus a leading `UnityEngine.` or `UnityEditor.` only (`AssetDatabase`, `SceneManagement.SceneManager`, `Unity.Scripting.LifecycleManagement.AutoStaticsCleanupAttribute`), and a package's types live in that package's docs instead. Without the CLI, build the same URL: `https://docs.unity3d.com/<major.minor>/Documentation/ScriptReference/<Class>.html`, or `Manual/<page-slug>.html`.
