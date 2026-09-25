# Research: merging Unity assets

Ticket: [#6](https://github.com/Hissal/mattpocock-skills-unity/issues/6), part of #1. Feeds the Unity delta of the `resolving-merge-conflicts` skill.

Versions checked: Unity manual 6000.3 (6.3 LTS), cross-checked against 6000.0 and 2022.3 where noted; UnityYAMLMerge shipped with Editor 6000.3.20f1 (its `mergespecfile.txt` and `mergerules.txt` are byte-identical in 6000.5.11f1 and 6000.7.0b1); Git 2.55 docs on git-scm.com; Git LFS 3.7.1 locally, docs from `main` (3.8.0).

Evidence labels used below:

- **[doc]** stated by the cited primary source.
- **[tool]** observed by running the shipped `UnityYAMLMerge.exe` (6000.3.20f1) on synthetic three-way inputs, or read from the text files that ship next to it. Primary in the sense that it is Unity's own shipped artifact, but undocumented behaviour can change between versions.
- **[binary]** a message string found in the shipped Editor binary (`Unity.dll`, 6000.3.20f1). The string exists; the exact trigger is inferred.
- **[unverified]** community reports or inference, not confirmed by a primary source.

## 1. Rules an agent needs first (per file type)

The upstream skill says "resolve every hunk, never abort". For Unity that is wrong in four situations: the file is binary or LFS (there are no hunks, only a side to pick), a UnityYAMLMerge driver reported a conflict (the file has **no markers** but is not resolved), a `.meta` conflict touches the `guid:` line (picking by text rules silently breaks references), and a scene or prefab conflict is structural (hierarchy, prefab overrides) where a hand-edit of YAML can produce a file Unity cannot load. In those cases the correct move is to pick a side deliberately, re-run the Unity tool, or stop and hand back to the human.

| File type | Mergeable? | Agent rule |
|---|---|---|
| `.unity`, `.prefab` (Force Text YAML) | Yes, semantically, with UnityYAMLMerge. | Prefer UnityYAMLMerge over line merge. If it still reports conflicts, the listed properties are the conflicts; resolve those property by property, never by hand-merging whole YAML documents. Structural conflicts (reparenting, added/removed components, prefab override lists): stop and ask. |
| Other Unity YAML (`.asset`, `.mat`, `.controller`, `.anim`, `.overrideController`, `ProjectSettings/*.asset`, ...) | Textually, yes. UnityYAMLMerge refuses them unless run with `--force` [tool]. | Resolve like `.unity`/`.prefab` if the driver was run with `--force`; otherwise treat as a line merge of YAML and validate by reimport. Keep `fileID` / `guid` pairs intact. |
| `.meta` | Text YAML. | Import-setting hunks: resolve normally. `guid:` hunk: never pick by formatting; pick the GUID that the rest of the project references (search for both), then reimport. See section 4. |
| Binary assets and anything in Git LFS (textures, audio, models, `LightingData.asset`, NavMesh / Terrain `.asset` if stored binary) | No. | There are no hunks. Choose `--ours` or `--theirs` for the whole file (and its `.meta` as a pair), or stop and ask which side wins. Never edit the pointer file. See section 5. |
| `Packages/packages-lock.json` | Do not hand-merge. | Take either side, then let the Package Manager regenerate it: "Don't manually modify the lock file" [doc]. `Packages/manifest.json` is ordinary JSON: resolve normally. |
| `.cs`, `.asmdef`, `.uss`, `.uxml`, `.shader` | Ordinary text. | Upstream rules apply. After resolving, a GUID-carrying `.asmdef` reference (`GUID:...`) must still match an existing `.asmdef.meta`. |

Sources for the table rows are in the sections below.

## 2. UnityYAMLMerge (Smart Merge)

### What it is

- UnityYAMLMerge "merges scene and prefab files in a semantically correct way", usable from the command line and third-party version control. [doc] https://docs.unity3d.com/6000.3/Documentation/Manual/SmartMerge.html
- Location: `C:\Program Files\Unity\Editor\Data\Tools\UnityYAMLMerge.exe` (Windows), `/Applications/Unity/Unity.app/Contents/Tools/UnityYAMLMerge` (macOS). With Unity Hub installs the path is `<Hub>/Editor/<version>/Editor/Data/Tools/` [tool]. [doc] https://docs.unity3d.com/6000.3/Documentation/Manual/SmartMerge.html
- Requires text serialization. Asset Serialization Mode "Force Text" is the default and the docs tie it to merges: "To help with version control merges, Unity can store scene files in a text-based format." [doc] https://docs.unity3d.com/6000.3/Documentation/Manual/class-EditorManager.html
- Unity staff blog (Adrian Woods, 2024-07-15): set Smart Merge as the default merge tool for YAML files; switching to binary serialization removes merge capability; "more elaborate changes can result in unresolvable conflicts". [doc] https://unity.com/blog/author-scenes-and-prefabs-with-verson-control

### Git setup, option A: mergetool (Unity's documented route)

Unity documents only the mergetool form (identical text in 2022.3, 6000.0, 6000.3):

```ini
[merge]
tool = unityyamlmerge

[mergetool "unityyamlmerge"]
trustExitCode = false
cmd = '<path to UnityYAMLMerge>' merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"
```

[doc] https://docs.unity3d.com/6000.3/Documentation/Manual/SmartMerge.html

This is **human-driven**: it only runs when someone calls `git mergetool`, and with `trustExitCode = false` "git mergetool will prompt the user to indicate the success of the resolution after the custom tool has exited". [doc] https://git-scm.com/docs/git-mergetool . With `-p` and the default fallback file, an unresolved conflict opens a GUI merge tool (section 3). An agent should not run this form.

### Git setup, option B: merge driver via `.gitattributes`

Git merge drivers are defined in `.git/config` or `~/.gitconfig`, "not in the gitattributes file", with placeholders `%O` (ancestor), `%A` (current), `%B` (other), `%L`, `%P`; the driver "must leave the result in the file named by %A", exits zero on a clean merge and non-zero on conflicts; above 128 is a failure rather than a conflict. `merge.<driver>.recursive` names the driver for internal merges of multiple merge bases. [doc] https://git-scm.com/docs/gitattributes

UnityYAMLMerge's own argument order is `merge [options] <base> <left> <right> [dest]`, where left is "theirs" and right is "mine" [tool, from `UnityYAMLMerge.exe` usage text]. That matches Unity's mergetool line (`$BASE $REMOTE $LOCAL $MERGED`). A driver therefore maps to:

```ini
# .git/config or ~/.gitconfig (per clone, not versioned)
[merge "unityyamlmerge"]
    name = Unity SmartMerge (UnityYAMLMerge)
    driver = '<path to UnityYAMLMerge>' merge -h -p --force --fallback none %O %B %A %A
    recursive = binary
```

```gitattributes
*.unity   merge=unityyamlmerge eol=lf
*.prefab  merge=unityyamlmerge eol=lf
*.asset   merge=unityyamlmerge eol=lf
```

This driver line is **derived** from the git-scm contract plus the tool's usage text and was exercised with the exe directly [tool]; Unity does not document a merge-driver form [unverified as an officially supported setup]. Notes per flag:

- `-h` headless, "no error dialogs" [tool].
- `--fallback none` disables the GUI fallback: "Can be set to 'none' to disable fallback" [tool]. Without it, the shipped `mergespecfile.txt` launches the first installed GUI tool (observed: the Plastic SCM `mergetool.exe` window opened and the process blocked until closed) [tool]. An agent-run merge that hits that path hangs.
- `--force` "Force merging even on unknown file extensions" [tool]. Without it, only `.unity` and `.prefab` are accepted; `.asset`, `.mat`, `.controller`, `.meta` and others exit 1 with "Don't know how to merge asset files" and leave `%A` untouched [tool]. Because any non-zero exit is a conflict to Git, a driver without `--force` turns **every** concurrent change to those types into a conflict, even non-overlapping ones.
- `recursive = binary` keeps the Unity tool out of virtual merge-base construction; this is a conservative choice, not something Unity documents [unverified].

Real example (a Unity project's `.gitattributes`, read only): it declares `[attr]unity-yaml merge=unityyamlmerge eol=lf` and applies it to `*.unity`, `*.prefab`, `*.asset`, `*.mat`, `*.meta`, `*.controller`, `*.anim` and about twenty more YAML extensions, while sending `*LightingData.asset`, `*-NavMesh.asset`, `*-Terrain.asset`, `*OcclusionCullingData.asset` and all images, audio, models and fonts to `lfs`. The driver definition itself is not in the repo; each clone must add it.

If `.gitattributes` names a driver that the local config does not define, Git falls back to its default merge: "if no merge driver is specified for the value of the merge attribute (as is the case by default with merge=lfs), then the default Git merge strategy is used". [doc] https://github.com/git-lfs/git-lfs/blob/main/docs/man/git-lfs-merge-driver.adoc . git-scm's own gitattributes page does not state this case. Consequence for an agent: the same repo behaves differently per machine; check `git config --get merge.unityyamlmerge.driver` before assuming Smart Merge ran.

### What the driver leaves behind on a conflict (critical)

Observed with 6000.3.20f1 on a scene where both sides renamed the same GameObject [tool]:

| Invocation | Exit | Content written to dest for the conflicted property |
|---|---|---|
| clean (different properties changed) | 0 | both changes merged |
| `-p` (premerge) | 2 | the **left / theirs** value |
| no `-p` | 2 | the **base** value (both edits lost) |
| `-r` | 2 | the **right / mine** value |
| unsupported extension, no `--force` | 1 | dest untouched (ours) |

In every conflict case the file contains **no conflict markers**. The conflicts are only reported on stdout, for example `Left  100.GameObject.m_Name change to Theirs` / `Right 100.GameObject.m_Name change to Renamed`, and optionally in a file via `-o <file>` (`--describe` adds what was done) [tool]. Git records the path as unmerged because the exit code is non-zero.

Agent consequences:

1. A marker search (`<<<<<<<`) on a Smart-Merged file finds nothing even though it is unresolved. Trust `git status` / `git diff --name-only --diff-filter=U`, not markers.
2. To see what conflicted, rebuild the three inputs from the index stages and rerun the tool with a report: `git show :1:<path> > base`, `:2:` (ours) `> mine`, `:3:` (theirs) `> theirs`, then `UnityYAMLMerge merge -h --force --fallback none -o conflicts.txt --describe base theirs mine out`. Stage numbers per [doc] https://git-scm.com/docs/git-checkout (`--ours`/`--theirs` check out "stage #2 (ours) or #3 (theirs)").
3. During `git rebase` "ours and theirs may appear swapped" [doc] https://git-scm.com/docs/git-checkout , so the "theirs wins by default" behaviour of `-p` means the rebased-onto side wins during a merge but your commit's side wins during a rebase. Check which side the file currently holds before declaring it resolved.

### Premerge and `mergespecfile.txt`

- Project Settings > Version Control > Smart merge: **Off** (default merge tool only), **Premerge** ("accepts clean merges. Unclean merges will create premerged versions of base, theirs and mine versions of the file" for the fallback tool), **Ask** (default, dialog on conflict). This setting governs merges done from inside the Editor's version control integration, not Git on the command line [unverified: inferred from the page's wording]. [doc] https://docs.unity3d.com/6000.3/Documentation/Manual/SmartMerge.html
- `-p` on the command line creates premerged left, base and right files "which contains only conflicts that this tool could not handle" and passes those to the fallback tool [tool, usage text].
- `mergespecfile.txt` ships next to the exe and "specifies how it should proceed with unresolved conflicts or unknown files". [doc] https://docs.unity3d.com/6000.3/Documentation/Manual/SmartMerge.html . Its contents [tool]: `unity use ...` and `prefab use ...` placeholder lines for scene and prefab fallbacks, then `* use ...` entries tried in order (Plastic SCM, Beyond Compare 3/4/5, Araxis, P4Merge, DiffMerge, Apple FileMerge), with `%l` local, `%r` remote, `%b` base, `%d` destination. "First tool found is used." Override per invocation with `--fallback <specfile>` or disable with `--fallback none`.

### What it cannot merge

- Anything not in text serialization, and any binary file. [doc] https://unity.com/blog/author-scenes-and-prefabs-with-verson-control
- File types other than `.unity` / `.prefab` unless `--force` is passed [tool]. The usage banner calls it a tool "for scene or prefab files" [tool].
- Paths listed under `[exclusions]` in the shipped `mergerules.txt`: "if both sides have modified it then it is to be treated as a conflict" [tool; the mechanism is also described at the doc URL above]. Shipped exclusions: `*.MeshRenderer.m_Materials.*`, `*.SpriteRenderer.m_Materials`, `*.SpriteRenderer.m_Color`, all of `*.ParticleSystem.*` except `InitialModule` itself, and `MonoBehaviour` fields whose children are `x y z` or `r g b` (vectors and colours) [tool].
- Arrays: `m_Component` and prefab `m_Modifications` are merged as keyed sets; `Renderer.m_Materials` as plain arrays; everything else uses heuristics [tool]. Reordered or concurrently edited lists outside the keyed ones are the likeliest bad merges [unverified].
- Structural edits: the tool needs a coherent hierarchy (a synthetic Transform without `m_Father` failed with "Could not determine the transform parent") [tool]. Unity staff: "More elaborate changes can result in unresolvable conflicts". [doc] https://unity.com/blog/author-scenes-and-prefabs-with-verson-control
- Floats that differ by rounding only are treated as equal only if configured under `[comparisons]` [doc] https://docs.unity3d.com/6000.3/Documentation/Manual/SmartMerge.html

### YAML facts an agent must not break when hand-resolving

- Each object starts `--- !u!<classID> &<fileID>`; the fileID is "unique within the file, although the number is assigned to each object arbitrarily". [doc] https://docs.unity3d.com/6000.3/Documentation/Manual/FormatDescription.html
- Hence: keeping both sides of a hunk that adds objects can create two documents with the same `&fileID`, or a `{fileID: N}` reference to a document that was dropped. The Editor reports the latter as `Broken text PPtr in file(%s). Local file identifier (%lld) doesn't exist!` and a bad cross-file reference as `Broken text PPtr. GUID %s fileID %lld is invalid!` [binary].

## 3. Validating a merged asset

Unity itself detects some bad merges on import. Strings present in the 6000.3 Editor [binary]:

- `The file '%s' seems to have merge conflicts. Please open it in a text editor and fix the merge.` (leftover markers)
- `Unable to parse file %s: [%s]`, `Parser Failure at line %u: %s` (broken YAML)
- `Broken text PPtr ...` (dangling fileID or GUID reference, above)
- `The referenced script (%s) on this Behaviour is missing!`, `Missing Prefab with guid: {0}`, `Missing Prefab Variant parent: '{0}'`

Recommended validation sequence (each step sourced; the sequence itself is a recommendation):

1. No path left unmerged and no markers in any text file (`git diff --check` and a marker grep; Smart-Merged files need the stage check from section 2).
2. Open or import the project so Unity reimports the changed files, ideally headless: `-batchmode` ("runs command line arguments without the need for human interaction"), `-projectPath`, `-logFile`, `-quit` ("This can hide some error messages, but they still appear in the Editor's log file"). Unity exits with code 1 when a method run via `-executeMethod` throws, or when `EditorApplication.Exit` is called non-zero. [doc] https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html
3. Grep the log for the [binary] strings above.
4. From an `-executeMethod` validator, load each merged scene / prefab and check:
   - `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go)` "Gets the number of MonoBehaviours with a missing script for the given GameObject". [doc] https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObjectUtility.GetMonoBehavioursWithMissingScriptCount.html
   - `PrefabUtility.IsPrefabAssetMissing(obj)` for prefab instances whose source asset is gone. [doc] https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PrefabUtility.IsPrefabAssetMissing.html
   - Broken object fields: iterate `SerializedProperty` of type ObjectReference and flag ones where `objectReferenceValue == null` but `objectReferenceInstanceIDValue != 0` [unverified: common community technique, not documented as a "missing reference" test].
5. Optional: `AssetDatabase.ForceReserializeAssets(paths, options)` rewrites assets in the current serialization format, making the merged file canonical; call it from a user action or batch method, not a callback. [doc] https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.ForceReserializeAssets.html . This produces its own diff; commit it separately.

An agent without an Editor available can only do step 1 plus YAML-level checks (duplicate `&fileID` anchors in one file, `{fileID: N}` with no matching anchor, `guid:` values with no `.meta` in the repo). It must report that the asset is unvalidated rather than calling the conflict resolved.

## 4. `.meta` conflicts and GUID collisions

- Each `.meta` holds "the unique ID assigned to the asset, and values for all the asset's import settings". If an asset loses its `.meta`, "any reference to that asset is broken"; materials lose textures and GameObjects get an unassigned script. Moving or renaming outside Unity requires moving the `.meta` too. [doc] https://docs.unity3d.com/6000.3/Documentation/Manual/AssetMetadata.html
- Unity staff: ".meta files should always be checked into version control"; they hold the "file GUID that connects all references between assets". [doc] https://unity.com/blog/author-scenes-and-prefabs-with-verson-control

Conflict shapes and rules:

1. **Import-setting hunks** (compression, max size, `userData`, etc.): ordinary YAML field merge, then reimport. Low risk.
2. **`guid:` hunk, same asset path added on both branches** (add/add): each side generated its own GUID, and assets on each side reference their own. Picking either side breaks the other side's references. Rule: search the tree for both GUIDs, keep the one with more (or more important) referrers, and re-point the others, or stop and ask. Never keep both lines. [unverified: reasoning from the AssetMetadata page, no Unity page describes this merge case]
3. **`.meta` and asset resolved to different sides** (for example a binary texture taken `--theirs` while its `.meta` kept `--ours`): the GUID and import settings no longer match the content the other branch expected. Rule: resolve an asset and its `.meta` as a pair. [unverified: inference]
4. **Spurious rename conflicts**: `.meta` files are small and near-identical, so Git rename detection can pair an unrelated deleted `.meta` with an added one and report a rename conflict. `merge.renames` controls rename detection during merges ("If set to false, rename detection is disabled"). [doc] https://git-scm.com/docs/git-merge . That this commonly bites Unity `.meta` files is a community report only [unverified] https://discussions.unity.com/t/git-conflict-markers-in-meta-files-usually-guid-conflicts-sourcetree/1496722

**GUID collision** (two different assets carrying the same GUID, typically from copying a file with its `.meta` outside Unity, or duplicate package content): the Editor detects it on import. Strings in the 6000.3 Editor [binary]: `GUID [{0}] for asset '{1}' conflicts with:` ... `Assigning a new guid.`, and for packages and other read-only locations `We can't assign a new GUID because the asset is in an immutable folder. The asset will be ignored.` So Unity re-GUIDs one of the two, and every reference that meant the re-GUIDed asset now points at the other one. Which of the two keeps the GUID is not documented [unverified]. Rule for an agent: after a merge, check that no two `.meta` files share a `guid:` value; if they do, decide which asset owns it before opening Unity, give the other a fresh GUID, and fix its referrers deliberately.

## 5. Binary and Git LFS files

- The `binary` built-in driver and the `-merge` attribute "take the version from the current branch as the tentative merge result, and declare that the merge has conflicts", for "binary files that do not have a well-defined merge semantics". `binary` is the macro `-diff -merge -text`. [doc] https://git-scm.com/docs/gitattributes
- `git lfs track` writes `filter=lfs diff=lfs merge=lfs -text`, but Git LFS "does not enable this merge driver by default", so `merge=lfs` falls back to Git's default merge, which merges the pointer files, "which usually is not useful". A text-only opt-in exists (`merge=lfs-text` with `git lfs merge-driver`, since Git LFS 3.2.0). [doc] https://github.com/git-lfs/git-lfs/blob/main/docs/man/git-lfs-merge-driver.adoc , https://github.com/git-lfs/git-lfs/blob/main/CHANGELOG.md
- To pick a side: `git checkout --ours|--theirs -- <path>` then `git add`. [doc] https://git-scm.com/docs/git-checkout . To inspect both LFS sides first: `git lfs checkout --to <tmp> --ours|--theirs|--base <path>` (conflict options since Git LFS 2.6.0). [doc] https://github.com/git-lfs/git-lfs/blob/main/docs/man/git-lfs-checkout.adoc
- Agent rule: there is nothing to "resolve hunk by hunk". Choosing a side discards the other author's work, so an agent should not choose silently. If the task gives no rule (for example "their branch owns the art"), stop and ask, listing each binary path with both commits' authors and messages.

## 6. Locking (prevent, not resolve)

- Lockable patterns: `git lfs track "*.psd" --lockable` writes `... lockable`, or `*.ext lockable` alone. Lockable files are made read-only locally until locked. Commands: `git lfs lock <path>`, `git lfs locks`, `git lfs unlock <path>`, `git lfs unlock --force <path>` (may need admin). Locking exists because concurrent edits "will lead to merge conflicts, which are very difficult to resolve in large binary files". [doc] https://github.com/git-lfs/git-lfs/wiki/File-Locking
- Push-time enforcement is via `lfs.<url>.locksverify`: when true, Git LFS halts a push "if the user attempts to update a file locked by another user"; unset means try and warn; a `501` from the server sets it false. `lfs.setlockablereadonly` controls the read-only behaviour. [doc] https://github.com/git-lfs/git-lfs/blob/main/docs/man/git-lfs-config.adoc
- Unity staff: locking suits teams with "zero tolerance for potential lost work"; in Git it requires Git LFS. They also recommend reducing conflicts structurally: additive scene loading to split big scenes, and nested prefabs edited in Prefab Mode. [doc] https://unity.com/blog/author-scenes-and-prefabs-with-verson-control
- Agent rule: locks are team policy. An agent must not `--force` unlock someone else's file. If a conflict hits a lockable path, that means the lock workflow was bypassed; report it rather than resolving quietly.

## 7. Gaps and open questions

- Unity does not document the Git merge-driver form, the exit codes (0 clean, 1 error or unsupported, 2 conflicts) or what the dest file contains on conflict; all of that is observed behaviour of 6000.3.20f1 and 6000.6.2f1 [tool] (section 8).
- Whether `--force` is semantically safe for every YAML asset type is untested beyond a property-level merge of an `AnimatorController` and an `AnimationClip`, which loaded cleanly (section 8) [tool].
- The GUID reassignment on collision was observed on 6000.6.2f1 (section 8), but is undocumented and may change [tool].
- No primary source found for a Unity-provided "validate project after merge" command; the validator in section 3 has to be written by the project.

## 8. Sandbox checks (6000.6.2f1)

The four checks the `MERGING.md` claims rest on, run for [#40](https://github.com/Hissal/mattpocock-skills-unity/issues/40) on 2026-09-25 with Unity 6000.6.2f1 (its bundled `UnityYAMLMerge.exe`), Git for Windows 2.55.0, in `unity-sandbox/` and a scratch Git repo. All [tool].

1. **The manual stage rerun works.** A copy of the URP blank `SampleScene.unity`, both sides renaming the same GameObject, merged by Git with no driver (markers present). `git show :1:/:2:/:3:` into base, ours, theirs, then `merge -h --force --fallback none -o report.txt --describe base theirs ours out`: exit 2, no markers in `out`, the conflicted `m_Name` left at the base value, and the report lists `Left <fileID>.GameObject.m_Name change to <theirs>` / `Right ... change to <ours>`. With `-p`: exit 2, `out` holds theirs. Different properties changed on each side: exit 0, both merged. A `.controller` without `--force`: exit 1, `Don't know how to merge controller files`, dest untouched. Same results as 6000.3.20f1 (section 2).
   - With a local driver (`merge -h -p --force --fallback none %O %B %A %A`) and `merge=unityyamlmerge` on `*.unity`: Git reports `CONFLICT (content)` and `UU`, the file has no markers and holds theirs for the conflicted property.
   - During `git rebase` of the same branches, stage 2 holds the upstream (the rename the rebase is onto), and the working file holds the replayed commit's value: the sides swap.
   - **Driver defined, exe missing** (the global driver pointed at an editor version no longer installed): Git marks every file carrying the attribute unmerged, including one whose sides changed different objects, and each holds our side untouched, with no markers. So "no markers" alone does not mean Smart Merge ran; check that the driver's exe exists.
   - Every argument error opens a modal `UnityYAMLMerge Error` dialog, even with `-h`, when `-h` comes before `merge` or `merge` is missing (`need 'merge' command specified`, `Missing command (e.g. 'merge') before option -h`). The call blocks until a human closes it.
2. **A `--force` merge of `.controller` and `.anim` loads.** An `AnimatorController` (states Idle and Run, a float parameter) and an `AnimationClip` made through the API. Ours: Idle speed 2, clip sample rate 60. Theirs: Run speed 3, parameter renamed, clip wrap mode Loop. Both merges exit 0; after a headless import the controller loads with Idle 2, Run 3 and the renamed parameter, and the clip with frame rate 60, wrap Loop, its bindings intact. No import error beyond the file-name warning from the copy. Both sides changing Idle's speed: exit 2, `AnimatorState.m_Speed` in the report, dest at base.
3. **The import log strings.** Headless import of three broken files:
   - leftover markers in an `.anim`: `The file 'Assets/.../Markers.anim' seems to have merge conflicts. Please open it in a text editor and fix the merge.`
   - a `.controller` whose `m_State: {fileID: N}` points at a missing anchor: `Broken text PPtr in file(Assets/.../Dangling.controller). Local file identifier (999999) doesn't exist!`
   - broken YAML in an `.anim`: `Unable to parse file Assets/.../Broken.anim: [Parser Failure at line 80: Expected closing '}']`
4. **Which asset keeps its GUID on a collision.** An imported clip (`ZZZ_Old.anim`) plus a new one (`AAA_New.anim`, sorting first) with a copy of its `.meta`: the log says `GUID [...] for asset 'AAA_New.anim' conflicts with: 'ZZZ_Old.anim' (current owner)` then `Assigning a new guid.`, and `AAA_New.anim.meta` is rewritten on disk with a new GUID. The asset already in the `Library/` keeps the GUID, whatever the name order. Two new clips sharing a fresh GUID: each is reported against the other and **both** get new GUIDs, so neither keeps it.
