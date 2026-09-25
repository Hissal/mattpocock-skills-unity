# Merging Unity assets

Resolving a merge or rebase conflict in a Unity repo. The generic loop (primary sources, both intents, finish the merge) still runs; this file adds what Unity changes about it.

## Pause, never abort

A merge is always carried to its commit. A few conflicts **pause** it instead: leave the merge in progress, ask the user, then resolve with the answer. A pause keeps every resolution made so far; it is not an abort.

Stop on these, unless the task or the merge's goal gives a rule (an upstream sync where their side owns the art; a `.meta` GUID only one side's files reference, decidable by grep):

1. A binary or LFS file changed on both sides.
2. A structural scene or prefab conflict: reparenting (`m_Father`, `m_Children`), components added or removed (`m_Component`), or `m_Modifications` entries.
3. A `.meta` `guid:` add/add: the same path added on both sides, each with its own GUID.
4. A conflict on a lockable LFS path (`lockable` in `.gitattributes`). Report it even when a rule resolved it: the lock workflow was bypassed. Leave locks to their owners.
5. A GUID collision: two different assets whose `.meta` files share a `guid:`.

Decide alone on `Packages/packages-lock.json`, `.meta` import-setting hunks, property-level Smart Merge conflicts and ordinary text.

Stops are **batched**: resolve everything else first, then one ask listing each stopped path with what each side did and both sides' commits and authors (`git log --merge --format='%h %an %s' -- <path>`).

## Find what is unmerged

- **Git state, not markers.** `git diff --name-only --diff-filter=U` lists every unmerged path. A Smart-Merged file carries no conflict markers, so a marker grep misses it.
- **Did the driver run?** `git check-attr merge -- <path>` names the file's merge driver, and `git config --get merge.<driver>.driver` its command. Detect it; leave git config as it is, and name a missing or broken driver in the final report.
  - **Command defined and its exe exists**: Smart Merge ran. The file holds its partial merge, with no markers (with `-p`, their side of each conflicted property).
  - **Command defined, exe missing** (a stale editor path): Git marks **every** file with that attribute unmerged, even ones with no overlapping change, and each holds our side untouched with no markers (observed on Git 2.55). Nothing was merged: run Smart Merge by hand on each.
  - **No command**: Git line-merged the file, and it has markers.
  - **A `.meta` sent to the driver**: UnityYAMLMerge cannot parse `.meta` files (`File is not a valid text serialized YAML file`) and exits 1, so every `.meta` both sides changed is unmerged, holding our side with no markers, even when the changes do not overlap. Line-merge it by hand from the index stages (as in *Smart Merge by hand*): `git merge-file ours base theirs` writes the merge into `ours`, markers included, and exits with the conflict count.
  - **The command's arguments**: UnityYAMLMerge takes base, theirs, ours, dest, and Git reads only `%A`, so the command ends `%O %B %A %A`. Without a dest the tool prints the merge to stdout and exits 0, and Git records our side as cleanly merged: their changes to that file are gone with no conflict shown. Name it in the final report.
- **Rebase swaps the sides.** During a rebase, stage 2 (`--ours`) is the branch being rebased onto and stage 3 (`--theirs`) is your commit being replayed. Read which side the file holds before calling it resolved.

## By file type

| File type | Rule |
|---|---|
| `.unity`, `.prefab` | Smart Merge. Its report's property conflicts: resolve property by property, by each side's intent. Structural conflicts: stop. |
| Other Unity YAML: `.asset`, `.mat`, `.controller`, `.anim`, `ProjectSettings/*.asset` | Smart Merge with `--force` (a merged `.controller` and `.anim` imported and loaded cleanly on 6000.6.2f1), or a line merge. Keep each `{fileID, guid}` pair whole. |
| `.meta` | Line merge, never Smart Merge (it cannot parse `.meta`). Import-setting hunks: resolve as YAML. A `guid:` hunk: grep the tree for both GUIDs; keep the one only one side references, or stop when both are referenced. Resolve an asset and its `.meta` to the same side. |
| Binary, or LFS (`git check-attr filter -- <path>` says `lfs`) | No hunks, only a side: `git checkout --ours\|--theirs -- <asset> <asset>.meta`, then `git add`. Changed on both sides with no rule: stop. The LFS pointer text is never edited. |
| `Packages/packages-lock.json` | Take either side; the Package Manager regenerates it on the next Editor open. |
| `.cs`, `.asmdef`, `.asmref`, `Packages/manifest.json`, `.uss`, `.uxml`, `.shader` | Ordinary text. |

