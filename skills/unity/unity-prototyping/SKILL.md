---
name: unity-prototyping
description: Unity prototyping rules. Use when building a throwaway prototype in a Unity repo; deciding whether an HTML prototype fits or it belongs in the engine; running a prototype or switching its variants; or capturing or removing a Unity prototype.
---

# Unity prototyping

This repo's Unity config (`docs/agents/unity.md`), if present, overrides these defaults, including its **Prototype folder** field.

This skill owns the Unity side of a prototype: where it lives, how it stays out of production code and player builds, its scenes, how it runs, and how it is captured and removed. What a prototype is for, its branches and its capture rule stay with the `prototype` skill; its rule to locate a prototype next to the code it serves becomes the prototype folder below, which keeps it out of `main` and player builds. It calls siblings through the Skill tool for their mechanics: `unity-assemblies` for the asmdef, `unity-serialization` for the GUID grep and deleting with `.meta`, `unity-verification` for compiling, running C# in the Editor and entering Play.

## In Unity or in HTML

In Unity is the default.

- **Logic** ("does this state model feel right?"): a pure C# class driven by an EditorWindow (below). Offer HTML in one line only when the person judging it will not open the Unity editor, and say that the HTML logic will not lift into the C# code. On a yes, follow the `prototype` skill's `LOGIC.md` as written.
- **UI and feel** ("what should this look like?", and "does this feel right?": a jump arc, a camera, hit-stop): variants in a scene, switched live in Play. Offer HTML in one line only for a flat screen layout (a menu, a HUD, a settings screen) where in-engine fidelity does not matter yet. Never for world-space UI, feel, or anything that needs real scene content.

Offer at most once; without a yes, build in Unity.

## Where it lives

Each prototype gets `<prototype folder>/<name>/`: its scripts, scenes and assets, nothing outside it. The prototype folder is the config's **Prototype folder** field, or `Assets/_Prototypes/` in the Unity project (in a package-only repo, ask which project hosts it).

- An existing folder is used as found. Know how it behaves in git before writing to it (`git check-ignore -v <folder>/x`): **committed** means the prototype must never be committed on the working branch; **ignored** (by a `.gitignore` anywhere) means nothing inside reaches a branch until captured with `git add -f`.
- A missing folder that `git check-ignore -v <folder>/x` shows ignored is gitignored on purpose (a fresh clone has none): create it plain.
- Any other missing folder is created self-ignoring: a `.gitignore` of `*` then `!.gitignore` inside it, so the folder exists on every clone and nothing else in it can be committed by accident. Commit the `.gitignore` and the folder's own `.meta` once Unity has made it, and say so to the user.

## Isolation

Call `unity-assemblies` to create the asmdef (its GUID rule, a hand-written `.meta`, and `GUID:` references to the production assemblies the prototype uses). The prototype's own asmdef sets:

```json
"autoReferenced": false,
"defineConstraints": ["UNITY_EDITOR"]
```

- `autoReferenced: false`: `Assembly-CSharp` cannot see it, and no production asmdef references it, so production code can never depend on the prototype.
- `defineConstraints: ["UNITY_EDITOR"]`: it compiles only in the Editor and is left out of player builds, while its MonoBehaviours still attach to scene objects. It may use `UnityEditor` (menu items, windows) with no `#if`.
- Never an Editor-platform asmdef (only the Editor platform included) or an `Editor` folder: Unity refuses to attach a MonoBehaviour from one.

When the code the prototype needs lives in `Assembly-CSharp`, which asmdef code cannot reference, use no asmdef: the files go in the prototype subfolder and each is wrapped whole in `#if UNITY_EDITOR`, which omits it from player builds.

The prototype compiles clean before hand-over: a compile error in it fails the whole project's compile, so nothing new runs until it is fixed.

## Scenes

- **On an existing scene** (preferred when one hosts the question): a prototype scene in the subfolder holding the variants, with the host scene opened additively beside it and the prototype scene set active, so objects created at run time land in it.
- **Standalone**: a greybox scene in the subfolder, when nothing existing hosts the question.

Neither goes into the build profile's scene list: `EditorSceneManager.OpenScene` needs no build entry.

**The host scene stays unsaved while the prototype is open**, a hard rule. It stays clean through Play and Play-mode edits, but any Edit-mode change dirties it, and a save of all open scenes then writes it. Save only the prototype scene (`EditorSceneManager.SaveScene` on it), and tell the human the same in the hand-over.

## Running

- **One menu item, `Prototypes/<name>`.** For a scene prototype it offers to save modified scenes, opens the prototype scene, opens the host additively, sets the prototype scene active and enters Play. For a logic prototype it opens the EditorWindow driver, with no Play.
- **Logic driver**: an EditorWindow over the pure class, following the HTML logic prototype's layout (the question, the current state in labelled fields, free-play buttons, one tab per scenario with its ordered steps). The class has no `UnityEngine` or `UnityEditor` in it, so it lifts into the real code as is.
- **Variant switcher**: a runtime overlay at the bottom centre with left and right arrows and the current variant's name, the arrow keys cycling too, ignoring keys while a text field has focus. It lives in the prototype's assembly, so the asmdef compiles it away from player builds. Variants are structurally different (a different layout, arc or camera rig, not a tweaked number); each variant is a GameObject subtree the switcher activates alone.

[TEMPLATES.md](TEMPLATES.md) has the menu item, the logic driver and the switcher.

## Hand-over

Check it yourself first, through a connected editor (call `unity-verification` for running C# in the Editor): compile clean, run the menu item with `EditorApplication.ExecuteMenuItem("Prototypes/<name>")`, confirm Play started with the prototype scene active and a clean console, switch a variant, and exit Play. Then hand the human the menu path, the variant names, and the never-save rule for the host scene. Without a connected editor, compile headless and report the Play check as unvalidated.

## Capture and removal

After the calling workflow's capture rule (the validated decision folded into the real code, the prototype kept on a throwaway branch):

1. **Capture** on the throwaway branch: the subfolder and its own `.meta`, which sits in the parent folder. When the prototype folder is ignored, both need `git add -f <folder>/<name> <folder>/<name>.meta`.
2. **GUID grep before removal** (call `unity-serialization` for how): every `guid:` in the subfolder's `.meta` files, grepped across the project outside the subfolder. Nothing may reference them. A hit means a validated piece was folded in by reference: move it out into production first (with its `.meta`, so its GUID and every reference to it hold), then grep again.
3. **Delete** the subfolder and its `.meta`, with no prototype scene open (open a production scene first).
4. **Compile check** through `unity-verification`, reading the console for errors and missing-script warnings. When Unity cannot run here, report the removal as unvalidated.

Folding a winner in means rewriting it into the production assemblies; a prototype's asmdef never becomes a production dependency.

## Validation

This skill names the checks; call `unity-verification` through the Skill tool for how Unity runs them, and put the result in its report.

- **Text checks**: every file of the prototype is inside its subfolder; its asmdef has `autoReferenced: false` and `defineConstraints: ["UNITY_EDITOR"]` (or, without one, every file is wrapped in `#if UNITY_EDITOR`); no production asmdef references it; no prototype scene is in the build profile; nothing of it is staged on the working branch; the host scene is not modified in the diff.
- **Compile** before hand-over and after removal.
- **Play**: the menu item enters Play cleanly, through a connected editor. Otherwise unvalidated.
