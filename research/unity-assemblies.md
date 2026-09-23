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
8. **Static state may survive entering Play mode.** In 6.0 to 6.5 the default reloads the domain, but many projects turn it off. From Unity 6.6 the default is "Reload Scene only" (domain reload off). Without domain reload, statics and static event subscribers persist between Play sessions. Never assume a static starts at its initializer. [6.0 domain reload](https://docs.unity3d.com/6000.0/Documentation/Manual/domain-reloading.html), [6.6 play mode config](https://docs.unity3d.com/6000.6/Documentation/Manual/configurable-enter-play-mode.html)
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
- **Inference** (from `IsCompatibleWith` in [UnityCsReference](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Scripting/ScriptCompilation/CustomScriptAssembly.cs)): an Editor-only assembly is not compiled for Player targets, so a runtime assembly that references it compiles in the Editor but breaks the Player build. Keep references pointing from Editor to runtime, never the other way.

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
- **Inference**: the string passed to `InternalsVisibleTo` is the target asmdef's `name` field, since that is the compiled assembly name. The standard pattern is runtime assembly grants internals to its test assembly.
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
- Unity 6.5 and later add lifecycle attributes (namespace `Unity.Scripting.LifecycleManagement` for code reload, `UnityEngine` for `[OnEnteringPlayMode]`/`[OnExitingPlayMode]`, `UnityEditor` for Edit mode pairs), recommended for new code and "guaranteed to work with any future scripting runtimes". [6.6 code lifecycle](https://docs.unity3d.com/6000.6/Documentation/Manual/programming-code-lifecycle.html)
- Unity 6.5 and later add `[AutoStaticsCleanup]` / `[NoAutoStaticsCleanup]` on static fields: a source generator re-evaluates field initializers on entering Play mode (readonly collections get `Clear()`), requires the type to be `partial`, and does not re-run static constructors. An opt-in analyzer emits `UAL0010` to `UAL0014`. Both are toggled per assembly with `<asmdefName>.globalconfig` (`build_property.UnityEnableAutoStaticsCleanupCodeGen`, default true; `build_property.UnityEnableAutoStaticsCleanupAnalysis`, default false). [6.6 domain reload](https://docs.unity3d.com/6000.6/Documentation/Manual/domain-reloading.html) (6.5 page also documents the attributes: [6.5 domain reload](https://docs.unity3d.com/6000.5/Documentation/Manual/domain-reloading.html))

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

## Opinion: own skill, or fold into verification?

Own skill, but a small one. The failure modes here (missing asmdef reference, asmdef swallowing an `Editor` folder, runtime code touching `UnityEditor`, test assembly hiding production code, cycles, stale statics with domain reload off, a class/file name mismatch) are things an agent causes while *writing and moving* code, not only things it detects when checking. They need to be in context at edit time, which argues for a model-invoked `unity-assemblies` skill triggered by touching `.asmdef`/`.asmref`, adding scripts, or adding `using UnityEditor`. The recompilation and "did it really compile, including for the Player" part does belong in the verification skill: that skill should cite this one for the "refresh, wait for compile, check Player compile too" loop rather than restate it. Keep the skill to the summary rules plus the asmdef field table; the rest of this file can serve as its reference.
