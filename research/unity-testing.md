# Research: Unity Test Framework (Unity 6)

Ticket: [#3](https://github.com/Hissal/mattpocock-skills-unity/issues/3), part of [#1](https://github.com/Hissal/mattpocock-skills-unity/issues/1). Feeds the `unity-testing` skill and the Unity adaptation of the TDD skill.

Researched 2026-09-23. Primary sources only: the Unity 6.3 LTS Manual (which now hosts the Test Framework user guide), the `com.unity.test-framework` package docs and source (registry tarball and the copy bundled with the Editor), the Performance Testing and Code Coverage package docs. Where the docs were silent or wrong, I ran a probe project on Unity 6000.3.20f1 (Test Framework 1.6.0) and say so. Anything neither documented nor observed is marked **unverified**.

## Summary: what an agent most needs

1. **Tests live in a test assembly.** Any `.asmdef` that references `nunit.framework.dll` (plus `UnityEngine.TestRunner`, and `UnityEditor.TestRunner` for Edit mode) is a test assembly. Edit mode = `"includePlatforms": ["Editor"]`. Anything else = Play mode. Tests cannot reference `Assembly-CSharp`, so code under test must be in its own asmdef. [edit-mode-vs-play-mode](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html)
2. **Default to Edit mode `[Test]`.** Plain `[Test]` runs synchronously in one Editor update and is the fastest loop. Use `[UnityTest]` (returns `IEnumerator`) only to skip frames or yield Editor instructions. [edit-mode-vs-play-mode](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html)
3. **Edit mode does not run the MonoBehaviour lifecycle.** A plain MonoBehaviour added in an Edit mode test gets no `Awake`, `OnEnable`, `Start` or `Update` (observed: all zero). Test pure C# logic there; test lifecycle-dependent behaviour in Play mode, where `Awake`/`OnEnable` fire synchronously on `AddComponent` and `Start`/`Update` after one `yield return null` (observed).
4. **`Destroy` vs `DestroyImmediate`.** In Edit mode tests use `Object.DestroyImmediate`. `Object.Destroy` logs an error there, and any unexpected error log fails the test (observed). In Play mode, `Destroy` is deferred: the object is still non-null in the same frame and null after one frame (observed).
5. **Command line:** `Unity.exe -batchmode -projectPath <p> -runTests -testPlatform EditMode|PlayMode|<BuildTarget> -testResults <file.xml> -logFile <file>`. Do **not** pass `-quit`. The project must not be open in another Editor. [run-tests-from-command-line](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/run-tests-from-command-line.html), [EditorCommandLineArguments](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)
6. **Exit codes:** `0` all passed (or no tests matched), `2` one or more tests failed, `3` run error, `4` unknown `-testPlatform`. Compile errors exit `1` before the Test Framework runs. Always read the results XML too, because "0 tests ran" also exits `0`. (Source code plus probe; the docs say there is no common definition of exit codes.)
7. **Single test or fixture:** `-testFilter "Namespace.Fixture.Method"` (semicolon list, unanchored regex, `!` negates). The flag is **case-sensitive**: the Manual spells it `-testfilter`, which is silently ignored and runs everything (observed). Anchor with `^...$` for an exact match. Also `-testCategory`, `-assemblyNames`.
8. **Cost:** every command-line run pays Editor startup, asset refresh and script compilation, plus a domain reload for Play mode. On an empty 6.3 project a warm Edit mode run took about 4 s wall clock and Play mode about 5 s; real projects are dominated by import and compile time and are much slower (unverified in general). Player tests add a full player build.

## Versions

| Unity | Test Framework | Where the user guide lives |
|---|---|---|
| 6000.0 LTS | 1.4.x / 1.5.x (exact bundled version per patch **unverified**) | [package docs 1.4](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/index.html) |
| 6000.3 LTS | 1.6.0 (bundled in 6000.3.20f1) | [Unity 6.3 Manual, Testing your code](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html) |
| 6000.5 | 1.7.0 (bundled in 6000.5.11f1) | Unity Manual |
| 6000.6 | 1.8.0 (bundled in 6000.6.2f1) | Unity Manual |
| 6000.7 beta | 1.9.0 (bundled in 6000.7.0b1) | Unity Manual |

- From 1.5.x the package docs site only hosts the scripting API; the user guide for Unity 6.2+ is in the Unity Manual. [package docs 1.9](https://docs.unity3d.com/Packages/com.unity.test-framework@1.9/manual/index.html)
- It is a **core package**: shipped with the Editor, "fixed to a single version matching the Editor version". [6.3 core package page](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.test-framework.html). Since 1.5.1 it lives in Unity's own source tree ("now it's an embedded package"). (CHANGELOG in the bundled package, `Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.test-framework/CHANGELOG.md`)
- The last registry release is 1.4.6 (2025-02-05), [registry metadata](https://packages.unity.com/com.unity.test-framework), [tarball](https://download.packages.unity.com/com.unity.test-framework/-/com.unity.test-framework-1.4.6.tgz). Newer versions only ship inside the Editor.
- **Gotcha:** a project created with bare `Unity.exe -batchmode -createProject` on 6000.3.20f1 had **no** `com.unity.test-framework` entry in `Packages/manifest.json`, so test assemblies failed to compile until I added `"com.unity.test-framework": "1.6.0"` (observed). Hub templates may differ (**unverified**). Check the manifest first.
- It embeds a custom NUnit based on 3.5. [introduction](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html)

Version notes on individual features: async `Task` tests since 1.3.0; `-randomOrderSeed` 1.3.6; `-retry`/`-repeat` 1.3.5; save results API, `LogAssert.Expect(string)` without severity 1.4.0; `[UnityOneTimeSetUp]`/`[UnityOneTimeTearDown]` 1.5.0; `IPrebuildSetupWithTestData` 1.6.0. (bundled CHANGELOG, see above; entries up to 1.4.6 are also in the [1.4.6 tarball](https://download.packages.unity.com/com.unity.test-framework/-/com.unity.test-framework-1.4.6.tgz))

## Edit mode, Play mode, Player

The mode is decided by the parent assembly's references and platforms, not by attributes. [edit-mode-vs-play-mode](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html)

| | Edit mode | Play mode (in Editor) | Player |
|---|---|---|---|
| asmdef platforms | `Editor` only | any other set (default all) | same Play mode assembly, built for a target |
| Can use `UnityEditor` API | yes | only inside `#if UNITY_EDITOR` | no |
| `[UnityTest]` driven by | `EditorApplication.update` | coroutine in the player loop | coroutine in the player loop |
| Test Runner tab | EditMode | PlayMode | Player |
| `-testPlatform` | `EditMode` (default) | `PlayMode` | a `BuildTarget` name, e.g. `StandaloneWindows64` |

Sources: [edit-mode-vs-play-mode](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html), [UnityTestAttribute API](https://docs.unity3d.com/Packages/com.unity.test-framework@1.9/api/UnityEngine.TestTools.UnityTestAttribute.html), [command-line reference](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-command-line.html), [setup-and-cleanup](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-setup-and-cleanup.html) (the `#if UNITY_EDITOR` note).

- **Edit mode:** "only run in the Unity Editor and have access to Editor code and runtime application code". They can drive Play mode themselves (`yield return new EnterPlayMode()`). The Manual also says "You can't run coroutines in Edit mode tests" (meaning `StartCoroutine`; `[UnityTest]` itself still works). [edit-mode-vs-play-mode](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html)
- **Play mode in Editor:** the Editor enters Play mode for the run and exits afterwards; `Application.isPlaying` is true. [course: Play mode tests](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/course/play-mode-tests.html)
- **Player:** builds and runs a player for the active build profile. Results come back to the Editor over the network (same network required); without that connection no XML results are produced. Some platforms cannot `Application.Quit`, so the player keeps running. [run Play mode tests in a Player](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/workflow-run-playmode-test-standalone.html). The test player is always a development build (`BuildOptions.Development | ConnectToHost | IncludeTestAssemblies | StrictMode`, `PlayerLauncher.cs` in the bundled 1.6.0 source; also stated in the [Performance Testing docs](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@6.7/manual/index.html)). The Editor waits for player heartbeats for 10 minutes by default (`-playerHeartbeatTimeout`). [command-line reference](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-command-line.html)
- Recommendation from Unity: use `[Test]` instead of `[UnityTest]` unless you must yield Editor instructions (Edit mode) or skip frames / wait (Play mode). [edit-mode-vs-play-mode](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html)

### What the runner does around a run (1.6.0 source)

From `UnityEditor.TestRunner/TestRun/TestJobRunner` task list in the bundled 1.6.0 source (same steps exist in the [1.4.6 tarball](https://download.packages.unity.com/com.unity.test-framework/-/com.unity.test-framework-1.4.6.tgz)):

- Prompts to save modified scenes (`SaveCurrentModifiedScenesIfUserWantsTo`; in batch mode dialogs are suppressed, [EditorCommandLineArguments](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)).
- **Edit mode:** opens a new unsaved scene with default GameObjects (camera and light), records an Undo group, and afterwards reverts all Undo operations down to that group and restores the previous scene setup.
- **Play mode:** creates an empty scene, saves it as `Assets/InitTestScene<guid>.unity` (observed in the import log), adds a `PlaymodeTestsController` GameObject, enters Play mode, runs, exits, deletes the scene and restores the previous scene setup.
- Runs `IPrebuildSetup` before and `IPostBuildCleanup` after.

Practical consequence: tests should still clean up after themselves; the Undo revert only covers Undo-recorded changes, and files or assets you create are yours to delete (the `fileCleanUpCheck` feature flag can turn leftover files into errors). [command-line reference, TestSettings featureFlags](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-command-line.html)

## Lifecycle: which messages fire where

### Documented rules

- "By default, MonoBehaviour event functions only execute at runtime." `[ExecuteAlways]` (or the older `[ExecuteInEditMode]`, or per instance `runInEditMode = true`) opts a class into Edit mode callbacks, and even then Edit mode calls are sparse: `Awake` when an instance is created, `Update` only when the Scene or Game view redraws. [ExecuteAlways](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ExecuteAlways.html), [ExecuteInEditMode](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ExecuteInEditMode.html), [runInEditMode](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour-runInEditMode.html)
- `Awake` runs when an active GameObject is created with `Instantiate`, on scene load, or on first activation, regardless of `enabled`. [Awake](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Awake.html). `OnEnable` runs after `Awake` and before `Start`. [OnEnable](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnEnable.html). `Start` runs "on the frame when a script is enabled just before any of the Update methods are called the first time". [Start](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Start.html)
- `Application.isPlaying` is true in a Player or in Editor Play mode. [isPlaying](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-isPlaying.html)

### Observed on 6000.3.20f1 / UTF 1.6.0 (probe project, plain MonoBehaviour, no `ExecuteAlways`)

| Scenario | Awake | OnEnable | Start | Update | isPlaying |
|---|---|---|---|---|---|
| Edit mode `[UnityTest]`: `new GameObject().AddComponent<T>()`, then two `yield return null` | 0 | 0 | 0 | 0 | false |
| Play mode `[UnityTest]`: right after `AddComponent<T>()` | 1 | 1 | 0 | n/a | true |
| Play mode: after one `yield return null` | 1 | 1 | 1 | 1 | true |

So in Edit mode tests, lifecycle methods on a plain MonoBehaviour never run. Either extract logic into plain C# classes and test those, call the methods directly (e.g. make them `internal` plus `InternalsVisibleTo`), or use Play mode.

### Destroy vs DestroyImmediate

- `Object.Destroy`: "Actual object destruction is always delayed until after the current Update loop, but always happens before rendering." [Object.Destroy](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Destroy.html)
- `Object.DestroyImmediate`: "intended for use in scripts that run in Edit mode ... In Edit mode, the usual delayed destruction performed by Destroy does not occur, so immediate destruction is necessary." [Object.DestroyImmediate](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.DestroyImmediate.html)
- Observed in an Edit mode `[Test]`: `Object.Destroy(go)` logged the error `Destroy may not be called from edit mode! Use DestroyImmediate instead.`, the object was not destroyed, and the test **failed** with "Unhandled log message ... Use UnityEngine.TestTools.LogAssert.Expect". The failing-on-error-log rule is documented: "A test fails if Unity logs a message other than a regular log or warning message." [asserting-and-comparing](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/asserting-and-comparing.html)
- Observed in a Play mode `[UnityTest]`: after `Object.Destroy(go)`, `go == null` was false in the same frame and true after `yield return null`.
- Rule of thumb for tests: TearDown in Edit mode uses `DestroyImmediate`; in Play mode `Destroy` works but assertions on destruction need a frame yield.

### Logs

- `LogAssert.Expect(LogType, string|Regex)` (or without `LogType` since 1.4.0) must be called **before** the code that logs, because the check runs at the end of each frame. Multiple expectations must appear in order. `LogAssert.ignoreFailingMessages = true` disables the error-log failure. [LogAssert API](https://docs.unity3d.com/Packages/com.unity.test-framework@1.9/api/UnityEngine.TestTools.LogAssert.html), [asserting-and-comparing](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/asserting-and-comparing.html)
- In async tests, failing log messages are only evaluated after the async test completes. [async tests](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-async-tests.html)

## Test assembly (asmdef) shape

- "Unity Test Framework tests must be in a test assembly, which is any assembly that references NUnit." The Test Runner's "Create a new Test Assembly Folder" (or Assets > Create > Testing > Test Assembly Folder) produces an asmdef whose inspector shows references to `nunit.framework.dll`, `UnityEngine.TestRunner` and `UnityEditor.TestRunner`; "The `UnityEditor.TestRunner` reference is only available for Edit mode tests." [create a test assembly](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/workflow-create-test-assembly.html)
- A test assembly cannot reference `Assembly-CSharp`; move code under test into its own asmdef. Test scripts must sit in the folder with the asmdef. [edit-mode-vs-play-mode](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html)
- The 6000.3.20f1 Editor templates (`Editor/Data/Resources/ScriptTemplates/20-...NewEditModeTestAssembly.asmdef.txt` and `21-...NewTestAssembly.asmdef.txt`) still use the legacy shorthand `"optionalUnityReferences": ["TestAssemblies"]`, the same shape the Manual shows. [edit-mode-vs-play-mode](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html)
- The explicit form, which compiled and ran on 6000.3.20f1 in the probe, and matches the example in the [asmdef file format reference](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-file-format.html):

```json
{
  "name": "MyGame.Tests.EditMode",
  "references": ["MyGame", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": ["Editor"],
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

Play mode variant: drop `"includePlatforms"` (empty means all platforms). `precompiledReferences` is ignored unless `overrideReferences` is true. [asmdef file format](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-file-format.html). References can also be GUIDs (`"GUID:..."`), same page.

- Selecting platforms other than Editor is what allows the assembly's Play mode tests to run on standalone players. [create a test assembly](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/workflow-create-test-assembly.html)
- Test assemblies are only included in a player build when `BuildOptions.IncludeTestAssemblies` is set or `PlayerSettings.playModeTestRunnerEnabled` (`TestBuildAssemblyFilter.cs`, bundled 1.6.0 source). The `UNITY_INCLUDE_TESTS` define constraint is the usual guard; a test assembly carrying it was observed left out of a normal Player build ([unity-assemblies.md](./unity-assemblies.md)).

## Writing tests: attributes and hooks

- `[UnityTest]` must return `IEnumerator`. `yield return null` skips one frame (one `EditorApplication.update` in Edit mode). [UnityTestAttribute](https://docs.unity3d.com/Packages/com.unity.test-framework@1.9/api/UnityEngine.TestTools.UnityTestAttribute.html), [course: UnityTest](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/course/unitytest-attribute.html)
- Edit mode yield instructions: `EnterPlayMode`, `ExitPlayMode`, `RecompileScripts`, `WaitForDomainReload`, custom `IEditModeTestYieldInstruction`. [yield instructions](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-custom-yield-instructions.html). Yielding one of these from a Play mode test throws "PlayMode test are not allowed to yield ..." (`EnumerableTestMethodCommand.cs`, bundled 1.6.0). An unexpected domain reload during a test fails it. [course: domain reload](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/course/domain-reload.html)
- In the Edit mode runner, only `null`, `AsyncOperation` and `IEditModeTestYieldInstruction` get special handling (`EditorEnumeratorTestWorkItem.cs`, bundled 1.6.0). Whether a runtime `WaitForSeconds` actually waits in Edit mode is **unverified**; do not rely on it.
- Play mode tests can yield `WaitForSeconds` etc., but Unity calls long-running tests "in general a bad practice" and recommends `[Explicit, Category("...")]` for them. [course: long running tests](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/course/long-running-tests.html)
- `MonoBehaviourTest<T>`: yield it to instantiate a MonoBehaviour implementing `IMonoBehaviourTest` and wait until `IsTestFinished`. [yield instructions](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-custom-yield-instructions.html)
- `[UnitySetUp]`/`[UnityTearDown]` (and `[UnityOneTimeSetUp]`/`[UnityOneTimeTearDown]` since 1.5.0) are `IEnumerator` versions of the NUnit hooks. On a domain reload inside a test, the non-Unity `[OneTimeSetUp]`/`[SetUp]` rerun; Unity ones do not. [setup and teardown](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-unitysetup-and-unityteardown.html). Full action order: [execution order](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-actions-outside-tests.html)
- Fields survive a domain reload only if `[SerializeField]`. [course: preserve test state](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/course/preserve-test-state.html)
- Default per-test timeout for Unity tests is 180 s (`k_DefaultTimeout = 1000 * 180`, `TimeoutCommand.cs` in bundled 1.6.0, `UnityWorkItem.cs` in the [1.4.6 tarball](https://download.packages.unity.com/com.unity.test-framework/-/com.unity.test-framework-1.4.6.tgz)); override with NUnit `[Timeout(ms)]` (mechanism from source reading, **unverified** by run).
- Scene content tests: open scenes with `EditorSceneManager.OpenScene` in Edit mode and restore with `EditorSceneManager.NewScene(...)` in `[TearDown]`. [course: scene-based tests](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/course/scene-based-tests.html)
- Unity comparers (`Vector3EqualityComparer` etc.) and a Unity `Is` constraint overlay exist in `UnityEngine.TestTools.Utils` / `UnityEngine.TestTools.Constraints`. [asserting-and-comparing](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/asserting-and-comparing.html)
- `[UnityPlatform]` includes/excludes tests per `RuntimePlatform`. [UnityPlatformAttribute](https://docs.unity3d.com/Packages/com.unity.test-framework@1.9/api/UnityEngine.TestTools.UnityPlatformAttribute.html)

## Async tests

- Write `[Test] public async Task Name()`. Async code runs on the main thread; the framework polls the task each update (Play mode) or each `EditorApplication.update` (Edit mode). [async tests](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-async-tests.html). Supported since 1.3.0 including SetUp and TearDown (CHANGELOG); `[SetUp]`/`[TearDown]` may return `Task` (`SetUpTearDownCommand.cs`, bundled 1.6.0).
- **Do not use `Assert.ThrowsAsync`**: it blocks the main thread and freezes the Editor. Use `try`/`catch` and assert. [async tests](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-async-tests.html)
- Returning Unity's `Awaitable` directly from a test method is not documented (the runner checks for `Task` and `IEnumerator`), so treat it as **unverified**; await it inside an `async Task` test instead.

## Running tests

### Test Runner window

Window > General > Test Runner, tabs EditMode / PlayMode / Player. Double-click a test or fixture, or right-click > Run, to run a subset. [run tests in the Test Runner window](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/workflow-run-test.html)

### Command line

```
Unity.exe -batchmode -projectPath <project> -runTests -testPlatform EditMode \
  -testResults <abs path>/results.xml -logFile <abs path>/editor.log \
  [-testFilter "<names or regex>"] [-testCategory "<cats>"] [-assemblyNames "<asm1;asm2>"]
```

[run from command line](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/run-tests-from-command-line.html), [command-line reference](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-command-line.html)

Key arguments (all from the [command-line reference](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-command-line.html) unless noted):

- `-runTests` starts the run (`-runEditorTests` is a legacy alias in source, `TestStarter.cs`).
- `-testPlatform EditMode | PlayMode | <BuildTarget>`; defaults to Edit mode when omitted.
- `-testFilter`: semicolon list of full test names or a regex over full names; `!` excludes. Parameterized variant: `"ClassName\.MethodName\(Param1,Param2\)"`.
- `-testCategory`: semicolon list or regex, `!` excludes; combined with `-testFilter` as AND.
- `-assemblyNames "A;B"`: assemblies without `.dll`.
- `-testResults <path>`: NUnit 3 XML. Default is the project root; in source the default file name is `TestResults-<ticks>.xml` (`ResultsSavingCallbacks.cs`, [1.4.6 tarball](https://download.packages.unity.com/com.unity.test-framework/-/com.unity.test-framework-1.4.6.tgz)).
- `-runSynchronously`: Edit mode only, runs everything in one Editor update and filters out `[UnityTest]` and Unity setup/teardown tests.
- `-orderedTestListFile`, `-randomOrderSeed`, `-repeat N`, `-retry N`, `-testSettingsFile`, `-playerHeartbeatTimeout`.
- `-quit` is **not** supported: "-quit causes the Editor to quit immediately, before in-progress tests have chance to complete". The runner exits the Editor itself when done. [EditorCommandLineArguments](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)
- Use `-batchmode` ("Always run Unity in batch mode when using command line arguments"). "You can't open a project in batch mode while the Editor has the same project open". `-nographics` skips GPU init (fine for logic tests, not for rendering). Batch mode without `-accept-apiupdate` skips the API updater. [EditorCommandLineArguments](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)
- `-logFile -` sends the Editor log to stdout. Same page.

#### Filter behaviour (source plus probe, 6000.3.20f1)

- Argument names are matched with exact, **case-sensitive** string comparison (`"-" + option.ArgName == argName` in `CommandLineOptionSet.cs`, identical in the [1.4.6 tarball](https://download.packages.unity.com/com.unity.test-framework/-/com.unity.test-framework-1.4.6.tgz)). The Manual table writes `-testfilter`; with that spelling the probe ran **all** tests. Use `-testFilter`.
- A value is only taken if the next token does not start with `-`, and values are split on `;` (`CommandLineOption.cs`).
- `-testFilter` values become NUnit full-name regex filters (`RuntimeTestRunnerFilter.cs`). They are unanchored: `-testFilter Add_` matched `LifecycleEditModeTests.Add_Works` (observed). `^Name$` with no regex metacharacters is optimized to an exact match. So:
  - one test: `-testFilter "^My.Namespace.MyFixture.MyTest$"` (dots are regex metacharacters, so strictly `\.`; the unescaped form also matches in practice)
  - one fixture: `-testFilter "^My\.Namespace\.MyFixture\."`
  - one namespace: `-testFilter "^My\.Namespace\."` (this example is from the `Filter.groupNames` API doc comment in source)
- In code, `TestRunnerApi` has separate `testNames` (exact) and `groupNames` (regex) filters. [run tests from code](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/extension-run-tests.html)

#### Exit codes

The Manual says: "There is currently no common definition for exit codes reported by individual Unity components under test." [command-line reference](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-command-line.html). The source defines them (`Executer.ReturnCodes`, bundled 1.6.0 and [1.4.6 tarball](https://download.packages.unity.com/com.unity.test-framework/-/com.unity.test-framework-1.4.6.tgz)), and the probe confirmed each:

| Code | Meaning | Probe |
|---|---|---|
| 0 | `Ok`: tests ran with no failures, **or no tests matched** (`CompletedJobWithoutAnyTestsExecuted`) | `-testFilter NoSuchTest` exited 0 with `total="0"` |
| 2 | `Failed`: one or more tests failed | yes |
| 3 | `RunError`: exception starting run, settings or ordered list file missing, no callbacks received | not triggered |
| 4 | `PlatformNotFoundReturnCode`: bad `-testPlatform` | `-testPlatform NotAPlatform` exited 4 |
| 1 | Editor-level: script compilation errors in batch mode ("Scripts have compiler errors") | yes; the Editor quit before the runner started |

Batch mode also "immediately exits with return code 1" on exceptions or failed operations. [EditorCommandLineArguments](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html). The Editor log ends with `Test run completed. Exiting with code N (...)`.

**Agent rule:** treat exit 0 as success only if the XML `<test-run ... total>` is greater than 0 and `failed="0"`.

#### Results XML

NUnit 3 format ([Test Result XML Format](https://docs.nunit.org/articles/nunit/technical-notes/usage/Test-Result-XML-Format.html), linked from the [command-line reference](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-command-line.html)). Observed root: `<test-run testcasecount total passed failed inconclusive skipped result="Passed|Failed(Child)" ...>`, nested `test-suite` (project, assembly, fixture), then `test-case` elements with `fullname`, `result`, `duration`, and for failures `<failure><message>` plus `<stack-trace>`; captured log output is attached to the case. Parse `//test-case[@result='Failed']`.

### Running from code or a live Editor

`TestRunnerApi.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = new[]{...} }))` runs tests inside an already-open Editor; callbacks give results. If no filter is given it runs all Edit mode tests; test mode and platform come from the first filter only. [run tests from code](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/extension-run-tests.html). This is the route for tooling that talks to an open Editor, since the command line cannot open a project that is already open.

JetBrains Rider can also run UTF tests. [run tests in the Test Runner window](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/workflow-run-test.html)

## Performance Testing package

- Package `com.unity.test-framework.performance`. Core package (bundled, no install) from **6000.6** (versions 6.6.0 for 6000.6, 6.7.0 for 6000.7). For 6000.0 and 6000.3 install **3.5.0** from the registry. Add a reference to `Unity.PerformanceTesting` in the test asmdef. [perf docs 6.7](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@6.7/manual/index.html), [registry](https://packages.unity.com/com.unity.test-framework.performance)
- Mark tests `[Test, Performance]` or `[UnityTest, Performance]`; `[Version("n")]` versions a test's results. [test attributes](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@6.7/manual/test-attributes.html)
- `Measure.Method(() => ...).WarmupCount(n).MeasurementCount(n).IterationsPerMeasurement(n).GC().Run()` for code, `yield return Measure.Frames()...Run()` for per-frame time (not supported in Edit mode tests), plus `Measure.Scope`, `Measure.ProfilerMarkers`, `Measure.Custom(SampleGroup, value)`. [measure-method](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@6.7/manual/measure-method.html), [measure-frames](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@6.7/manual/measure-frames.html). The docs disagree on the default `MeasurementCount` (7 on the writing-tests page, 9 on the Measure.Method page), so set it explicitly. [writing-tests](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@6.7/manual/writing-tests.html)
- Command line: add `-perfTestResults <path.json>`; otherwise JSON and XML go to `Application.persistentDataPath`. Results view: Window > General > Performance Test Report. [cmd-line-args](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@6.7/manual/cmd-line-args.html)
- Stability advice from Unity: aim for under 5% deviation, avoid sub-millisecond measurements, keep one quality level, disable VSync, close background apps. [writing-tests](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@6.7/manual/writing-tests.html), [index tips](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@6.7/manual/index.html)
- The package itself reports numbers; pass/fail thresholds are not built in (6.6.0 adds a `Threshold` property that is only used by a reporting endpoint). Regression checks need your own asserts or the external [Performance Benchmark Reporter](https://github.com/Unity-Technologies/PerformanceBenchmarkReporter/wiki). (bundled 6.6.0 CHANGELOG, [writing-tests](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@6.7/manual/writing-tests.html))

## Code Coverage package

- Package `com.unity.testtools.codecoverage`, latest 1.3.0 (2026-01-20), requires Unity 2021.3+. Not a core package; install via Package Manager. [registry](https://packages.unity.com/com.unity.testtools.codecoverage), [installing](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/InstallingCodeCoverage.html), [technical details](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/TechnicalDetails.html)
- Covers Edit mode and Play mode tests **in the Editor only**, not Player. Output is OpenCover XML plus optional HTML (ReportGenerator), SonarQube, Cobertura, LCOV and badges. Branch coverage is not implemented. [technical details](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/TechnicalDetails.html), [index](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/index.html)
- Accurate coverage needs Debug code optimization (`-debugCodeOptimization`) and Burst compilation disabled (`--burst-disable-compilation`). [using code coverage](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/UsingCodeCoverage.html)
- Batch mode: `-enableCodeCoverage -coverageResultsPath <dir> -coverageOptions "generateHtmlReport;assemblyFilters:+MyGame.*"`. By default only assemblies under `Assets` are included. Edit mode and Play mode must be separate runs; `dontClear` accumulates across them. Example from Unity: `Unity.exe -projectPath C:/MyProject -batchmode -testPlatform playmode -runTests -debugCodeOptimization -enableCodeCoverage -coverageResultsPath ... -coverageOptions "..."`. [batchmode](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/CoverageBatchmode.html), [with Test Runner](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/CoverageTestRunner.html)
- Exclude code with `[ExcludeFromCoverage]` (or .NET `ExcludeFromCodeCoverage`); skip tests under coverage with `[ConditionalIgnore("IgnoreForCoverage", "...")]`. "Enabling Code Coverage adds some overhead to the Editor." [using code coverage](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/UsingCodeCoverage.html), [with Test Runner](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3/manual/CoverageTestRunner.html)

## Typical run times and costs

Unity does not publish run-time figures. What is documented:

- Entering Play mode reloads the domain and the scene; "the amount of time increases as your scripts and scenes become more complex". [configurable enter play mode](https://docs.unity3d.com/6000.3/Documentation/Manual/configurable-enter-play-mode.html), [domain reloading](https://docs.unity3d.com/6000.3/Documentation/Manual/domain-reloading.html). Play mode test runs pay this; Edit mode `[Test]` runs do not (unless a test yields `EnterPlayMode`/`RecompileScripts`).
- 1.4.5 changed the Editor interaction mode and idle time during runs, "resulting in faster test runs". (bundled CHANGELOG)
- Player runs add a full development build, and the Editor may wait up to 10 minutes for heartbeats. [command-line reference](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-command-line.html)
- The Test Runner's suite duration excludes OneTimeSetUp/TearDown time. [run tests in the Test Runner window](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/workflow-run-test.html)

Measured (probe only, not a primary source): Unity 6000.3.20f1, empty project, Windows 11, `-batchmode -nographics`, wall clock for the whole Editor process:

| Run | Wall clock |
|---|---|
| Create empty project (`-createProject`) | asset refresh alone logged 19.8 s |
| First Edit mode run after adding the Test Framework (package resolve plus compile) | 11.9 s |
| Warm Edit mode run, 3 tests | 4.1 to 4.5 s |
| Warm Play mode run, 2 tests (3 domain reloads of about 0.7 to 0.8 s each in the log) | 5.2 to 5.3 s |
| Bad `-testPlatform` (fails fast) | 3.6 s |

Test execution itself was milliseconds (XML `duration` about 0.04 s for the Edit mode suite). The fixed cost is Editor startup, refresh and compile, so it scales with project size, package count and whether `Library/` is warm (scaling claim **unverified** with numbers). For an agent loop this means: batch many tests per Editor launch, filter to what changed, prefer Edit mode `[Test]`, and when an Editor is already open, run through it (Test Runner or `TestRunnerApi`) instead of launching a second one, which batch mode refuses anyway.

## Sandbox checks for the `unity-testing` skill

The seven checks the design requires ([#10](https://github.com/Hissal/mattpocock-skills-unity/issues/10), check 7 from [#15](https://github.com/Hissal/mattpocock-skills-unity/issues/15)), run for [#32](https://github.com/Hissal/mattpocock-skills-unity/issues/32) in `unity-sandbox/` on 6000.6.2f1 (Test Framework 1.8.0, CLI 1.0.0-beta.11, Windows 11, Enter Play Mode Options on with domain and scene reload off, the 6.6 default). **Observed** on 2026-09-25 through `unity test` (cold batch launch), unless marked pending.

1. **EditMode lifecycle**: `new GameObject().AddComponent<T>()` on a plain MonoBehaviour gave `Awake`, `OnEnable`, `Start`, `Update` all 0, both straight after `AddComponent` and after two `yield return null`; `Application.isPlaying` false. Calling the component's public method directly worked (the test passed). Same result as the 6000.3 probe above.
2. **`WaitForSeconds` in an EditMode `[UnityTest]`**: `yield return new WaitForSeconds(1.5f)` resumed after **0 ms** (Stopwatch). It does not wait.
3. **`Awaitable` as a test return type**: `[Test] public async Awaitable M()` ran its body to the end (both logs appeared, including the one after `await Awaitable.NextFrameAsync()`), then the test **failed** with `System.NullReferenceException` at `NUnit.Framework.Internal.AsyncInvocationRegion+AsyncTaskInvocationRegion.WaitForPendingOperationsToComplete`, not with the body's own `Assert.Fail`. Unsupported. `await Awaitable.NextFrameAsync()` inside `[Test] public async Task M()` passed.
4. **PlayMode statics with Enter Play Mode Options on and off**: **pending** (see below). EditMode side observed: a static incremented in one EditMode test was still incremented in the next test of the same run.
5. **Explicit asmdef templates on 6000.6**: the EditMode template (this file's shape, with the module referenced by name) compiled and ran. The PlayMode template: **pending**.
6. **Template-created project (URP blank) manifest**: **pending**. `unity-sandbox/` (created from `com.unity.template.urp-blank`, then `unity pipeline install`) lists `com.unity.test-framework` 1.8.0, but which step added it was not isolated.
7. **Run-cost timing**: recorded in [unity-verification.md](./unity-verification.md) section 11.

Found alongside, same run:

- `Object.Destroy` in an EditMode `[Test]` logged "Destroy may not be called from edit mode! Use DestroyImmediate instead. Destroying an object in edit mode destroys it permanently", left the object non-null, and failed the test with "Unhandled log message". Same as on 6000.3.
- In a plain `[Test]`, `LogAssert.Expect(LogType.Error, msg)` placed **after** the `Debug.LogError` still passed, as did placing it before: the whole synchronous body runs within one frame, before the end-of-frame check. The documented "before the code that logs" rule matters once a yield separates them; that `[UnityTest]` case (`Y_ExpectAfterYield`) is **pending**.

**Pending**: after the EditMode run above, a further sandbox run for checks 4, 5 (PlayMode) and 6, the `LogAssert` yield case and the timing matrix was stopped by the agent's permission layer and is waiting on the maintainer. The skill ships those claims hedged until they run.

## Open questions / unverified

- Exact Test Framework version bundled in each 6000.0 patch.
- Whether Hub project templates include `com.unity.test-framework` in the manifest (check 6, pending).
- Play mode tests with Enter Play Mode Options (domain reload disabled): the runner just sets `EditorApplication.isPlaying = true` (`EnterPlayModeTask.cs`), so it presumably honours the project setting, but no doc states it.
- Run-time scaling on real projects.
