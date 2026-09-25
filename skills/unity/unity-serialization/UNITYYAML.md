# UnityYAML

Scene (`.unity`), prefab (`.prefab`), ScriptableObject (`.asset`) and most other Unity asset files are text when Asset Serialization Mode is **Force Text** (Project Settings > Editor), the default. Unity documents them as tool-readable, and hand edits as unsupported: read and grep them freely, and edit them only surgically (see the editing order in `SKILL.md`).

## Not standard YAML

UnityYAML is a subset with its own tags: no comments, no complex mapping keys, no `|` or `+` block scalars, and `stripped` markers after document headers. A general YAML library that loads and re-dumps a file loses or rewrites these, and corrupts the file. Edit the text lines in place, keeping the file's line endings.

## Documents

```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: bbd5c944f11dd854fabf8c061de5ffc5, type: 3}
  m_Name: Settings
  m_EditorClassIdentifier: Assembly-CSharp::Game.Settings
  speed: 5
```

- Each object is one document, `--- !u!<classID> &<fileID>`. The fileID is unique within the file and arbitrary.
- Common class IDs: `1` GameObject, `4` Transform, `114` MonoBehaviour (a component script or a ScriptableObject), `1001` PrefabInstance. A ScriptableObject `.asset` holds its main object at `&11400000`.
- Serialized fields follow as keys under their serialized names (a backing field as `<Name>k__BackingField`).
- `m_EditorClassIdentifier` records the assembly and class name for the Editor; the script link is `m_Script`.
- A `[SerializeReference]` field holds `rid: <id>`, resolved in the host's `references:` block, where each entry carries `type: {class: ..., ns: ..., asm: ...}` and its `data`.
- Floats can be written as IEEE 754 hex (`0x...`); to edit one by hand, replace it with a decimal.

## References

- `{fileID: <id>, guid: <32 hex>, type: <n>}` points into another file: the GUID from that file's `.meta`, the fileID of the object inside it.
- `{fileID: <id>}` without a GUID points inside the same file. `{fileID: 0}` is null.
- `type: 2` loads directly from `Assets/` (materials, `.asset` files); `type: 3` loads through the import pipeline (prefabs, textures, models, scripts).
- Well-known fileIDs: `11500000` is a `.cs` script (in `m_Script`); `100100000` is a prefab's asset handle (in `m_SourcePrefab`). A script inside a DLL has the DLL's GUID and its own fileID.
- The Editor blocks references across scenes by default (`EditorSceneManager.preventCrossSceneReferences`). The format implies a prefab or ScriptableObject cannot hold a reference to a scene object either, though no single doc sentence says so.

## `.meta` files

A `.meta` holds `guid:` (the asset's identity) and its importer settings. Change import settings through the `AssetImporter` APIs; hand edits to them are not documented as supported. The `guid:` line changes only when adopting a known GUID is the point. A new file needs no `.meta`: Unity generates one on import. Binary assets (textures, audio, models) cannot be merged, but their `.meta` is text. Check `.gitattributes` for `filter=lfs` before trusting a binary file's bytes: without Git LFS installed, Git checks out pointer files silently, and Unity imports the pointers.

## Surgical edit recipes

Each ends with the validation checklist in `SKILL.md`.

- **GUID swap**, repairing references to an asset replaced by one with a new GUID: list the files first, then replace in place. Other `.meta` files can hold references too (a script's default references, a model's remapped materials), so they stay in the search; only a `.meta` whose own `guid:` line is the old GUID stays out.
  ```bash
  grep -rlF 'guid: <old>' Assets Packages | grep -vxF '<old asset path>.meta'
  grep -rlF 'guid: <old>' Assets Packages | grep -vxF '<old asset path>.meta' | xargs sed -i 's/guid: <old>/guid: <new>/g'
  ```
  Also check the fileID: a script moved into a DLL, or a sub-asset, changes it too.
- **`m_Script` repair**: the same swap on the `m_Script:` lines, with the script's `.meta` GUID and `fileID: 11500000`.
- **Renamed override**, without an Editor: in each file the scope lists, rewrite `propertyPath: <old>` (or `<parent>.<old>`) to the new name on the `m_Modifications` entries whose `target` points at the changed component.
- **A single value**: change the one `key: value` line; for an instance, the `value:` of its `m_Modifications` entry.
