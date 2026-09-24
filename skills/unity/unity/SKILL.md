---
name: unity
description: Unity knowledge entry point. Use when another skill asks for Unity knowledge, or in a Unity repo when no `unity-*` skill fits the task.
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

Rows land with the Unity skill that owns them, so the table names only skills that exist.

## 4. No match

When no row fits, return the config facts: the project path(s) and shape, the conventions pointers, and any policy the config sets (or the assumed defaults from step 2). Then name the need, as `No Unity skill covers: <need>.`, so the gap is visible to the caller and the user.
