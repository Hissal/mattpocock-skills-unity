# Research: Unity prototypes

Ticket: [#34](https://github.com/Hissal/mattpocock-skills-unity/issues/34), designed in [#19](https://github.com/Hissal/mattpocock-skills-unity/issues/19), part of [#1](https://github.com/Hissal/mattpocock-skills-unity/issues/1). Feeds the `unity-prototyping` skill and the prototype folder question in setup. Researched 2026-09-25.

Sourcing rule: every claim cites a primary source (docs.unity3d.com, git-scm.com) or is marked **observed**: checked on one Windows 11 machine on 2026-09-25 with Unity 6000.6.2f1, Unity CLI 1.0.0-beta.11 and `com.unity.pipeline` 0.7.0-exp.1, in the gitignored `unity-sandbox/` (URP blank, Mono backend, Enter Play Mode Options on with domain and scene reload off, Active Input Handling set to the Input System only), through a GUI editor opened with `unity open`. **Unverified** means neither documented nor observed.

Already recorded elsewhere, cited rather than repeated: `autoReferenced`, `defineConstraints`, asmdef code never seeing `Assembly-CSharp`, and unreferenced asmdefs still shipping are in [unity-assemblies.md](./unity-assemblies.md) sections 2, 3 and 5 and its sandbox checks. `.meta` GUIDs and how a reference is stored are in [unity-serialization.md](./unity-serialization.md) section 1. Driving Play mode through a connected editor is in [unity-debugging.md](./unity-debugging.md) section 1.

## Summary: what an agent most needs

1. **An asmdef with `defineConstraints: ["UNITY_EDITOR"]` and `autoReferenced: false` isolates a prototype**: its MonoBehaviours attach to scene objects and survive a save and reopen, `Assembly-CSharp` cannot see its types, it may use `UnityEditor` without `#if`, and a player build leaves it out (observed, checks 1 and 3).
2. **An Editor-platform asmdef cannot hold a prototype's MonoBehaviours**: attaching one fails (observed, check 2).
3. **A host scene opened additively stays clean** through entering Play, Play-mode edits and exiting; an edit in Edit mode dirties it, and then a save of all open scenes would write it (observed, check 4).
4. **A self-ignoring folder plus `git add -f` captures the prototype**, every `.meta` included, but the subfolder's own `.meta` sits in the parent folder and has to be named too (observed, check 5).
5. **Removing a prototype no outside file references leaves a clean compile and console** (observed, check 6).

## Isolation

- `defineConstraints` symbols "must all hold for the assembly to compile or be referenced", evaluated against the active platform. [Assembly Definition properties, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/class-AssemblyDefinitionImporter.html). `UNITY_EDITOR` is defined only in the Editor, so the assembly compiles there and never for a player.
- An Editor-only asmdef (only the Editor platform included) is Editor code, like a script in an `Editor` folder. [Special folder names, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/SpecialFolders.html)
- Asmdef code cannot reference `Assembly-CSharp`, so a prototype that needs types living there gets no asmdef; each of its files is wrapped in `#if UNITY_EDITOR` instead. See [unity-assemblies.md](./unity-assemblies.md) section 3 (observed there, with `autoReferenced: false` and an explicit reference both).

## Scenes and running

- `EditorSceneManager.OpenScene(path, OpenSceneMode.Additive)` opens a scene beside the ones already open, in Edit mode. [OpenScene, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SceneManagement.EditorSceneManager.OpenScene.html)
- `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()` asks the user whether to save modified open scenes and returns `false` when they cancel. [SaveCurrentModifiedScenesIfUserWantsTo, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo.html)
- `SceneManager.SetActiveScene` picks the scene new GameObjects are created in. [SetActiveScene, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html)
- `EditorApplication.EnterPlaymode()` enters Play mode from script. [EnterPlaymode, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/EditorApplication.EnterPlaymode.html)
- `[MenuItem("Prototypes/<name>")]` on a static method adds the menu item; the path's first segment is the top-level menu. [MenuItem, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/MenuItem.html)
- `GUIUtility.keyboardControl` is the id of the IMGUI control with keyboard focus, `0` when none has it. [keyboardControl, 6000.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/GUIUtility-keyboardControl.html)

## The prototype folder in git

- A `.gitignore` inside a folder applies to paths below it; `*` then `!.gitignore` ignores everything there except that file, so the folder exists on every clone while its contents never get committed by `git add -A`. [gitignore](https://git-scm.com/docs/gitignore)
- `git add -f` adds "otherwise ignored files". [git-add](https://git-scm.com/docs/git-add)
- A pattern with a leading `/` is relative to the directory of the `.gitignore` holding it. [gitignore](https://git-scm.com/docs/gitignore). **Observed** (scratch git repo, 2026-09-25): a nested project `Game/` whose own `.gitignore` held `/Assets/_Prototypes/` and `/Assets/_Prototypes.meta` ignored both `Game/Assets/_Prototypes/` and `Game/Assets/_Prototypes.meta`; the same two paths prefixed with `/Game` in the root `.gitignore` did the same; and `git add -f` on a prototype subfolder and its `.meta` staged them under either.
- Prior art, read-only on a real Unity repo (2026-09-25): a folder under `Assets/` holding a `.gitignore` of `*`, `!.gitignore` and one readme exception, with the folder's own `.meta` and the `.gitignore` committed, and everything else inside it untracked.

## Sandbox checks for the `unity-prototyping` skill

The six checks the design requires ([#19](https://github.com/Hissal/mattpocock-skills-unity/issues/19)), run for [#34](https://github.com/Hissal/mattpocock-skills-unity/issues/34). Probes lived in `Assets/_t34/`: `Proto/` held an asmdef `T34.Proto` (`autoReferenced: false`, `defineConstraints: ["UNITY_EDITOR"]`) with a MonoBehaviour, a pure C# class, a `Prototypes/...` menu item, an EditorWindow driver and an IMGUI variant switcher; `EdOnly/` held an asmdef with only the Editor platform and one MonoBehaviour; `Scenes/` held `Host.unity` (a cube) and `Proto.unity`. Assembly membership was read with `CompilationPipeline.GetAssemblies` through `run_script`.

1. **`defineConstraints: ["UNITY_EDITOR"]` keeps MonoBehaviours attachable and strips them from a player build.** `GameObject.AddComponent` of the prototype's MonoBehaviour worked in Edit mode, and after saving `Proto.unity` and reopening it the component was still there, not a missing script. The assembly referenced `UnityEditor` (the menu item and the EditorWindow compiled with no `#if`) and was absent from `GetAssemblies(AssembliesType.Player)`. A Windows player build (connected `build`, 33 s, 0 errors) had `Assembly-CSharp.dll` in `Managed/` and no `T34.Proto.dll`. **Confirmed.**
2. **An Editor-platform asmdef forbids attaching MonoBehaviours.** Both `GameObject.AddComponent` and `ObjectFactory.AddComponent` of the `EdOnly` MonoBehaviour failed with "Can't add script behaviour 'EdBehaviour' because it is an editor script. To attach a script it needs to be outside the 'Editor' folder." (logged as an error; `AddComponent` returned null). **Confirmed.**
3. **`autoReferenced: false` hides the prototype from `Assembly-CSharp`.** `Assembly-CSharp` listed no reference to `T34.Proto`. A script in `Assembly-CSharp` naming a prototype type failed with CS0246 ("The type or namespace name 'T34' could not be found"), which blocks the whole project's compile until removed. **Confirmed.**
4. **An additively loaded host scene stays clean when nothing in it is edited.** The menu item (`SaveCurrentModifiedScenesIfUserWantsTo`, open `Proto.unity` single, open `Host.unity` additive, `SetActiveScene(proto)`, `EnterPlaymode()`) entered Play with both scenes loaded, `Proto.unity` active, both `isDirty == false`, the host's cube reachable from Play mode, and the build profile's scene list unchanged. Moving the host's cube during Play, then exiting Play, left both scenes clean and `Host.unity`'s SHA-1 unchanged. This ran with scene reload off (Enter Play Mode Options on); with scene reload on, Unity reloads the open scenes from disk on entering Play, which should leave them equally clean, but that setting was not run. Moving it in Edit mode (with `Undo.RecordObject`) set the host's `isDirty` to true. **Confirmed**, with the Edit-mode case as the reason for the never-save rule.
5. **Self-ignoring `.gitignore` plus `git add -f` captures everything, `.meta` files included.** In a scratch git repo with `Assets/_Prototypes/.gitignore` of `*` and `!.gitignore` committed, a copy of the probe folder at `Assets/_Prototypes/Feel/` showed as ignored. `git add -f Assets/_Prototypes/Feel` staged all 23 files in it, 13 of them `.meta`. The folder's own `Assets/_Prototypes/Feel.meta` sits in the ignored parent, stayed ignored, and needed its own `git add -f`. **Confirmed**, with that path added to the capture step.
6. **Removing a subfolder with no outside GUID references leaves a clean compile and console.** Every `guid:` from the `.meta` files under `Assets/_t34/` (14) was grepped across `Assets/`, `ProjectSettings/` and `Packages/` outside that folder: no hits (the same grep inside the folder found the MonoBehaviour's GUID in `Proto.unity`, so the pattern matched). With a production scene open instead of the prototype's, deleting the folder and its `.meta` and recompiling gave 0 errors, 0 warnings and an empty console; the EditMode smoke test passed. **Confirmed.**

Found alongside, same run:

- `EditorApplication.ExecuteMenuItem("Prototypes/T34 Feel")` through `run_script` ran the menu item and returned `true`, so an agent with a connected editor can run a prototype's entry point the way the human will.
- The IMGUI switcher's `Show` (called through `run_script`) toggled the variants and logged the change, and Play mode logged no errors with Active Input Handling set to the Input System only. Its arrow keys and the text field focus guard were **not** driven by real key presses.

## Open questions / unverified

- A MonoBehaviour in `Assembly-CSharp` wrapped whole in `#if UNITY_EDITOR`, attached in a prototype scene: attachable in the Editor and harmless because the scene is not in the build, by the same reasoning as check 1, but not run.
- Whether switching away from a throwaway branch that tracks the prototype files removes them from the working tree when the working branch ignores them was not tried; the removal step deletes whatever is left either way.
