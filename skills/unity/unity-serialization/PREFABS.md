# Prefabs

## Overrides

- An override on a prefab instance (a property value, an added or removed component, an added or removed child) always wins over the prefab asset: changing the asset has no effect on an instance that overrides that property.
- Collection overrides are **by index**. An overridden array, list or dictionary entry stays at its position, so inserting or removing entries in the asset can move the override onto the wrong element.
- Default overrides are always present and not real overrides: the root's `m_Name`, the root Transform's `m_LocalPosition`, `m_LocalRotation` (with `m_LocalEulerAnglesHint`, `m_RootOrder`), and on a RectTransform root also `m_AnchoredPosition`, `m_SizeDelta`, `m_AnchorMin`, `m_AnchorMax` and `m_Pivot`. Apply All and Revert All skip them.

## Nesting and variants

- A nested prefab instance keeps its link to its own prefab asset while being part of the outer one.
- A variant inherits from a base prefab (which can itself be a variant); its edits are overrides on the base. A variant made from an instance carries that instance's overrides.
- Apply All applies to the outermost prefab. Applying to an inner prefab or a variant's base goes through the per-property menu or the `PrefabUtility.Apply*` APIs.

## Prefab YAML

- An instance, in a scene or inside another prefab, is a `PrefabInstance` document (`--- !u!1001`) with `m_SourcePrefab: {fileID: 100100000, guid: <prefab GUID>, type: 3}` and an `m_Modification` block: `m_TransformParent`, `m_Modifications`, `m_RemovedComponents`, `m_RemovedGameObjects`, `m_AddedGameObjects`, `m_AddedComponents`.
- Each `m_Modifications` entry is `target` (the object in the source prefab), `propertyPath` (a `SerializedProperty` path such as `m_LocalPosition.x` or `a.inner.value`), `value` (a string) and `objectReference`.
- The instance's own objects are not written out. Where the file must point at one (a child Transform used as a parent), Unity writes a `stripped` placeholder (`--- !u!4 &<id> stripped`) holding only `m_CorrespondingSourceObject`, `m_PrefabInstance` and `m_PrefabAsset`.
- A variant file is one `PrefabInstance` with `m_TransformParent: {fileID: 0}` whose `m_SourcePrefab` is the base.
- So a value seen on an instance may appear nowhere in the scene file (it comes from the asset), and editing the asset's YAML leaves every instance that overrides that property unchanged.

## Renamed fields and overrides

`FormerlySerializedAs` covers overrides: an override whose `propertyPath` still names the old field applies to the renamed field on load, in scenes, variants and nested prefabs alike. On disk the old path stays until the file is rewritten:

- `ForceReserializeAssets` on a prefab asset (a variant, or a prefab with a nested instance) writes an entry under the new `propertyPath` and keeps the old entry beside it.
- `ForceReserializeAssets` on a scene rewrites the scene's own objects but leaves its prefab instance overrides on the old `propertyPath`. So does opening and saving the scene. `PrefabUtility.RecordPrefabInstancePropertyModifications` on the instance's component, then a save, writes the new path.
- Once the attribute is removed, an override that only exists under the old path is lost: the instance falls back to the prefab asset's value.

`SerializationScope.Reserialize` handles both; a surgical text edit of the `propertyPath` lines is the fallback without an Editor.
