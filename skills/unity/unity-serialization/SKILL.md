---
name: unity-serialization
description: Unity serialization rules. Use when changing a serialized field, type or enum; moving, renaming or deleting an asset or script; editing, reviewing or merging scene, prefab, `.asset` or `.meta` files; or touching prefab overrides.
---

# Unity serialization

This repo's Unity config (`docs/agents/unity.md`), if present, overrides these defaults.

These rules cover Unity's own serializer. When the config's **Serialization** section names a third-party serializer (Odin and similar), fields that serializer owns follow its rules and the conventions the config points at, not this skill.

A rule gated on a Unity version says so. When the gate matters, read the version from `ProjectSettings/ProjectVersion.txt` (a UPM package: the `unity` field of its `package.json`, its minimum).

## Identity

- **The `.meta` GUID is the asset**, not its path. A reference to another file is stored as `{fileID, guid, type}`, so a moved or renamed asset keeps every reference as long as its `.meta` travels with it. Move, rename and delete an asset together with its `.meta` (a folder with its folder `.meta`), and commit `.meta` files. A lost or regenerated `.meta` means a new GUID, and every reference to the asset breaks silently.
- **Scripts are assets.** A component stores `m_Script: {fileID: 11500000, guid: <the script's .meta GUID>, type: 3}`, never the class name. Renaming a MonoBehaviour or ScriptableObject class is safe when its file is renamed to match and keeps its `.meta`; deleting and recreating the file leaves a missing script everywhere it was used.
- **One class per script file, named after the file, in a block namespace.** Unity cannot map a class in a file-scoped namespace (`namespace X;`) to its script: `MonoScript.GetClass()` returns null, and the component saves as `m_Script: {fileID: 0}`, a missing script on the next load (observed on 6000.6.2f1).
- **ScriptableObject and `AssetReference` fields are GUID references**: they survive moves and renames like any reference. An Addressables address, and any path string passed to a load call, is a **string contract**: renaming or moving the asset breaks it silently, so grep the code for the string.
- `Library/` is a per-machine cache Unity regenerates from `Assets/`, `Packages/` and `ProjectSettings/`: leave it out of edits and commits.

What serializes at all (field rules, inline versus `[SerializeReference]`, dictionaries from 6.6, callbacks): [WHAT-SERIALIZES.md](WHAT-SERIALIZES.md).

## Renames

Serialized data is keyed by name. Every rename below carries its attribute in the same change, or the stored value is lost.

- **A serialized field**, including one inside a `[Serializable]` class: `[FormerlySerializedAs("oldName")]` on the renamed field. Unity reads the old name everywhere on load, prefab overrides included, but files keep it on disk until reserialized: see the follow-up below.
- **A field turned into an auto-property** with `[field: SerializeField]` serializes as its backing field, `<Name>k__BackingField`: add `[field: FormerlySerializedAs("oldName")]`. Back from an auto-property to a field: `[FormerlySerializedAs("<Name>k__BackingField")]`.
- **A `[SerializeReference]` type** renamed, or moved to another namespace or assembly: `[MovedFrom(false, sourceNamespace: ..., sourceAssembly: ..., sourceClassName: ...)]` on the type, naming only what changed. The stored data records each object's class, namespace and assembly; without the attribute the reference loads as null, and re-saving the host drops its data for good. Fix the type before any host is re-saved.
- **A serialized enum** stores its integer value: give it explicit values, append new members, and never reorder, remove or reuse a value.
- **A serialized field's type** changing is not a rename: treat it as data loss unless you check the conversion in the Editor.

Moving a script folder or an assembly definition keeps every GUID when the `.meta` files move too; a `[SerializeReference]` type that changes assembly needs `MovedFrom`. For the asmdef side of the move, call the Skill tool with "unity-assemblies".

## The reserialize follow-up

A rename is **expand then contract**: two commits, the second only on the user's okay.

1. **Expand**: the rename plus its attribute. Commit. The change is complete here; the attribute is harmless if it stays forever.
2. **Contract**: propose rewriting the files that still hold the old name, then removing the attribute. Always propose, even when the scope is small: build the scope (below), then show the file count and the path list and wait. When the scope is uncertain (built by the grep fallback, or the script printed notes), offer two options: the scoped list, or the whole project.

On the user's okay:

- **Scoped**: run `SerializationScope.Reserialize` with the approved list. It calls `AssetDatabase.ForceReserializeAssets(paths)`, which rewrites only the listed files, then re-records the prefab instance overrides in each listed scene. `ForceReserializeAssets` leaves those scene overrides on the old `propertyPath`, and they are lost once the attribute goes.
- **Whole project**: `ForceReserializeAssets()` with no arguments rewrites every asset: a large diff. Scene overrides still need the re-record pass, so then run `Reserialize` with every `.unity` file in the project.

Then remove the attribute, compile, validate, and commit. Prefab files keep a stale entry under the old `propertyPath` beside the new one; it no longer applies to anything and can stay.

### Building the scope