## Smart Merge by hand

For an unmerged Unity YAML file the driver did not merge, or to list what it left conflicted. The exe ships with the editor: `<editor>/Editor/Data/Tools/UnityYAMLMerge.exe` on Windows, `Unity.app/Contents/Tools/UnityYAMLMerge` on macOS; use the project's editor version (`ProjectSettings/ProjectVersion.txt`).

```bash
git show :1:<path> > base; git show :2:<path> > ours; git show :3:<path> > theirs
'<UnityYAMLMerge>' merge -h --force --fallback none -o report.txt --describe base theirs ours merged
```

- Every call starts `merge -h`, in that order: with `-h` missing, or placed before `merge`, an argument error opens a modal dialog and the call blocks until a human closes it. `--fallback none` keeps a conflict from launching a GUI merge tool.
- Argument order is base, **theirs**, **ours**, dest. During a rebase, swap stages 2 and 3.
- **Exit 0**: clean; `merged` holds both sides. Copy it over the path and `git add`.
- **Exit 2**: conflicts. `report.txt` lists each one as `Left <fileID>.<Class>.<property> change to <theirs>` and `Right ... change to <ours>`. `merged` holds everything else merged and the **base** value for each conflicted property, with no markers. Set each conflicted property with a surgical edit ([UNITYYAML.md](UNITYYAML.md)), then copy it over the path and `git add`. A conflicted property on the structural list above is a stop.
- **Exit 1**: refused or failed (without `--force` it refuses anything but `.unity` and `.prefab`); `merged` is untouched. A failure such as `Could not determine the transform parent` means a structural conflict: stop.

(Observed with the UnityYAMLMerge of 6000.3.20f1 and 6000.6.2f1; Unity does not document the exit codes or the dest contents.)

## Check the merged result

Text checks, always, before the merge commit:

- `git diff --name-only --diff-filter=U` prints nothing.
- No markers in the resolved files: `git grep -nE '^(<<<<<<<|>>>>>>>)( |$)|^=======$' -- <paths>`.
- No `&fileID` anchor twice in one file, and no `{fileID: N}` (no GUID, N not 0) whose `&N` anchor is missing from that file:
  ```bash
  grep -o '^--- !u![0-9]* &-\?[0-9]*' <file> | sed 's/.*&//' | sort | uniq -d
  comm -23 <(grep -o '{fileID: -\?[0-9]*}' <file> | grep -o -- '-\?[0-9]*' | grep -vx 0 | sort -u) <(grep -o '^--- !u![0-9]* &-\?[0-9]*' <file> | sed 's/.*&//' | sort -u)
  ```
- No two `.meta` files share a `guid:`: `grep -rh --include='*.meta' '^guid:' Assets Packages | sort | uniq -d`. Settle any collision before Unity opens the project: Unity keeps the GUID on the asset it already imported and gives the other a new one, rewriting its `.meta`; when both are new to its `Library/`, both get new GUIDs (observed on 6000.6.2f1). Either way, references meant for the re-GUIDed asset now point elsewhere.
- Only when an `.asmdef` was conflicted: run `unity-assemblies`' [scripts/asmdefs.sh](../unity-assemblies/scripts/asmdefs.sh) on the project; no reference prints unresolved (`?GUID:...`).

Then, when Unity can run (call the Skill tool with "unity-verification" for where and how), import the merged files and grep the Editor log for these strings, each seen on 6000.6.2f1:

- `seems to have merge conflicts`: a leftover marker.
- `Broken text PPtr`: a dangling `fileID` or GUID reference.
- `Unable to parse file`: broken YAML.
- `conflicts with:` then `Assigning a new guid.`: a GUID collision Unity just resolved on its own.

Finish with the validation checklist in `SKILL.md`. When Unity cannot run, commit the merge anyway and carry the Unity checks into the final report as **unvalidated**.
