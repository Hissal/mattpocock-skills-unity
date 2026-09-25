# Code lifecycle reference

## The eight lifecycle attributes (6.5 and later)

Each goes on a static, parameterless, `void` method in a `partial` type (`UAC0031` otherwise). None exists before 6.5.

| Attribute | Namespace | Fires | Older API it replaces |
|---|---|---|---|
| `[OnCodeLoaded]` | `Unity.Scripting.LifecycleManagement` | After assemblies load: initial load (Editor and Player), and after each code reload (Editor) | None |
| `[OnCodeInitializing]` | `Unity.Scripting.LifecycleManagement` | After managed objects are deserialized, before `Awake`/`OnEnable`; on initial load, before the first scene loads | `[InitializeOnLoad]`, `[InitializeOnLoadMethod]`, `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` |
| `[OnCodeDeinitializing]` | `Unity.Scripting.LifecycleManagement` | Before managed objects are serialized for a code reload | None |
| `[OnCodeUnloading]` | `Unity.Scripting.LifecycleManagement` | Before assemblies unload for a code reload, and on shutdown | `AssemblyReloadEvents.beforeAssemblyReload` |
| `[OnEnteringPlayMode]` | `UnityEngine` | Entering Play mode, after a code reload in Play mode, and at Player startup | `PlayModeStateChange.EnteredPlayMode`; timing of `[InitializeOnEnterPlayMode]` |
| `[OnExitingPlayMode]` | `UnityEngine` | Exiting Play mode, before a code reload in Play mode, and before Player quit | `PlayModeStateChange.ExitingPlayMode` |
| `[OnEnteringEditMode]` | `UnityEditor.Scripting.LifecycleManagement` | Returning to Edit mode, and after a code reload in Edit mode | `PlayModeStateChange.EnteredEditMode` |
| `[OnExitingEditMode]` | `UnityEditor.Scripting.LifecycleManagement` | Before entering Play mode, and before a code reload in Edit mode | `PlayModeStateChange.ExitingEditMode` |

The Play mode pair lives in `UnityEngine`, so runtime scripts use it without `#if UNITY_EDITOR`. The Edit mode pair is Editor-only.

## What fires on a Play session

| Fires on every Play entry, reload on or off | Fires only when code actually reloads |
|---|---|
| `[OnExitingEditMode]`, `[OnEnteringPlayMode]`, `[OnExitingPlayMode]`, `[OnEnteringEditMode]`, the `[AutoStaticsCleanup]` reset, `[InitializeOnEnterPlayMode]`, every `RuntimeInitializeOnLoadMethod` load type | `[OnCodeLoaded]`, `[OnCodeInitializing]`, `[OnCodeDeinitializing]`, `[OnCodeUnloading]`, `[InitializeOnLoad]`, `[InitializeOnLoadMethod]`, `AssemblyReloadEvents` |

Exiting Play mode never reloads code, with either setting.

## Observed orders

Entering Play mode, reload off:

1. `[OnExitingEditMode]`, `playModeStateChanged(ExitingEditMode)`, `[InitializeOnEnterPlayMode]`
2. The `[AutoStaticsCleanup]` reset
3. `[OnEnteringPlayMode]` (the first callback with `Application.isPlaying` true)
4. `SubsystemRegistration`, `AfterAssembliesLoaded`, `BeforeSplashScreen`, `BeforeSceneLoad`, `Awake`, `OnEnable`, `AfterSceneLoad`, `Start`, `playModeStateChanged(EnteredPlayMode)`

With reload on, a full code reload (`[OnCodeDeinitializing]`, `[OnCodeUnloading]`, then `[InitializeOnLoad]`, `[OnCodeLoaded]`, `[InitializeOnEnterPlayMode]`, `[OnCodeInitializing]`, `[InitializeOnLoadMethod]`) runs between steps 1 and 3.

Exiting Play mode: `playModeStateChanged(ExitingPlayMode)`, `[OnExitingPlayMode]`, the `[AutoStaticsCleanup]` reset, `[OnEnteringEditMode]`, `playModeStateChanged(EnteredEditMode)`.

The reset is itself registered as a lifecycle callback, and the order between callbacks on the same transition is not documented. Keep each static under one mechanism.

## `[AutoStaticsCleanup]` in detail

- Takes no arguments: there is no enter-only or exit-only form.
- On a member: a field gets its initializer again (`= new()` builds a fresh instance), or `default` with no initializer; a property or event likewise. An event resets to `null`, dropping every subscriber.
- A `static readonly` field keeps its instance and gets `Clear()`, when its type is a collection or has a parameterless `Clear()`; its initializer must be omitted, `new()`, or the exact declared type. Any other readonly type breaks generation for the whole assembly (warning `CS8785`).
- On a type: every static member resets, except those marked `[NoAutoStaticsCleanup]`.
- Works with no `.globalconfig`, in an asmdef assembly and in `Assembly-CSharp`. A `<asmdefName>.globalconfig` can turn generation off (`build_property.UnityEnableAutoStaticsCleanupCodeGen`) or the UAL analyzer on (`build_property.UnityEnableAutoStaticsCleanupAnalysis`); the analyzer flags a static constructor next to cleaned members as `UAL0014`.

## Pre-6.5 mapping

| Need | Before 6.5 |
|---|---|
| Reset runtime statics on each Play entry | `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` |
| Reset Editor statics on each Play entry | `[InitializeOnEnterPlayMode]` (Editor-only, optionally taking `EnterPlayModeOptions`) |
| Run Editor code on load and after each recompile | `[InitializeOnLoad]` static constructor, `[InitializeOnLoadMethod]` |
| Run Editor code before a code reload | `AssemblyReloadEvents.beforeAssemblyReload` |
| React to Play and Edit mode changes in Editor code | `EditorApplication.playModeStateChanged` |

A fallback reset assigns every static back by hand, event fields included (`= null`).
