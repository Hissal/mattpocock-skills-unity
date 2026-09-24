---
"mattpocock-skills-unity": minor
---

Add the `unity-serialization` skill: `.meta` GUID identity, rename rules (`FormerlySerializedAs`, auto-property backing fields, `MovedFrom`, append-only enums), the expand-then-contract reserialize follow-up with a shipped Editor scope script (and a grep-only fallback) that lists every file serializing a changed type and re-records scene prefab overrides that `ForceReserializeAssets` leaves on the old name, the editing order ending in surgical UnityYAML text edits, the validation checklist, and guidance for reviewing big asset diffs, slicing tickets and hotspot scans. The `unity` router gains its rows and `ask-matt` its line.
