# Symptoms per bug class

Exact messages as Unity 6000.6 prints them, and how to detect each class. The rules for fixing each class live in the skill the class table names.

## Serialization

- **Missing script** (a component whose script `.cs` and `.meta` are gone): the scene keeps the component's `m_Script` GUID. Opening the scene in Edit mode logs nothing. Entering Play with scene reload on logs the warning `The referenced script (Unknown) on this Behaviour is missing!` with an empty stack trace; with scene reload off, nothing. Detect it with the API, not the console: `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go)` over the scene's objects (`RemoveMonoBehavioursWithMissingScript` removes them).
- **Unassigned serialized reference**, in the Editor, on an object loaded from a scene: `UnassignedReferenceException: The variable target of Holder has not been assigned.`, then "You probably need to assign the target variable of the Holder script in the inspector." The same field on a component made with `AddComponent` at runtime, and in every player build, throws a plain `NullReferenceException`.
- **Destroyed object**: `MissingReferenceException: The object of type 'UnityEngine.Transform' has been destroyed but you are still trying to access it.`
- **The `== null` trap**: a destroyed `UnityEngine.Object` compares `== null` as true while `(object)obj == null` is false, because Unity overloads equality: test a Unity object's liveness with `== null` or its bool conversion.
- **Data lost after a rename**: a serialized field renamed without `FormerlySerializedAs` reads back as its default; nothing is logged.

## Second Play

With domain reload off, statics and static event subscriptions survive into the next Play session. Signatures: a bug that reproduces on the first Play and not the second, or the reverse; a counter that starts above zero; a one-shot flag already set; a handler that runs twice. Check `EditorSettings.enterPlayModeOptions` first when a repro is not repeatable, and run the Play loop twice from a fresh domain reload.

## Editor-only or Player-only

- `#if UNITY_EDITOR` code is left out of player compilation entirely, and `UnityEditor` APIs (`AssetDatabase` and the like) exist only in the Editor. A build that compiles can still behave differently where the Editor branch did setup work.
- **Stripping**: the linker removes code it cannot see being reached; reflection-only use is the usual victim. Preserve it with `[Preserve]` or `link.xml`.
- **IL2CPP**: no `System.Reflection.Emit`; reflection-only serialization and runtime-built generics (`MakeGenericType`, `MakeGenericMethod`) may lack generated code; exception-filter side effects run in a different order than on Mono; Web has no managed threads.
- Asserts and other `[Conditional]` diagnostics disappear from release builds, so side effects inside an assert change behaviour.
- A release stack has no file or line and can skip inlined frames: rebuild as a Development Build before reading it.

## Import

`Texture2D.GetPixels` and `EncodeToPNG` need `Texture.isReadable`, which is false by default for imported textures (Read/Write Enabled, `TextureImporter.isReadable`); textures created from script are readable. The Particle System Shape Module and Terrain Paint Detail need it, or the build fails. The connected editor's `get_import_settings` and `set_import_settings` read and change importer settings. The exact exception for an unreadable texture has not been observed here.

## Timing or order

- The order in which one event function (`Update`, `Start`) runs across objects is not guaranteed, even for instances of one script. Script Execution Order and `[DefaultExecutionOrder]` order types, the Editor setting wins over the attribute, equal orders stay undefined, and neither affects `[RuntimeInitializeOnLoadMethod]`, `OnDisable` or `OnDestroy`.
- The order between resumed coroutines and async continuations is not guaranteed.
- `Destroy` takes effect at the end of the frame; the object is still there for the rest of it.

These show up as flakiness across runs or machines, not a fixed failure: raise the reproduction rate (loop the scenario, vary the object count), then make the order explicit.