With an Editor, run [scripts/SerializationScope.cs](scripts/SerializationScope.cs) (to run C# in the Editor, call the Skill tool with "unity-verification"; its header names each entry point and its arguments). `Find` walks every MonoBehaviour and ScriptableObject type that serializes the changed type at any depth (nested `[Serializable]` types, inherited fields, arrays and lists, `[SerializeReference]` fields that can hold it), maps them to script GUIDs, and greps every UnityYAML file to a fixpoint (each matched prefab adds its own GUID, which pulls in nested instances, variants and scenes). The list over-reaches by type: every file using a host component is listed, whether or not that instance holds the changed type. Over-reaching only adds files to the rewrite; missing one loses data.

Without an Editor, run [scripts/scope.sh](scripts/scope.sh) `<project> <script.cs>...`, seeded with every script whose class declares the changed field or holds the changed type, and every subclass of those. It covers only the grep phase, so the proposal states that hosts reached through subclasses, nested types or `[SerializeReference]` fields may be missing.

## Editing order

1. **The Editor**: a connected editor or the `unity` CLI (`unity skill show` for its commands).
2. **A scripted API**: `AssetDatabase`, `SerializedObject`, `PrefabUtility`, run in the Editor.
3. **A surgical text edit**: line-level find-and-replace on known files, written straight to the text. Typical cases: a project-wide GUID swap repairing references to a replaced asset, an `m_Script` repair, a single value. UnityYAML is not standard YAML, so always edit the text in place; a YAML library that parses and re-emits a file corrupts it.

Structural text edits (adding or removing `--- !u!` documents, fileIDs, `m_Modifications` entries) prefer the Editor; without one they are allowed only when simple. A `.meta` file's own `guid:` line changes only when adopting a known GUID is the point of the change. Every surgical or structural edit is followed by validation. The format and edit recipes: [UNITYYAML.md](UNITYYAML.md). Overrides, nesting and variants: [PREFABS.md](PREFABS.md).

## Merge conflicts

Resolving a merge or rebase conflict in a Unity asset, or checking the merged result: read [MERGING.md](MERGING.md) before touching a conflicted file.

## Validation

This skill names what to check; for how Unity runs it and how to report the result, call the Skill tool with "unity-verification". After a serialization change, check:

- **No missing scripts**: no new `m_Script: {fileID: 0}` in the changed files; in the Editor, `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount` on each affected object is 0.
- **No missing references**: every `guid:` added in the diff resolves to a `.meta` in the repo or a package.
- **No managed references with missing types**: every `type: {class: ..., ns: ..., asm: ...}` line in the changed files' `references:` blocks names a type that exists. `SerializationUtility.HasManagedReferencesWithMissingTypes` is documented for this, but returned false on 6000.6.2f1 while a reference loaded as null, so treat a false as unconfirmed.
- **No lost values**: a renamed field's value read back through `SerializedObject` on a sample prefab, variant and scene instance matches the value before the change.
- **Every move kept its `.meta`**: `git status` or `git diff -M --name-status` pairs each moved or renamed asset with its `.meta`, and no `.meta` was deleted and re-added under a new GUID.
- **No old GUID left** after a GUID swap: a grep for it finds nothing.

Without Unity, run the text checks, then report the Unity-side checks as **unvalidated** in Unity.

## Reviewing serialized changes

Start from `git diff --stat -- '*.unity' '*.prefab' '*.asset' '*.meta'`, never a scene diff read whole. Then grep the diff:

- `m_Script` lines: a changed script reference, or `{fileID: 0}`.
- `guid:` lines: references added, removed or swapped.
- `propertyPath` lines: overrides renamed or dropped.
- Meta churn: a `.meta` whose `guid:` changed, a `.meta` added or deleted without its asset, an asset moved without its `.meta`.

In the code: a serialized field renamed without `FormerlySerializedAs`, a field turned into an auto-property without it, an enum reordered or a value removed, a `[SerializeReference]` type renamed or moved without `MovedFrom`, a MonoBehaviour or ScriptableObject in a file-scoped namespace. A finding is hard when it loses data or breaks a reference; otherwise it is a judgement call.

## Slicing work into tickets

- Tickets that touch the same `.unity` or `.prefab` block each other, even when their logic is independent: a structural merge conflict in one scene stops for a human.
- A serialized rename is two slices, expand then contract, with the contract waiting on the user's okay.
- Authoring slices (building or rewiring scenes and prefabs) need a connected editor or a human.

## Hotspot scans and friction signals

- A churn or hotspot scan leaves out asset churn: `*.unity`, `*.prefab`, `*.asset`, `*.meta`, `ProjectSettings/` and `Packages/packages-lock.json`. It scans `*.cs`, `*.asmdef` and `*.asmref`.
- Friction signal, **serialized fields as hidden interface**: scenes and prefabs depend on a type's serialized field names and shapes, invisible from the code, so every rename is a data migration. Report where it shows (types whose serialized fields churn, or many prefabs overriding the same fields); what to do about it is the user's call.
