# Research: Unity assemblies and compilation

Ticket: [#5](https://github.com/Hissal/mattpocock-skills-unity/issues/5), part of #1. Feeds the planned `unity-assemblies` skill.

Sources are the Unity Manual and Scripting API on docs.unity3d.com, plus the Unity-Technologies `UnityCsReference` GitHub source. The baseline is Unity 6.0 LTS (6000.0) docs. Where the latest docs (Unity 6.6, 6000.6, built 2026-09-21) differ, both are cited. The `UnityCsReference` master branch was at "Unity 6000.7.0b1" when read (2026-09-23). Anything not stated by a primary source is marked **unverified** or **inference**.

## Summary: the rules an agent most needs

1. **Everything without an asmdef lands in `Assembly-CSharp`** (or one of three sibling predefined assemblies, decided by `Editor` and `Plugins` folder names). A folder with an `.asmdef` pulls itself and all subfolders into that assembly, unless a subfolder has its own `.asmdef` or `.asmref`. [intro](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-intro.html)
2. **An asmdef swallows nested `Editor` folders.** Once a parent folder has an asmdef, its `Editor` subfolders stop going to `Assembly-CSharp-Editor` and go into the runtime assembly instead, which then breaks Player builds if they use `UnityEditor`. Give each `Editor` folder its own Editor-only asmdef (or an asmref to one). [intro](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-intro.html)
3. **Asmdef code can never reference `Assembly-CSharp`** (or any predefined assembly). Predefined assemblies can only see asmdefs with `autoReferenced: true`. Cycles between assemblies are a compile error: merge the types or refactor. [referencing](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-referencing.html). `autoReferenced: false` does not lift this: observed on 6000.6.2f1, an asmdef with `autoReferenced: false` and `"references": ["Assembly-CSharp"]` got no reference to it (the entry is dropped silently, no warning) and failed with CS0103 on an `Assembly-CSharp` type.
4. **Every type an assembly uses must come from an assembly it lists in `references`.** Missing reference means CS0246 "type or namespace not found", even though the type exists in the project. [referencing](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-referencing.html)
5. **`UnityEditor` cannot be referenced by Player code.** Use `#if UNITY_EDITOR` for small Editor-only bits inside runtime scripts, and Editor-only assemblies (`includePlatforms: ["Editor"]`) or `Editor` folders for whole files. Editor code that compiles fine in the Editor can still fail the Player build. [UnityEditor API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/UnityEditor.html), [creating assemblies](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-creating.html)
6. **`defineConstraints` gate the whole assembly; `versionDefines` define symbols only inside that assembly.** Constraints are ANDed per entry, support `!` and `||`, and are evaluated against the active platform. [inspector ref](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AssemblyDefinitionImporter.html), [includes](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-includes.html)
7. **Test assemblies are never in normal builds.** An assembly referencing `nunit.framework.dll`, `UnityEngine.TestRunner` and `UnityEditor.TestRunner` is a test assembly; production code placed there silently vanishes from builds. [creating assemblies](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-creating.html)
8. **Static state may survive entering Play mode.** In 6.0 to 6.5 the default reloads the domain, but many projects turn it off. From Unity 6.6 the default is "Reload Scene only" (domain reload off). Without domain reload, statics and static event subscribers persist between Play sessions. Never assume a static starts at its initializer. Reset it in a callback that fires on every Play entry either way: `[OnExitingPlayMode]`/`[OnEnteringPlayMode]` or `[AutoStaticsCleanup]` from 6.5 (the declaring type must be `partial`), `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` or `[InitializeOnEnterPlayMode]` before that; the `OnCode*` attributes fire only on a real code reload (section 8). [6.0 domain reload](https://docs.unity3d.com/6000.0/Documentation/Manual/domain-reloading.html), [6.6 play mode config](https://docs.unity3d.com/6000.6/Documentation/Manual/configurable-enter-play-mode.html)
9. **Recompilation happens on Asset Database refresh**, which by default follows Editor focus (Auto Refresh). Changing `.cs`, `.asmdef`, `.asmref`, `.rsp` or `.dll` files, scripting define symbols, or the active platform all recompile. An agent editing files from outside the Editor must trigger a refresh and wait for compile before trusting results. [refresh](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetDatabaseRefreshing.html)
10. **One MonoBehaviour or ScriptableObject per file, file name equals class name, one namespace per file.** Mismatches produce warnings or an unusable component. [naming scripts](https://docs.unity3d.com/6000.0/Documentation/Manual/naming-scripts.html)
11. **The language is C# 9 on Roslyn, with gaps** (no init-only setters, no covariant returns, no module initializers; records need a hand-declared `IsExternalInit` and must not be serialized). [C# compiler](https://docs.unity3d.com/6000.0/Documentation/Manual/csharp-compiler.html)

## 1. Predefined assemblies and special folders

Unity compiles scripts in four phases by folder location, each producing a predefined assembly and a `.csproj`. Empty phases produce nothing. [predefined assemblies](https://docs.unity3d.com/6000.0/Documentation/Manual/script-compile-order-folders.html)

| Phase | Assembly | Contains |
|---|---|---|
| 1 | `Assembly-CSharp-firstpass` | Runtime scripts in folders called `Plugins` |
| 2 | `Assembly-CSharp-Editor-firstpass` | Scripts in `Editor` folders anywhere inside top-level `Plugins` folders |
| 3 | `Assembly-CSharp` | All other scripts not inside an `Editor` folder |
| 4 | `Assembly-CSharp-Editor` | All remaining scripts inside `Editor` folders |

- "A script can reference anything compiled in its own compilation phase or an earlier one, but can't reference anything compiled in a later phase." So `Plugins` code cannot see `Assembly-CSharp`. [predefined assemblies](https://docs.unity3d.com/6000.0/Documentation/Manual/script-compile-order-folders.html)
- `Editor` folders are for scripts that "aren't available in Player builds at runtime". MonoBehaviours in an `Editor` folder cannot be attached to GameObjects. [reserved folder names](https://docs.unity3d.com/6000.0/Documentation/Manual/SpecialFolders.html)
- `Plugins` is "reserved for third-party plugins". [reserved folder names](https://docs.unity3d.com/6000.0/Documentation/Manual/SpecialFolders.html)
- The 6000.0 and 6000.6 pages only list `Plugins` for firstpass. `Standard Assets` is not mentioned in current docs (it was in older manuals); treat it as **unverified** for Unity 6.

## 2. Assembly Definition (`.asmdef`) and Assembly Reference (`.asmref`)

### Scope

- All scripts in the asmdef's folder and subfolders join that assembly, "unless a subfolder has its own Assembly Definition or Assembly Reference asset". [intro](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-intro.html)
- An `.asmref` in a non-child folder adds that folder's scripts to an existing asmdef's assembly (for example, gathering scattered `Editor` folders into one Editor assembly). [intro](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-intro.html)
- Scripts in custom assemblies "are no longer added to the default assemblies and can only access scripts in your other custom assemblies." [intro](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-intro.html)
- Compile order follows dependencies; "You can't specify the order in which compilation takes place." [intro](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-intro.html)
- Assembly names must be unique across the project; reverse-DNS naming is suggested. Renaming the `.asmdef` file does not change its `name` field. [inspector ref](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AssemblyDefinitionImporter.html), [UTF create test assembly](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/workflow-create-test-assembly.html)
- To find which assembly a script is in, select it: the Inspector's Assembly Information section shows it. [intro](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-intro.html)

### `.asmdef` JSON fields

From the [file format reference](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-file-format.html), the [Inspector reference](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AssemblyDefinitionImporter.html), and the serialized class `CustomScriptAssemblyData` in [UnityCsReference `CustomScriptAssembly.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Scripting/ScriptCompilation/CustomScriptAssembly.cs).

| Field | Default | Effect |
|---|---|---|
| `name` | required | Assembly name (no extension). Unique across the project. |
| `rootNamespace` | empty | Default namespace IDEs (Rider, Visual Studio) put in new scripts. Present in source and Inspector docs but missing from the file format table. |
| `references` | `[]` | Other asmdef assemblies, by name or `"GUID:<guid>"`. The Inspector's "Use GUIDs" is not serialized; it is inferred from the reference form. GUIDs are preferred because renames do not break them. |
| `includePlatforms` / `excludePlatforms` | `[]` | Platform names from `CompilationPipeline.GetAssemblyDefinitionPlatforms`. Only one may be non-empty (source throws "Both 'excludePlatforms' and 'includePlatforms' are set."). Only platforms with installed build support are valid. `"Editor"` is a platform. |
| `allowUnsafeCode` | `false` | Passes `/unsafe` to the compiler. |
| `autoReferenced` | `true` | Whether predefined assemblies (`Assembly-CSharp` etc.) reference this one automatically. "This has no effect on whether Unity includes the assembly in the build." |
| `noEngineReferences` | `false` | Do not add `UnityEngine` / `UnityEditor` references (pure C# assembly). |
| `overrideReferences` | `false` | When true, only the DLLs in `precompiledReferences` are referenced, instead of all precompiled assemblies. |
| `precompiledReferences` | `[]` | DLL file names including extension, for example `"nunit.framework.dll"`. |
| `defineConstraints` | `[]` | Symbols that must all hold for the assembly to compile or be referenced. |
| `versionDefines` | `[]` | Objects `{ "name", "expression", "define" }`: define a symbol when a package, module, or Unity version matches. |

Legacy field: `"optionalUnityReferences": ["TestAssemblies"]` is still read and converted to `autoReferenced: false`, `overrideReferences: true`, references to `UnityEngine.TestRunner` and `UnityEditor.TestRunner`, `nunit.framework.dll`, and define constraint `UNITY_INCLUDE_TESTS`. [UnityCsReference `CustomScriptAssembly.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Scripting/ScriptCompilation/CustomScriptAssembly.cs)

`.asmref` has one field, `reference`, holding the target assembly's name or `"GUID:<guid>"`. [file format](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-file-format.html)

## 3. References, `autoReferenced`, precompiled DLLs, cycles

- Default wiring: predefined assemblies reference every asmdef assembly and every precompiled plugin; asmdef assemblies reference all precompiled plugins. [referencing](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-referencing.html)
- Turning off `autoReferenced` means "the predefined assemblies don't recompile when you change code in the assembly but also means the predefined assemblies can't use code in this assembly directly." [referencing](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-referencing.html)
- A plugin DLL can also have Auto Referenced disabled in the Plugin Inspector; asmdefs can still reference it explicitly. [referencing](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-referencing.html)
- Because all asmdefs reference all precompiled DLLs by default, "Unity recompiles all your assemblies when you update any one of the precompiled assemblies". `overrideReferences` plus an explicit list avoids that. The dropdown shows DLLs for the active platform only, so repeat per platform. [referencing](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-referencing.html)
- Forbidden references (quoted list): asmdef to predefined assemblies; explicit references from predefined assemblies ("can only use code in auto-referenced assemblies"); "Cyclical references, which is when two assemblies reference each other. ... you must refactor your code to remove the cyclical reference or put the mutually referencing classes in the same assembly." [referencing](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-referencing.html)
- GUID references must be fixed if `.meta` files are deleted or files are moved outside the Editor without their metas. [referencing](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-referencing.html)
- **Hand-written `.meta`, observed** on 6000.6.2f1 (2026-09-24, `unity-sandbox/`, resident headless editor): a new `Probe.A.asmdef` with a hand-written `.meta` (`fileFormatVersion: 2`, a random lowercase 32-hex `guid:`, an `AssemblyDefinitionImporter:` block with `externalObjects: {}`, empty `userData`, `assetBundleName`, `assetBundleVariant`) kept that GUID on import, and a second asmdef referencing it as `"GUID:<that>"` compiled against it, with no editor round trip between writing the two. So an agent can generate the GUID and reference it at once, without falling back to a name reference.
- **The race, observed** (section 13, check 6): Unity deletes a `.meta` whose asset is missing when it refreshes, so a refresh between writing the `.meta` and the `.asmdef` deletes the `.meta` and the asset later gets a new GUID. Mitigation adopted for the skill: write both in one command, `.meta` first, re-read the `.meta` before using its GUID anywhere else.
- **Lookup cost**: an asmdef's GUID is the `guid:` line of its `.asmdef.meta`. Name to GUID is a grep for `"name": "<name>"` across `*.asmdef` plus reading the matched `.meta`; GUID to name is a grep for `guid: <guid>` across `*.asmdef.meta`. Registry and Git packages' asmdefs live only in `Library/PackageCache/`, absent on a cold checkout, so their GUIDs cannot be resolved there.
- **Inference** (from `IsCompatibleWith` in [UnityCsReference](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Scripting/ScriptCompilation/CustomScriptAssembly.cs)): an Editor-only assembly is not compiled for Player targets, so a runtime assembly that references it compiles in the Editor but breaks the Player build (observed, section 13, check 3). Keep references pointing from Editor to runtime, never the other way.

## 4. Editor vs runtime code and `#if UNITY_EDITOR`

- "The UnityEditor assembly implements the editor-specific APIs in Unity. It cannot be referenced by runtime code compiled into players." [UnityEditor namespace](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/UnityEditor.html)
- Three ways to keep Editor code out of Players: an `Editor` folder (predefined `Assembly-CSharp-Editor`), an asmdef with only the Editor platform included (can live anywhere, extra folders joined via asmref), or `#if UNITY_EDITOR` blocks inside runtime files. [creating assemblies](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-creating.html), [reserved folders](https://docs.unity3d.com/6000.0/Documentation/Manual/SpecialFolders.html)
- `#if` is a compile-time directive: code in a false branch is "omitted entirely", unlike a runtime `if`. [conditional compilation](https://docs.unity3d.com/6000.0/Documentation/Manual/platform-dependent-compilation.html)
- Useful built-in symbols: `UNITY_EDITOR`, `UNITY_EDITOR_WIN`/`OSX`/`LINUX`, `UNITY_STANDALONE`, `UNITY_ANDROID`, `UNITY_IOS`, `UNITY_WEBGL`, `UNITY_SERVER`, `DEVELOPMENT_BUILD`, `ENABLE_MONO`, `ENABLE_IL2CPP`, and version symbols such as `UNITY_6000_0_OR_NEWER`. [symbol reference](https://docs.unity3d.com/6000.0/Documentation/Manual/scripting-symbol-reference.html)
- The docs call `UNITY_64` "an unreliable test for 64-bit architecture" and suggest a runtime check instead. [conditional compilation](https://docs.unity3d.com/6000.6/Documentation/Manual/platform-dependent-compilation.html)
- A MonoBehaviour or ScriptableObject file cannot contain multiple namespaces "even if one is excluded with preprocessor directives". [naming scripts](https://docs.unity3d.com/6000.6/Documentation/Manual/naming-scripts.html)
- **Unverified**: wrapping serialized fields in `#if UNITY_EDITOR` makes the serialized layout differ between Editor and Player. This is a known pitfall but no Unity 6 manual page stating it was found in this pass.

## 5. Define constraints, version defines, custom symbols

### Define constraints

- "All the listed symbols must be defined for the assembly to compile. Constraints work like the `#if` preprocessor directive in C#, but on the assembly level." [inspector ref](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AssemblyDefinitionImporter.html)
- `!SYMBOL` negates; `A || B` within one entry means either. Separate entries are ANDed. [inspector ref](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AssemblyDefinitionImporter.html)
- Any built-in or custom symbol works, including symbols from `versionDefines`. They are evaluated for the currently active platform in Build Profiles. [inspector ref](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AssemblyDefinitionImporter.html), [includes](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-includes.html)
- Constraints decide both compiling and including in builds, "including Play mode in the Editor". [includes](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-includes.html)
- In source, an assembly whose constraints include `UNITY_INCLUDE_TESTS` is excluded from Player builds unless building with test assemblies, and invalid constraint syntax throws "Invalid Define Constraint". [UnityCsReference](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Scripting/ScriptCompilation/CustomScriptAssembly.cs)

### Version defines

- Resource can be `Unity`, a package, or a module; when the version expression matches, `define` is set. [includes](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-includes.html)
- "Symbols defined in the Assembly Definition are only in scope for the scripts in the assembly created for that definition." [includes](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-includes.html)
- Interval syntax: `[1.3,3.4.1]` inclusive, `(1.3.0,3.4)` exclusive, `[2.4.5]` exact, bare `2.1.0-preview.7` means minimum. "No spaces are allowed in an expression. No wildcard characters are supported." Example for Unity: `[6000.0,6000.2)`. Release-type order: `a < b < f = c < p < x`. [includes](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-includes.html)
- Typical use: optional package integration, where a `versionDefines` entry defines `HAS_FOO` and `defineConstraints: ["HAS_FOO"]` makes the whole integration assembly compile only when the package is installed. [includes](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-includes.html)

### Custom scripting symbols (project-wide)

From [custom scripting symbols](https://docs.unity3d.com/6000.0/Documentation/Manual/custom-scripting-symbols.html):

- `Assets/csc.rsp` with `-define:A;B` applies to all Editor and Player code regardless of build profile. Changes take effect only after a recompile (touch or reimport one script).
- Player Settings > Scripting Define Symbols: per platform. Build Profiles: per profile.
- `PlayerSettings.SetScriptingDefineSymbols` affects Editor scripts but only after the Editor regains control and recompiles, so setting symbols and calling `BuildPipeline.BuildPlayer` on the next line builds without them. `BuildPlayerOptions.extraScriptingDefines` applies to Player builds only.
- In batch mode "there's no mechanism to trigger recompilation of scripts"; required symbols must come from `csc.rsp` at startup.
- Scopes add up: `csc.rsp` + platform + build profile.

## 6. `InternalsVisibleTo` and assembly attributes

- The 6000.1 manual shows `[assembly: InternalsVisibleTo(...)]` alongside other assembly attributes, noting it "can be useful for testing", and recommends putting them in `AssemblyInfo.cs` next to the asmdef. [metadata 6000.1](https://docs.unity3d.com/6000.1/Documentation/Manual/assembly-definition-metadata.html)
- The 6000.0 and 6000.6 versions of that page drop the `InternalsVisibleTo` example but keep the `AssemblyInfo.cs` convention and `[assembly: UnityEngine.Scripting.Preserve]`. [metadata 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/assembly-definition-metadata.html)
- The string passed to `InternalsVisibleTo` is the target asmdef's `name` field, since that is the compiled assembly name (observed, section 13). The standard pattern is runtime assembly grants internals to its test assembly.
- `CompilationPipeline` lists all assemblies Unity builds, including asmdef-based ones. [metadata](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-metadata.html)

## 7. Test assemblies

- Identified by references to `nunit.framework.dll`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`. [creating assemblies](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-creating.html)
- "Test assemblies are not compiled as part of the regular build pipeline ... If you have production code that unexpectedly doesn't compile into your project build, double-check to make sure it's not in a test assembly." [creating assemblies](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-creating.html)
- Edit Mode tests: Editor-only platform. Play Mode tests in a Player need Any Platform or specific platforms, and cannot use `UnityEditor.TestRunner`. [UTF create test assembly](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/workflow-create-test-assembly.html)

## 8. Domain reload, Enter Play Mode options, static state

### Settings and defaults (this changed in Unity 6.6)

- Location: Edit > Project Settings > Editor > Enter Play Mode Settings > "When entering Play Mode": Reload Domain and Scene, Reload Scene only, Reload Domain only, Do not reload Domain or Scene. [6.0 config](https://docs.unity3d.com/6000.0/Documentation/Manual/configurable-enter-play-mode.html)
- Unity 6.0 to 6.5: default is Reload Domain and Scene. [6.0 config](https://docs.unity3d.com/6000.0/Documentation/Manual/configurable-enter-play-mode.html), [6.5 config](https://docs.unity3d.com/6000.5/Documentation/Manual/configurable-enter-play-mode.html)
- Unity 6.6: "Reload Scene only. This is the default option." and "By default Unity doesn't reload the scripting domain". Unity recommends leaving domain reload off to "prepare for Unity's adoption of the CoreCLR runtime, which doesn't have the domain reload concept." [6.6 config](https://docs.unity3d.com/6000.6/Documentation/Manual/configurable-enter-play-mode.html)
- **Unverified**: whether upgrading an existing project to 6.6 flips its stored setting, or the new default only applies to new projects. The setting is per project, so check the project's actual value rather than trusting a version default.

### Effects of domain reload off

From [6.0 domain reload](https://docs.unity3d.com/6000.0/Documentation/Manual/domain-reloading.html):

- "Static variables keep their values between Play mode sessions."
- "Static events keep their registered subscribers between Play mode sessions."
- Non-serialized fields keep Play mode values on return to Edit mode.
- No extra `OnEnable`/`OnDisable` for `[ExecuteInEditMode]`/`[ExecuteAlways]` scripts.
- Domain reload still happens on script changes during an Asset Database refresh, even with reload on Play disabled.

With scene reload off, objects are not recreated and constructors are not re-run, so non-serialized fields also persist, and startup time in the Editor stops representing the build. [scene reload](https://docs.unity3d.com/6000.0/Documentation/Manual/scene-reloading.html)

### Resetting statics

- Unity 6.0 to 6.4: runtime code uses `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` on a static method; Editor code uses `[InitializeOnEnterPlayMode]`. [6.0 domain reload](https://docs.unity3d.com/6000.0/Documentation/Manual/domain-reloading.html), [RuntimeInitializeLoadType](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/RuntimeInitializeLoadType.html)
- `SubsystemRegistration` runs "before the first scene is loaded"; order within one load type "is not guaranteed". [RuntimeInitializeOnLoadMethodAttribute](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html)
- `[InitializeOnLoad]` static constructors run on Editor load, after recompiles, and on entering Play mode only if domain reload is on. [InitializeOnLoadAttribute](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/InitializeOnLoadAttribute.html)
- Unity 6.5 and later add lifecycle attributes (namespace `Unity.Scripting.LifecycleManagement` for code reload, `UnityEngine` for `[OnEnteringPlayMode]`/`[OnExitingPlayMode]`, `UnityEditor.Scripting.LifecycleManagement` for the Edit mode pair), recommended for new code and "guaranteed to work with any future scripting runtimes". 6.6 resets statics with `[OnExitingPlayMode]`/`[OnEnteringPlayMode]` or `[AutoStaticsCleanup]`, and no longer shows `SubsystemRegistration` for it. [6.6 code lifecycle](https://docs.unity3d.com/6000.6/Documentation/Manual/programming-code-lifecycle.html), [6.6 domain reload](https://docs.unity3d.com/6000.6/Documentation/Manual/domain-reloading.html). Details in the next subsection.
- Unity 6.5 and later add `[AutoStaticsCleanup]` / `[NoAutoStaticsCleanup]` on static fields: a source generator re-evaluates field initializers on entering Play mode (readonly collections get `Clear()`), requires the type to be `partial`, and does not re-run static constructors. An opt-in analyzer emits `UAL0010` to `UAL0014`. Both are toggled per assembly with `<asmdefName>.globalconfig` (`build_property.UnityEnableAutoStaticsCleanupCodeGen`, default true; `build_property.UnityEnableAutoStaticsCleanupAnalysis`, default false). [6.6 domain reload](https://docs.unity3d.com/6000.6/Documentation/Manual/domain-reloading.html) (6.5 page also documents the attributes: [6.5 domain reload](https://docs.unity3d.com/6000.5/Documentation/Manual/domain-reloading.html))
- `[AutoStaticsCleanup]` details, from the [6.6 API page](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Scripting.LifecycleManagement.AutoStaticsCleanupAttribute.html) and the 6.6 domain reload page (read 2026-09-23):
  - No per-assembly opt-in: code generation defaults to true, so a `.globalconfig` is only needed to turn generation off or the analyzer on. Unity does not write one.
  - Applies to classes, structs, fields, properties and events; on a type, it resets all its static members.
  - A field gets its initializer re-applied, or its C# `default` when it has none; `= new()` builds a fresh instance. A `static readonly` collection (`List<T>`, `Dictionary<TKey,TValue>`, `HashSet<T>`, or any type with a parameterless `Clear`) keeps its instance and gets `Clear()`; its initializer must be omitted, `new()`, or the exact declared type. A readonly field of any other type is not documented.
  - The static constructor never re-runs; a type with `[AutoStaticsCleanup]` members and an explicit static constructor is UAL0014. Setup outside field initializers goes into an `[OnEnteringPlayMode]` method.
  - No performance statement anywhere.
  - **Contradiction**: the Manual says it resets "on entering Play mode"; the API page says "on entering or exiting Play mode". **Unverified** which.

### Code lifecycle attributes (Unity 6.5 and later)

Sources: [6.6 code lifecycle](https://docs.unity3d.com/6000.6/Documentation/Manual/programming-code-lifecycle.html) (the [6.5 page](https://docs.unity3d.com/6000.5/Documentation/Manual/programming-code-lifecycle.html) has the same tables), each attribute's 6.6 Scripting API page, and [LifecycleAttributeBase](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Scripting.LifecycleManagement.LifecycleAttributeBase.html).

| Attribute | Namespace | Fires (per Unity docs) | Older API it replaces |
|---|---|---|---|
| [`[OnCodeLoaded]`](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Scripting.LifecycleManagement.OnCodeLoadedAttribute.html) | `Unity.Scripting.LifecycleManagement` | After assemblies load: initial load (Editor and Player), and after each code reload (Editor only) | None ("No pre-existing equivalent") |
| [`[OnCodeInitializing]`](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Scripting.LifecycleManagement.OnCodeInitializingAttribute.html) | `Unity.Scripting.LifecycleManagement` | After managed objects are deserialized, before `Awake`/`OnEnable`; on initial load, "before initial scene loads" | `[InitializeOnLoad]`, `[InitializeOnLoadMethod]`, `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` |
| [`[OnCodeDeinitializing]`](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Scripting.LifecycleManagement.OnCodeDeinitializingAttribute.html) | `Unity.Scripting.LifecycleManagement` | Before managed objects are serialized for a code reload | None |
| [`[OnCodeUnloading]`](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Scripting.LifecycleManagement.OnCodeUnloadingAttribute.html) | `Unity.Scripting.LifecycleManagement` | Before assemblies unload for a code reload; also on shutdown (Editor and Player) | `AssemblyReloadEvents.beforeAssemblyReload` |
| [`[OnEnteringPlayMode]`](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/OnEnteringPlayModeAttribute.html) | `UnityEngine` | Entering Play mode, after a code reload in Play mode, and at Player startup | `PlayModeStateChange.EnteredPlayMode`; timing equals `[InitializeOnEnterPlayMode]` |
| [`[OnExitingPlayMode]`](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/OnExitingPlayModeAttribute.html) | `UnityEngine` | Exiting Play mode, before a code reload in Play mode, and before Player quit (same platform limits as `OnApplicationQuit`) | `PlayModeStateChange.ExitingPlayMode` |
| [`[OnEnteringEditMode]`](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Scripting.LifecycleManagement.OnEnteringEditModeAttribute.html) | `UnityEditor.Scripting.LifecycleManagement` | Returning to Edit mode, and after a code reload in Edit mode | `PlayModeStateChange.EnteredEditMode` |
| [`[OnExitingEditMode]`](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Scripting.LifecycleManagement.OnExitingEditModeAttribute.html) | `UnityEditor.Scripting.LifecycleManagement` | Before entering Play mode, and before a code reload in Edit mode | `PlayModeStateChange.ExitingEditMode` |

- **Shape**: each goes on a static, parameterless, `void` method. All derive from `LifecycleAttributeBase`, which derives from `Scripting.RequiredMemberAttribute` (so the linker keeps them). [LifecycleAttributeBase](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Scripting.LifecycleManagement.LifecycleAttributeBase.html)
- **The declaring type must be `partial`.** Every doc example uses a `partial` type but no page states the rule. Observed on 6000.6.2f1: a method in a non-`partial` static class fails to compile with `error UAC0031: Lifecycle method '...' must be declared in a partial class or partial struct`, for all eight attributes. The `UAC0031` analyzer ships in 6000.5.11f1, 6000.6.2f1 and 6000.7.0b1 (`Data/Tools/BuildPipeline/.../Unity.Analyzers.Common.dll`).
- **Versions**: the eight attributes, their manual page and their API pages exist from 6.5; the 6.4, 6.3 and 6.0 docs 404 on all of them. Observed in the bundled assemblies: none of the eight is in 6000.3.20f1; all are in 6000.5.11f1, 6000.6.2f1 and 6000.7.0b1. **Unverified**: 6.4 (no Editor installed; docs only).
- **Recommendation**: "It's recommended to adopt the newer APIs in new code." The 6.6 API pages for `[InitializeOnLoad]`, `[InitializeOnLoadMethod]` and `RuntimeInitializeLoadType.SubsystemRegistration` say "For new code, use OnCodeInitializingAttribute instead"; `playModeStateChanged` points at the new attributes. None of the older APIs is marked obsolete in 6.6. The attributes avoid event subscription leaks, and the Play mode pair lives in `UnityEngine` so runtime scripts need no `#if UNITY_EDITOR`. [6.6 code lifecycle](https://docs.unity3d.com/6000.6/Documentation/Manual/programming-code-lifecycle.html), [6.6 InitializeOnLoad](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/InitializeOnLoadAttribute.html)
- **Reset statics on exit**: 6.6 calls resetting on exit "often most efficient", but code that also runs in Edit mode can dirty statics after exit, so it must reset on entry instead. [6.6 domain reload](https://docs.unity3d.com/6000.6/Documentation/Manual/domain-reloading.html)
- **Ordered execution diagram**: [6.6 execution order](https://docs.unity3d.com/6000.6/Documentation/Manual/configurable-enter-play-mode-details.html) (the SVG shows the new attributes; its placement of `InitializeOnEnterPlayMode` differs from what was observed below).

#### Observed order, 6000.6.2f1

Logged from a probe in `unity-sandbox/` (batch-mode Editor, one GameObject spawned in `BeforeSceneLoad`). Asset import worker processes also load project code and fire `[OnCodeLoaded]`, `[OnCodeInitializing]`, `[InitializeOnLoadMethod]` and friends, so a callback with side effects (files, sockets) runs in several processes.

Entering Play mode, **domain reload on**:

1. `[OnExitingEditMode]`, then `playModeStateChanged(ExitingEditMode)`
2. `beforeAssemblyReload`, `[OnCodeDeinitializing]`, `[OnCodeUnloading]`
3. (reload) `[InitializeOnLoad]` static constructor, `[OnCodeLoaded]`, `[InitializeOnEnterPlayMode]`, `[OnCodeInitializing]`, `[InitializeOnLoadMethod]`, `afterAssemblyReload`
4. `[OnEnteringPlayMode]` (first callback with `isPlaying` true)
5. `SubsystemRegistration`, `AfterAssembliesLoaded`, `BeforeSplashScreen`, `BeforeSceneLoad`, then `Awake`, `OnEnable`, `AfterSceneLoad`, `Start`, then `playModeStateChanged(EnteredPlayMode)`

Entering Play mode, **domain reload off**: steps 2 and 3 vanish except `[InitializeOnEnterPlayMode]` (options `DisableDomainReload`), which runs right after `ExitingEditMode`; steps 4 and 5 are unchanged.

- So `[OnEnteringPlayMode]`, `[OnExitingPlayMode]`, `[InitializeOnEnterPlayMode]`, every `RuntimeInitializeOnLoadMethod` load type, and the Edit mode pair fire on every Play session with domain reload on or off. The four `OnCode*` attributes, `[InitializeOnLoad]`, `[InitializeOnLoadMethod]` and `AssemblyReloadEvents` fire only when code actually reloads.
- Exiting Play mode (either setting): `playModeStateChanged(ExitingPlayMode)`, `[OnExitingPlayMode]`, `[OnEnteringEditMode]`, `playModeStateChanged(EnteredEditMode)`. No code reload happened on exit.
- Script recompile in Edit mode: `beforeAssemblyReload`, `[OnExitingEditMode]`, `[OnCodeDeinitializing]`, `[OnCodeUnloading]`, (reload) `[InitializeOnLoad]`, `[OnCodeLoaded]`, `[OnCodeInitializing]`, `[InitializeOnLoadMethod]`, `afterAssemblyReload`, `[OnEnteringEditMode]`.
- **Unverified**: the same orders on 6.5 (docs match, not run), code reload during Play mode, and Player builds (docs say `[OnEnteringPlayMode]` runs at Player startup and `[OnExitingPlayMode]` before quit).

### Pre-6.5 fallbacks and Enter Play Mode Options history

- `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` (`UnityEngine`): the 6.0 manual's way to reset runtime statics; runs "before the first scene is loaded", and the Editor ensures the same invocations on entering Play mode. Observed above to fire on every Play entry with domain reload off. [6.0 domain reload](https://docs.unity3d.com/6000.0/Documentation/Manual/domain-reloading.html), [6.6 RuntimeInitializeOnLoadMethod](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html)
- `[InitializeOnEnterPlayMode]` (`UnityEditor`): static method, optionally taking `EnterPlayModeOptions`; "Use to reset static fields in Editor classes on Enter Play Mode without Domain Reload." Unchanged in 6.6 with no replacement note; the manual maps `[OnEnteringPlayMode]` to its timing, except `[OnEnteringPlayMode]` runs after `OnDisable` on Editor objects and `[InitializeOnEnterPlayMode]` before. [6.0 API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/InitializeOnEnterPlayModeAttribute.html), [6.6 code lifecycle](https://docs.unity3d.com/6000.6/Documentation/Manual/programming-code-lifecycle.html)
- `[InitializeOnLoad]` (`UnityEditor`, static constructor) and `[InitializeOnLoadMethod]`: run on every domain reload, before asset import completes, so asset loading there can return null. [6.6 InitializeOnLoad](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/InitializeOnLoadAttribute.html)
- Enter Play Mode Options first shipped in **2019.3** (with `[InitializeOnEnterPlayMode]`), marked "experimental" through 2020.1; the label is gone from 2020.2. The 2019.2 docs 404 on both pages. [2019.3 manual](https://docs.unity3d.com/2019.3/Documentation/Manual/ConfigurableEnterPlayMode.html), [2020.2 manual](https://docs.unity3d.com/2020.2/Documentation/Manual/ConfigurableEnterPlayMode.html)

## 9. What triggers recompilation

- Asset Database refresh happens when the Editor regains focus (if Auto Refresh is on), via Assets > Refresh, or via `AssetDatabase.Refresh`. Refresh imports and compiles "code-related files such as .dll, .asmdef, .asmref, .rsp, and .cs files", then reloads the domain (unless Refresh was invoked from a script). Generating a `.cs` file during refresh restarts it. [refresh](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetDatabaseRefreshing.html)
- Auto Refresh preference: Disabled, Enabled, or Enabled Outside Playmode. [asset pipeline prefs](https://docs.unity3d.com/6000.0/Documentation/Manual/preferences-asset-pipeline.html)
- Script Changes While Playing: Recompile And Continue Playing (default), Recompile After Finished Playing, Stop Playing And Recompile. [general prefs](https://docs.unity3d.com/6000.0/Documentation/Manual/preferences-general.html)
- Changing scripting define symbols in Player settings recompiles on Apply; `csc.rsp` changes need a manual recompile. [custom symbols](https://docs.unity3d.com/6000.0/Documentation/Manual/custom-scripting-symbols.html)
- Switching the active build target or profile recompiles all scripts on the next Editor update and cannot happen in batch mode for a non-active platform. [SwitchActiveBuildTarget](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/EditorUserBuildSettings.SwitchActiveBuildTarget.html), [SetActiveBuildProfile](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.Profile.BuildProfile.SetActiveBuildProfile.html)
- `CompilationPipeline.RequestScriptCompilation()` requests a recompile from code; `RequestScriptCompilationOptions.CleanBuildCache` forces a full rebuild (useful to re-see all warnings). [RequestScriptCompilation](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Compilation.CompilationPipeline.RequestScriptCompilation.html)
- Only changed assemblies and their dependents recompile, which is the main reason for asmdefs; `autoReferenced: false` and `overrideReferences` cut dependents further. [intro](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-intro.html), [referencing](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definitions-referencing.html)
- Compile errors on startup can put the Editor into Safe Mode (a preference controls the confirmation dialog). [asset pipeline prefs](https://docs.unity3d.com/6000.0/Documentation/Manual/preferences-asset-pipeline.html)

## 10. Roslyn analyzers and source generators

- Both are imported as managed plugin DLLs. Set up: target .NET Standard 2.0; in the Plugin Inspector uncheck Any Platform and uncheck Editor, Standalone, and every other platform; give the DLL the asset label `RoslynAnalyzer` (exact, case sensitive). [source generator](https://docs.unity3d.com/6000.0/Documentation/Manual/create-source-generator.html), [install existing](https://docs.unity3d.com/6000.0/Documentation/Manual/install-existing-analyzer.html)
- "Your source generator must use Microsoft.CodeAnalysis.Csharp 4.3 to work with Unity." Unchanged in the 6.6 docs. [source generator 6.6](https://docs.unity3d.com/6000.6/Documentation/Manual/create-source-generator.html)
- The docs only show `ISourceGenerator`. **Unverified**: `IIncrementalGenerator` support (the API exists in Roslyn 4.x, but no Unity page confirms it).
- Scope: a DLL in the `Assets` root applies to all predefined assemblies. A DLL inside an asmdef folder applies "only ... to that assembly, and to any other assembly that references it." [analyzer scope](https://docs.unity3d.com/6000.0/Documentation/Manual/analyzer-scope-and-diagnostics.html)
- A generator injected into several assemblies causes CS0436 conflicts; make generated types `internal` or uniquely named. [source generator](https://docs.unity3d.com/6000.0/Documentation/Manual/create-source-generator.html)
- Rule sets: `Assets/Default.ruleset` applies to all assemblies; `Assets/<PredefinedAssemblyName>.ruleset` overrides for `Assembly-CSharp` and siblings (only those five names are allowed in the Assets root); a `.ruleset` next to an `.asmdef` overrides for that assembly (file name need not match). [analyzer scope](https://docs.unity3d.com/6000.0/Documentation/Manual/analyzer-scope-and-diagnostics.html)
- Analyzers "are only compatible with the IDEs that Unity supports." [Roslyn analyzers](https://docs.unity3d.com/6000.0/Documentation/Manual/roslyn-analyzers.html)

## 11. C# language version

Roslyn, C# 9.0 in both 6.0 and 6.6 docs. Not supported: suppress localsinit, covariant return types, module initializers, extensible calling conventions for function pointers, init-only setters. Records need `System.Runtime.CompilerServices.IsExternalInit` declared by hand, and "You shouldn't use C# records in serialized types". [C# compiler 6.0](https://docs.unity3d.com/6000.0/Documentation/Manual/csharp-compiler.html), [6.6](https://docs.unity3d.com/6000.6/Documentation/Manual/csharp-compiler.html)

## 12. Script file name and class name rules

From [naming scripts](https://docs.unity3d.com/6000.0/Documentation/Manual/naming-scripts.html):

- New scripts use the file name as the class name, and file name matching class name is "good practice".
- For MonoBehaviour and ScriptableObject, Unity can resolve a mismatched name with limits: with several classes in one file it picks the one matching the file name; for a `partial` MonoBehaviour only the file named like the class can be used as a Component; ambiguous cases log a warning.
- A file defining a MonoBehaviour or ScriptableObject cannot contain multiple namespaces; Unity then does not recognise the class and it cannot be used on GameObjects.
- **Agent rule (inference from the above)**: always one MonoBehaviour or ScriptableObject per file, file name identical to class name, one namespace per file. Renaming a class means renaming the file (and keeping its `.meta`, so the script GUID and scene references survive).

## 13. Sandbox checks for the `unity-assemblies` skill

Run on 6000.6.2f1 with CLI 1.0.0-beta.11 (2026-09-25, `unity-sandbox/`, ticket #30), through a GUI editor opened with `unity open` (default preferences, so Auto Refresh on), and headless `unity build --target StandaloneWindows64` with that editor closed. Every test assembly set `autoReferenced: false`; assembly membership was read with `CompilationPipeline.GetAssemblies` through `run_script`.

1. **Package code without an asmdef is ignored.** An embedded package `Packages/com.t30.noasmdef` with one `.cs` file and no asmdef: the Editor log says "Script 'Packages/com.t30.noasmdef/Runtime/T30Loose.cs' will not be compiled because it exists outside the Assets folder and does not to belong to any assembly definition file." (Unity's wording), logged on each import. No compile error, no console warning, and the file is in no assembly.
2. **A nested `Editor` folder under a runtime asmdef is swallowed.** `T30.C2/Editor/T30C2EditorThing.cs` calling `AssetDatabase` compiled into `T30.C2` (the runtime assembly) and the Editor compile was clean. The Player build failed with `CS0103: The name 'AssetDatabase' does not exist in the current context`. The `using UnityEditor;` line itself raised no Player error; only the first use of an Editor API did. Adding an asmdef in that `Editor` folder with `includePlatforms: ["Editor"]` and a `GUID:` reference to `T30.C2` made the next Player build pass.
3. **A runtime asmdef referencing an Editor-only asmdef** (`includePlatforms: ["Editor"]`) and using its type: clean Editor compile; the Player build failed with `CS0234: The type or namespace name 'Ed' does not exist in the namespace 'T30.C3' (are you missing an assembly reference?)`. The Editor-only assembly is absent from `GetAssemblies(AssembliesType.Player)`.
4. **`versionDefines` plus `defineConstraints`.** An asmdef with a `versionDefines` entry on the installed `com.unity.inputsystem` (expression `1.0.0`) and that symbol in `defineConstraints` compiled in the Editor and shipped in the Player (`T30.C4.Present.dll` in `Managed/`). The same shape keyed on a package name that is not installed (`com.t30.absent`) produced no assembly at all: its source, which does not compile, raised no error. The package-absent case used a missing package name rather than uninstalling one; the constraint sees the same thing either way (no matching package).
5. **`scripts/asmdefs.sh`.** On the sandbox it listed 104 assemblies, matching a `find` over `Assets/`, `Packages/` and `Library/PackageCache/` that skips `~` and hidden folders, then 115 with the check assemblies present; hand-checked GUIDs (`Unity.InputSystem`, `Unity.Collections`, `Unity.Burst`, the test runner pair) resolved to the right names. References to assemblies that do not exist (`Unity.ugui` in the Input System asmdef, `Unity.Mathematics`, which is not installed) printed with `?`. With `Library/PackageCache` moved aside, and on a copy holding only `Assets/` and `Packages/`, it printed the missing-cache line on stderr and package references as `?`. A package-shaped root (a `package.json` with `unity`, no `Assets/`), key order other than `name` first, a `versionDefines` entry with its own `name`, a CRLF `.meta` and an asmdef without `.meta` (`no-meta`) all came out right.
6. **The `.meta` race, GUI editor.** (a) A lone `T30.C6.Lone.asmdef.meta` with no asset survived 15 seconds while the unfocused editor idled, and was deleted by the next refresh (`AssetDatabase.Refresh`, what regaining focus runs), with the console warning "A meta data file (.meta) exists but its asset '...' can't be found." (b) Writing the `.meta` then the `.asmdef` in one command kept the hand-written GUID through import and compile; so did an `.asmref` with an `AssemblyDefinitionReferenceImporter` `.meta`, and a script beside that `.asmref` compiled into the referenced assembly.

Extra observations from the same session:

- **Unresolved references are dropped silently.** An asmdef with `"references": ["T30.DoesNotExist", "GUID:ffffffffffffffffffffffffffffffff"]` compiled with no error and no warning, and shipped in the Player.
- **`InternalsVisibleTo`** with the target asmdef's `name` (`[assembly: InternalsVisibleTo("T30.Vis.User")]` in an `AssemblyInfo.cs` beside the asmdef) let that assembly use an `internal` type.
- **A reference cycle** (two asmdefs referencing each other by GUID) logged "One or more cyclic dependencies detected between assemblies: <both asmdef paths>" in the console, yet `unity recompile --json` exited **0** with `compilationFailed: true` and an empty `errors` array. After the cycle files were deleted, the next `unity recompile` still said `compilationFailed: true` with no errors; the cause (a stale flag or something else) was not pinned down.
- **A missing reference** to an existing type whose parent namespace the assembly already has gave `CS0234 ... (are you missing an assembly reference?)`; `unity recompile` exited 6 with it in `errors`. `CS0246` (no part of the namespace visible) was not reproduced.
- **Test assemblies stay out of the Player.** An asmdef referencing `UnityEngine.TestRunner` and `nunit.framework.dll` with `defineConstraints: ["UNITY_INCLUDE_TESTS"]`, holding a plain production class, is listed by `GetAssemblies(AssembliesType.Player)` but its DLL was absent from the built `Managed/` folder.
- **Unreferenced asmdefs still ship**: every runtime check assembly (all `autoReferenced: false`, none used by a scene) was in `Managed/`.
- **`#if UNITY_EDITOR` around `using UnityEditor` and its use** in a runtime asmdef built for the Player.
- An `AssetDatabase.GetAssetPath(int)` call is **CS0619** (obsolete as error) on 6000.6.2f1. A GUI editor opened on the project with that error showed the modal "Enter Safe Mode?" dialog (Enter Safe Mode, Ignore, Quit) and waited there, unreachable by `unity status`, until a human chose Ignore.
- The first headless Player build left an empty `Assets/Resources/` folder (with `.meta`) behind; the cause was not identified.

## Opinion: own skill, or fold into verification?

Own skill, but a small one. The failure modes here (missing asmdef reference, asmdef swallowing an `Editor` folder, runtime code touching `UnityEditor`, test assembly hiding production code, cycles, stale statics with domain reload off, a class/file name mismatch) are things an agent causes while *writing and moving* code, not only things it detects when checking. They need to be in context at edit time, which argues for a model-invoked `unity-assemblies` skill triggered by touching `.asmdef`/`.asmref`, adding scripts, or adding `using UnityEditor`. The recompilation and "did it really compile, including for the Player" part does belong in the verification skill: that skill should cite this one for the "refresh, wait for compile, check Player compile too" loop rather than restate it. Keep the skill to the summary rules plus the asmdef field table; the rest of this file can serve as its reference.
