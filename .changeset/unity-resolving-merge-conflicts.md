---
"mattpocock-skills-unity": minor
---

`resolving-merge-conflicts` gains its Unity delta: in a Unity repo it calls the `unity` router for resolving conflicts in Unity assets and checking the merged result works. `unity-serialization` gains `MERGING.md`: pauses instead of aborts, batched into one ask; unmerged files found by git state, with a check on whether the UnityYAMLMerge driver really ran; per-file-type rules; Smart Merge run by hand on the index stages; text checks and the import log strings before the validation checklist. The router gains a row for resolving a conflict in a Unity asset, and the skill's docs page gains one Unity line.
