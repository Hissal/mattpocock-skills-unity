# Research: Unity serialization mechanics

Ticket: [#2](https://github.com/Hissal/mattpocock-skills-unity/issues/2), part of [#1](https://github.com/Hissal/mattpocock-skills-unity/issues/1). Feeds the `unity-serialization` skill.

Sources are primary only: the Unity manual and scripting API on docs.unity3d.com, Unity's [UnityCsReference](https://github.com/Unity-Technologies/UnityCsReference) source, official Unity package docs, and posts on unity.com by Unity staff. Unless a claim says otherwise, it was checked against the Unity 6.0 (6000.0) docs. Claims that only hold from a certain version say so. Anything marked **Unverified** could not be traced to a primary source and should not be stated as fact in the skill without a hedge.

## Summary: the rules an agent most needs

1. **An asset's identity is the GUID in its `.meta` file, not its path.** References between files are stored as `{fileID, guid, type}`. Moving or renaming an asset is safe only if its `.meta` moves with it. Losing or regenerating a `.meta` gives the asset a new GUID and breaks every reference to it, silently. ([AssetMetadata](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetMetadata.html), [assets-direct-reference](https://docs.unity3d.com/6000.3/Documentation/Manual/assets-direct-reference.html))
2. **Always move, rename and delete an asset together with its `.meta`** (and folders with their folder `.meta`). Prefer doing it in the Editor or through `AssetDatabase` APIs. Always commit `.meta` files. ([AssetMetadata](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetMetadata.html), [Unity blog, 2024](https://unity.com/blog/author-scenes-and-prefabs-with-verson-control))
3. **Scripts are assets too.** A component on a GameObject stores `m_Script: {fileID: 11500000, guid: <script .meta guid>, type: 3}`. Renaming or moving a `.cs` file without its `.meta`, or deleting and recreating it, produces "missing script" components everywhere it was used. ([TryGetGUIDAndLocalFileIdentifier, 2018.1](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/AssetDatabase.TryGetGUIDAndLocalFileIdentifier.html))
4. **Serialized data is keyed by field name.** Renaming a serialized field loses its data unless you add `[FormerlySerializedAs("oldName")]`. Renaming, moving namespace, or moving assembly for a `[SerializeReference]` class loses (nulls) those objects unless you add `[MovedFrom]`. ([FormerlySerializedAs](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Serialization.FormerlySerializedAsAttribute.html), [ManagedReferenceMissingType](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ManagedReferenceMissingType.html), [MovedFromAttribute.cs](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/Scripting/Attributes/MovedFromAttribute.cs))
5. **Enums serialize as their integer value.** Reordering, inserting or deleting members changes what saved data means. Give serialized enums explicit values and only append. ([unity.com how-to](https://unity.com/how-to/scriptableobject-based-enums))
6. **Only some fields serialize:** `public` or `[SerializeField]`, not `static`/`const`/`readonly`, of a supported type. Properties, dictionaries (before Unity 6.6), multidimensional/jagged arrays and nested containers do not. Custom classes serialize inline (by value, no null, no polymorphism) unless `[SerializeReference]`. ([serialization rules](https://docs.unity3d.com/6000.0/Documentation/Manual/script-serialization-rules.html))
7. **Prefab overrides win.** An overridden property on an instance ignores later changes to the prefab asset. Collection overrides are by index and can land on the wrong element if the asset's list changes. ([PrefabInstanceOverrides](https://docs.unity3d.com/Manual/PrefabInstanceOverrides.html))
8. **Scene, prefab and `.asset` files are UnityYAML, not general YAML.** Unity says they cannot be externally produced or edited, and hand edits are "not supported". Treat raw YAML as read-mostly: fine to read and grep, risky to edit; prefer Editor or `AssetDatabase`/`SerializedObject` APIs, and let Unity re-save. ([UnityYAML](https://docs.unity3d.com/6000.0/Documentation/Manual/UnityYAML.html), [Unity blog, 2022](https://unity.com/blog/engine-platform/understanding-unitys-serialization-language-yaml))
9. **Never edit or commit `Library/`.** It is a regenerable per-machine cache. ([default-directories](https://docs.unity3d.com/6000.5/Documentation/Manual/default-directories.html))
10. **Keep Asset Serialization Mode on Force Text** (the default) so scenes and prefabs can be diffed and merged. Use UnityYAMLMerge (Smart Merge) for `.unity`/`.prefab`, and Git LFS for large binary content. ([class-EditorManager](https://docs.unity3d.com/6000.0/Documentation/Manual/class-EditorManager.html), [SmartMerge](https://docs.unity3d.com/6000.0/Documentation/Manual/SmartMerge.html), [Unity blog, 2024](https://unity.com/blog/author-scenes-and-prefabs-with-verson-control))

## 1. `.meta` files, GUIDs and fileIDs

### What a `.meta` holds

- Unity creates a `.meta` file for every file and folder in `Assets`, stored next to the asset. It holds the asset's unique ID (GUID) and all its import settings. Changed import settings are written to the `.meta`. ([AssetMetadata, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetMetadata.html))
- The GUID "allows Unity to connect the original asset file with the artifact in the asset database". Imported artifacts live in `Library`, named by hash in two-character subfolders, and can always be regenerated from source files plus settings. ([asset-database-contents, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/asset-database-contents.html))

### How a reference is stored

- A serialized reference to an asset is a pointer made of a GUID ("a 128-bit (32-character hexadecimal) ID that uniquely identifies the asset file on disk") and a fileID ("a 64-bit ID that is unique to the target object within that specific asset file"). ([assets-direct-reference, 6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/assets-direct-reference.html))
- In text files this looks like `{fileID: 2100000, guid: 31321ba15b8f8eb4c954353edc038b1d, type: 2}`. References inside the same file omit the guid: `{fileID: 330585543}`. `{fileID: 0}` is null. ([YAMLSceneExample, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/YAMLSceneExample.html))
- `type` tells Unity where to load from: `2` for assets loaded directly from `Assets` (materials, `.asset` files), `3` for processed assets loaded from `Library` (prefabs, textures, models, scripts). ([Unity blog, Borromeo, 2022-07-28](https://unity.com/blog/engine-platform/understanding-unitys-serialization-language-yaml))
- The fileID is local to the file and can repeat across files; the GUID is global. ([Unity blog, 2022](https://unity.com/blog/engine-platform/understanding-unitys-serialization-language-yaml))
- Local IDs can exceed 32 bits (for example in prefabs); use the `long` overloads of `AssetDatabase.TryGetGUIDAndLocalFileIdentifier`. ([TryGetGUIDAndLocalFileIdentifier, 6000.0](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetDatabase.TryGetGUIDAndLocalFileIdentifier.html))
- Well-known fileIDs seen in practice: `11500000` for the script behind a MonoBehaviour's `m_Script` ([TryGetGUIDAndLocalFileIdentifier, 2018.1](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/AssetDatabase.TryGetGUIDAndLocalFileIdentifier.html)); `100100000` for a prefab's asset handle, which exists only in `Library`, never in the YAML ([yaml-prefab-serialization, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/yaml-prefab-serialization.html)). **Unverified** from primary docs: `11400000` as the main object of a ScriptableObject `.asset` and `2100000` for a material (the latter only appears as an example value in YAMLSceneExample).

### What breaks references

- Moving or renaming inside the Project window moves the `.meta` too. Moving or renaming outside Unity (file explorer, `git mv`, a script) requires moving the `.meta` yourself. ([AssetMetadata](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetMetadata.html))
- "If an asset loses its `.meta` file, any reference to that asset is broken in your project." Unity then generates a new `.meta` as if it were a brand-new asset (new GUID, default import settings) and deletes the old one, breaking, for example, material-to-texture links and script assignments. ([AssetMetadata](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetMetadata.html))
- Consequence for agents: creating a new asset file on disk without a `.meta` is fine (Unity generates one on import), but deleting and re-creating an existing file, or letting a tool regenerate `.meta` files, changes the GUID. Inference from the above, not a separate doc statement.
- Deleting an asset leaves its referrers holding the dead GUID. If the same asset with the same `.meta` GUID reappears (for example via `git checkout`), the references resolve again. **Unverified** as an explicit doc statement; it follows from references being GUID plus fileID.
- Moving a script file outside the Editor can leave the Editor not recognising the MonoBehaviour until the script is reimported (Unity forum thread, staff status not confirmed, so **unverified**: [discussion](https://discussions.unity.com/t/editor-cant-recognize-monobehaviour-if-script-file-is-moved-manually-via-explorer-finder-git/927952)).
- Replacing a script with a compiled DLL changes the script's GUID and fileID, breaking every `m_Script` reference; the documented fix is to look up the new IDs and find-and-replace the `m_Script` lines. ([TryGetGUIDAndLocalFileIdentifier, 2018.1](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/AssetDatabase.TryGetGUIDAndLocalFileIdentifier.html))
- **Unverified**: duplicate GUIDs (copying an asset and its `.meta` on disk) make Unity assign a new GUID to one of the copies on import. Not found in current docs.

### Scripts and class names

- File name and class name should match. For MonoBehaviour and ScriptableObject Unity can resolve some mismatches (it picks the class matching the file name when a file has several; for partial classes only the file matching the class name works as a component). Since 2020.1 a single file cannot contain MonoBehaviour or ScriptableObject definitions in multiple namespaces; this renders the MonoBehaviour unusable. ([naming-scripts, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/naming-scripts.html))
- The ScriptableObject manual is stricter: "The script file must have the same name as the class." ([class-ScriptableObject, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/class-ScriptableObject.html))

### Refresh and `Library`

- The Asset Database refreshes when the Editor regains focus (if Auto-Refresh is on), on Assets > Refresh, or on `AssetDatabase.Refresh`. It detects added, modified and deleted files in `Assets` and `Packages`, imports code, reloads the domain (unless refresh came from a script), then imports other assets. ([AssetDatabaseRefreshing, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetDatabaseRefreshing.html))
- `Library` is "a local cache of imported assets and metadata"; exclude it from version control because it is "unique to your computer". `Temp`, `Logs` and `UserSettings` are also local. `Assets`, `Packages` and `ProjectSettings` are the shared project. ([default-directories, 6000.5](https://docs.unity3d.com/6000.5/Documentation/Manual/default-directories.html))

## 2. What serializes

### Field rules (Unity 6.0)

A field serializes when it is `public` or has `[SerializeField]`, is not `static`, `const` or `readonly`, and has a serializable type. ([script-serialization-rules, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/script-serialization-rules.html))

Supported types: primitives (`int`, `float`, `double`, `bool`, `string`, ...), enums of 32 bits or smaller, fixed-size buffers, Unity built-ins (`Vector2`, `Vector3`, `Rect`, `Matrix4x4`, `Color`, `AnimationCurve`, ...), `[Serializable]` custom structs and classes, references to `UnityEngine.Object` subclasses, and arrays or `List<T>` of those. ([script-serialization-rules, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/script-serialization-rules.html))

Not supported (through 6.5): "multilevel types (multidimensional arrays, jagged arrays, dictionaries, and nested container types)". Wrap an inner list in a `[Serializable]` class to nest. ([script-serialization-rules, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/script-serialization-rules.html); still stated in [6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html))

Properties are not serialized. A property's backing field can be, for example `[field: SerializeField]` on an auto-property; the stored name is the compiler-generated backing field name. ([script-serialization-rules, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/script-serialization-rules.html)) Note: this means converting a plain field to an auto-property (or back) changes the serialized name and loses data unless handled. Inference as a doc statement, **observed** in the sandbox (section 10, check 4).

Generic field types (for example `MyClass<int>`) serialize directly since Unity 2020.1, without a concrete subclass. ([script-Serialization, 2020.1](https://docs.unity3d.com/2020.1/Documentation/Manual/script-Serialization.html))

### Inline (by value) serialization of custom classes

- `[Serializable]` custom classes serialize inline, by value. `[SerializeReference]` is required for null, shared references, polymorphism and cycles. ([script-serialization-rules, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/script-serialization-rules.html))
- Practical consequences: a null field of a serializable class comes back as a new default instance; a field of a base type holding a derived instance is saved as the base type (derived data lost); two fields pointing at one instance deserialize as two copies. These follow from "value-based serialization can't represent null" ([SerializeReference](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SerializeReference.html)) and from inline serialization.
- Indirect self-reference cycles in inline classes: Unity stops after 10 levels of recursion "and discard[s] the remaining data", with a warning. ([script-serialization-rules, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/script-serialization-rules.html))
- References to `UnityEngine.Object` (including ScriptableObjects) store only the reference, not the object graph, which is the recommended way to share data. ([serialization best practices, 6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-best-practices.html))

### Enums

- Serialized enum values are stored as integers, not names: "removing or reordering a value can lead to incorrect or unexpected behavior." ([unity.com how-to](https://unity.com/how-to/scriptableobject-based-enums))
- Enums larger than 32 bits do not serialize. ([script-serialization-rules, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/script-serialization-rules.html); analyzer rule UAC1011 in [6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/script-serialization-analyzer.html))

### Unity 6.6 changes: dictionaries and the analyzer

- Unity 6.6 serializes `Dictionary<TKey, TValue>` directly. Opt-in only: "Public dictionary fields aren't serialized automatically. Without `[SerializeField]`, Unity skips the field." The declared type must be exactly `Dictionary<TKey, TValue>`. Keys cannot implement `IEnumerable` (except `string`); keys and values cannot be interfaces or abstract types (other than `UnityEngine.Object` hierarchies); `[SerializeReference]` is not valid on dictionary fields; dictionaries cannot nest directly in other collections. Prefab instances support per-entry overrides. ([script-serialization-dictionaries, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/script-serialization-dictionaries.html), [WhatsNewUnity66](https://docs.unity3d.com/6000.6/Documentation/Manual/WhatsNewUnity66.html))
- Gotcha: in 6.5 and earlier a `[SerializeField] Dictionary` field silently does not serialize. Check the project's Unity version (`ProjectSettings/ProjectVersion.txt`) before relying on it.
- Unity 6.6 ships a compile-time serialization rules analyzer (UAC1000 to UAC1022) that flags, among others: `[SerializeReference]` on a struct, primitive, enum or dictionary; public fields skipped because the type lacks `[Serializable]`; self-reference cycles; unsupported collections; tuples; and "Field name conflicts with FormerlySerializedAs argument" (UAC1018). ([script-serialization-analyzer, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/script-serialization-analyzer.html)) Useful as a checklist even on older versions.

### Threading and callbacks

- Constructors and field initializers of serializable types run on a loading thread: do not call Unity APIs there. Do not trigger serialization from finalizers. ([best practices, 6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-best-practices.html))
- `ISerializationCallbackReceiver` (`OnBeforeSerialize`, `OnAfterDeserialize`) is the classic way to persist unsupported types (for example a Dictionary as two lists). The serializer runs off the main thread, so keep callbacks to the object's own fields. Host callbacks run before those of its managed references. ([ISerializationCallbackReceiver, 6000.0](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ISerializationCallbackReceiver.html))
- Best practice: never serialize duplicate or cached data; keep MonoBehaviour and ScriptableObject data flat. ([best practices, 6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-best-practices.html))

## 3. Renames: `FormerlySerializedAs` and `MovedFrom`

### `FormerlySerializedAs` (field renames)

- "Use this attribute to rename a field without losing its serialized value": `[FormerlySerializedAs("hitpoints")] public int health;`. ([FormerlySerializedAsAttribute, 6000.0](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Serialization.FormerlySerializedAsAttribute.html))
- Source: `[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]`, so a field can carry several old names (chained renames). ([FormerlySerializedAsAttribute.cs](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Serialization/FormerlySerializedAsAttribute.cs))
- Only removable once every scene and asset has been re-saved with the new name; leaving it in place is harmless. ([FormerlySerializedAsAttribute](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Serialization.FormerlySerializedAsAttribute.html))
- Unity upgrades old data in memory only; files keep the old name until re-saved. `AssetDatabase.ForceReserializeAssets` writes the upgrade to disk and is the documented step before removing `FormerlySerializedAs`. Call it only from a direct user action (menu item), never from callbacks like `OnEnable`. ([ForceReserializeAssets, 6000.0](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetDatabase.ForceReserializeAssets.html))
- Changing a field's type is not covered by this attribute. **Unverified** how Unity converts values across type changes; treat a type change as data loss unless tested.
- Whether prefab override `propertyPath` entries (which use the field name) are migrated by `FormerlySerializedAs`: **observed** on 6000.6.2f1 (section 10, checks 2 and 3). The attribute applies old-path overrides on load; on disk, a reserialized prefab gains the new path beside the old, while a reserialized scene keeps only the old path.

### `MovedFrom` (type renames, for `SerializeReference`)

- `[SerializeReference]` data records each object's "fully qualified class name". ([SerializeReference, 6000.0](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SerializeReference.html)) A type goes missing "if the class was renamed, moved to another assembly, or moved inside a different namespace." ([ManagedReferenceMissingType, 6000.0](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ManagedReferenceMissingType.html))
- `UnityEngine.Scripting.APIUpdating.MovedFromAttribute` tells the serializer the old identity. Constructor: `MovedFrom(bool autoUpdateAPI, string sourceNamespace = null, string sourceAssembly = null, string sourceClassName = null)`; a null argument means "unchanged". The source comment says serialization by reference "needs to be informed of classes being renamed", and that with `autoUpdateAPI == false` "any combination of changes to [assembly, namespace, class name] are supported, but *ONLY* by the serialization system". It also notes the APIUpdater does not support class renames through this attribute. ([MovedFromAttribute.cs](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/Scripting/Attributes/MovedFromAttribute.cs))
- `MovedFromAttribute` has no page in the 6000.0 scripting API (404 at [ScriptReference](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Scripting.APIUpdating.MovedFromAttribute.html)); the source is the authority.
- MonoBehaviour and ScriptableObject classes are not identified by class name in data but by their script's GUID (`m_Script`), so renaming such a class is safe as long as the `.cs` file keeps its `.meta` and the file name matches the new class name. Inference from sections 1 and 2.

## 4. `SerializeReference`

All from [SerializeReference, 6000.0](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SerializeReference.html) unless noted.

- Field type may be a regular class, abstract class, interface or `System.Object`; arrays and lists are supported. The assigned instance must be a `[Serializable]` custom class. It cannot be a `UnityEngine.Object` subclass, a value type (ints, structs), or a Dictionary.
- Supports null, polymorphism and shared references, but sharing is scoped to one host object: "The managed references are not shared between other UnityEngine.Object instances."
- Data lives in a `references` section after the regular fields, one entry per object with its ID, fully qualified class name and field values.
- Missing types: Unity "replaces the instance with null, but the serialized information is preserved." Tools: `SerializationUtility.HasManagedReferencesWithMissingTypes`, `GetManagedReferencesWithMissingTypes`, `ClearManagedReferenceWithMissingType`, `ClearAllManagedReferencesWithMissingTypes`. ([ManagedReferenceMissingType](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ManagedReferenceMissingType.html), [Unity blog, Barrette, 2021 LTS](https://unity.com/blog/engine-platform/serializereference-improvements-in-unity-2021-lts))
- Since 2021 LTS each managed reference keeps a stable ID across save and load, which keeps diffs small. ([Unity blog, 2021 LTS](https://unity.com/blog/engine-platform/serializereference-improvements-in-unity-2021-lts))
- Generic inflated types (for example `Foo<int>`) are supported since 2023.1. ([2023.1.0b1 release notes](https://unity.com/releases/editor/beta/2023.1.0b1))
- By-value serialization is cheaper in storage, memory and load/save time; use `SerializeReference` only when needed.
- **Unverified**: exactly when preserved missing-type data is lost. The docs say it is preserved, but not whether it survives the host being edited and saved in a version or tool that does not preserve it. Fix missing types (`MovedFrom`, restore the class) before re-saving hosts.

## 5. Prefabs: overrides, nesting, variants

### Overrides

- Overrides are: property values, added or removed components, added or removed child GameObjects. "An overridden property value on a prefab instance always takes precedence over the value from the prefab asset", so changing the asset has no effect on instances that override that property. ([PrefabInstanceOverrides, 6000.6](https://docs.unity3d.com/Manual/PrefabInstanceOverrides.html))
- Collection gotcha: when an array, list or dictionary entry is overridden, "the override stays at the same position" even if the prefab asset's collection changes, so inserting or removing entries in the asset can make overrides apply to the wrong element. ([PrefabInstanceOverrides, 6000.6](https://docs.unity3d.com/Manual/PrefabInstanceOverrides.html))
- Default overrides: the root GameObject's `name` and the root Transform's `localPosition`, `localRotation` (plus internal `localEulerAnglesHint`, `rootOrder`), and for RectTransform also `anchoredPosition`, `sizeDelta`, `anchorMin`, `anchorMax`, `pivot`. They are excluded from Apply All and Revert All, do not show in the Overrides dropdown, and only show as bold. ([PrefabUtility.IsDefaultOverride, 6000.0](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PrefabUtility.IsDefaultOverride.html))

### Nested prefabs and variants

- Nested prefab instances "keep their links to their own prefab asset assets, while also forming part of another prefab asset". Adding one to an instance from the Hierarchy shows as an added-GameObject override until applied. ([NestedPrefabs](https://docs.unity3d.com/Manual/NestedPrefabs.html))
- Apply All always applies to the outermost prefab. To apply to an inner (nested) prefab or a variant's base, use the per-property context menu or `PrefabUtility.Apply*` APIs. ([PrefabOverridesMultiLevel, 2022.3](https://docs.unity3d.com/2022.3/Documentation/Manual/PrefabOverridesMultiLevel.html))
- A variant inherits from a base prefab (which may itself be a variant). Edits in a variant are stored as overrides and take precedence over base values. Creating a variant from an instance carries that instance's overrides into the variant. ([PrefabVariants, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/PrefabVariants.html))
- Unity staff advise keeping prefab hierarchies under 5 to 7 levels deep for performance and fewer merge conflicts. ([Unity blog, Woods, 2024-07-15](https://unity.com/blog/author-scenes-and-prefabs-with-verson-control))

### Prefab YAML

From [yaml-prefab-serialization, 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/yaml-prefab-serialization.html):

- A prefab instance (in a scene or in another prefab) is a `PrefabInstance` document, class ID `1001`, with `m_SourcePrefab: {fileID: 100100000, guid: <prefab guid>, type: 3}` and an `m_Modification` block containing `m_TransformParent`, `m_Modifications` (property overrides), `m_RemovedComponents`, `m_RemovedGameObjects`, `m_AddedGameObjects`, `m_AddedComponents`.
- Each `m_Modifications` entry has `target` (cross-file reference to the object in the source prefab), `propertyPath` (same as `SerializedProperty.propertyPath`, for example `m_LocalPosition.x`), `value` (a string), and `objectReference`.
- The instance's objects are not written out. When something in the file must reference one (for example a child Transform), Unity writes a "stripped" placeholder with only `m_CorrespondingSourceObject`, `m_PrefabInstance` and `m_PrefabAsset`.
- A variant file is a `PrefabInstance` with `m_TransformParent: {fileID: 0}` whose `m_SourcePrefab` is the base prefab.
- Implication for agents: a property you see on an instance may not appear anywhere in the scene file (it comes from the prefab asset), and editing the prefab asset's YAML does not change instances that override that property.
- Forum reports (author affiliation not confirmed) of `m_Modifications` being reordered on save exist ([discussion](https://discussions.unity.com/t/we-keep-getting-annoying-redundant-change-to-the-order-of-prefabinstance-m_modifications/943063)); **unverified** as documented behaviour.

## 6. Scene and prefab YAML (Force Text)

- Asset Serialization Mode (Project Settings > Editor): **Force Text** converts all assets to text and "is the default option"; Force Binary converts all to binary; Mixed keeps each asset's format and uses binary for new assets. A "Reduce version control noise" option writes references and similar structures on one line. ([class-EditorManager, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/class-EditorManager.html))
- Text scene files exist to make version control merges practical; text "can be generated and parsed by tools". ([TextSceneFormat, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/TextSceneFormat.html))
- Format: each object is its own YAML document introduced by `--- !u!<classID> &<fileID>`; `!u!1` is GameObject, `!u!4` is Transform; the number after `&` is an object ID unique within the file, assigned arbitrarily. Floats may appear as IEEE 754 hex (`0x` prefix); to edit by hand, replace with a decimal. ([FormatDescription, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/FormatDescription.html))
- UnityYAML is "a custom-optimized YAML library" that "does not support the full YAML specification": no comments, no tags in the standard sense, no complex mapping keys, no `|`/`+` block scalar indicators, and multi-line scalars cost performance. The page states: "You cannot externally produce or edit UnityYAML files." ([UnityYAML, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/UnityYAML.html))
- Unity staff: "Manually modifying Asset files is a risky operation and is not supported by Unity"; prefer the Asset Database API. ([Unity blog, 2022](https://unity.com/blog/engine-platform/understanding-unitys-serialization-language-yaml))
- Reconciling the two: Unity documents text files as tool-readable, but does not support external edits. Round-tripping through a general YAML library will corrupt files (non-standard tags, `stripped` markers, custom subset). Small, targeted text edits (for example swapping a GUID in `m_Script` lines, which Unity's own DLL-migration example does) are practical but unsupported; validate by opening in the Editor and letting it re-save.
- Cross-scene references cannot be saved in scene files; the Editor blocks creating them by default (`EditorSceneManager.preventCrossSceneReferences`). ([preventCrossSceneReferences, 6000.0](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.EditorSceneManager-preventCrossSceneReferences.html)) **Unverified** in a single doc sentence, but implied by the reference format: assets (prefabs, ScriptableObjects) cannot hold references to scene objects.

## 7. Version control: merging, binary assets and LFS

- `.meta` files "should **always** be checked into version control". Switching serialization to binary "will remove the ability to merge these files." Unity recommends Smart Merge for YAML, file locking where no conflicts are tolerable, and "Git LFS for any project that has large content files and uses Git". ([Unity blog, Woods, 2024-07-15](https://unity.com/blog/author-scenes-and-prefabs-with-verson-control))
- UnityYAMLMerge ships with the Editor (`<Editor>/Data/Tools/UnityYAMLMerge(.exe)`) and merges `.unity` and `.prefab` files semantically. Git setup: `[merge] tool = unityyamlmerge` and `[mergetool "unityyamlmerge"] trustExitCode = false`, `cmd = '<path>' merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"`. Its behaviour for unresolved conflicts is configured in `mergespecfile.txt`. ([SmartMerge, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/SmartMerge.html))
- Visible vs Hidden meta files is a Version Control project setting; Visible is the mode for external VCS such as Git. ([Versioncontrolintegration, 6000.6](https://docs.unity3d.com/Manual/Versioncontrolintegration.html))
- LFS pointer-file trap: if Git LFS is not installed, Git checks out pointer files "without any error or warning messages", and Unity imports the pointers as content. Documented for UPM Git dependencies (which also use shallow clones); the same failure applies to any LFS-tracked asset. ([upm-git, 6000.2](https://docs.unity3d.com/6000.2/Documentation/Manual/upm-git.html)) An agent should check `.gitattributes` for `filter=lfs` before assuming a binary file's bytes on disk are real.
- Binary assets (textures, audio, models, and any asset saved in binary mode) cannot be merged; their `.meta` files are text and carry import settings, so an agent can change import settings by editing the `.meta` but must never change its `guid:` line. The "never change the guid" rule follows from section 1; editing import settings in `.meta` by hand is **unverified** as a supported workflow (prefer `AssetImporter` APIs).
- Unity documents no official `.gitattributes` or LFS pattern list for projects. **Unverified**: any specific list of extensions.

## 8. ScriptableObject references

- ScriptableObjects are stored as `.asset` files and referenced, not copied: many prefabs referencing one SO share "one copy of the data in memory". ([class-ScriptableObject, 6000.0](https://docs.unity3d.com/6000.0/Documentation/Manual/class-ScriptableObject.html))
- In the Editor, SOs can change in Edit and Play mode, and Play mode changes to an SO asset persist (they are the asset). In a Player they are read-only saved data; runtime changes are not saved. ([class-ScriptableObject](https://docs.unity3d.com/6000.0/Documentation/Manual/class-ScriptableObject.html))
- Script edits to an SO in Edit mode are not saved unless you call `EditorUtility.SetDirty()` (then save). ([class-ScriptableObject](https://docs.unity3d.com/6000.0/Documentation/Manual/class-ScriptableObject.html))
- Referencing a `UnityEngine.Object` serializes only the reference, so SOs are the recommended way to share data instead of embedding custom classes. ([best practices, 6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-best-practices.html))
- The SO script's file name must match its class; the `.asset` refers to the script by GUID, so the same `.meta` rules as MonoBehaviour apply. ([class-ScriptableObject](https://docs.unity3d.com/6000.0/Documentation/Manual/class-ScriptableObject.html), section 1)
- Direct references pull the target and its whole dependency graph into the build. ([assets-direct-reference, 6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/assets-direct-reference.html))

## 9. Addressables references

Package docs are versioned separately from the Editor; checked against Addressables 2.7 unless noted.

- `AssetReference` is a serializable field type that "Stores the guid of the asset" (`m_AssetGUID`) plus an optional `SubObjectName` for sub-objects (sprites in an atlas, objects in a model). ([AssetReference API, 2.7](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/api/UnityEngine.AddressableAssets.AssetReference.html)) So an `AssetReference` survives moves and renames of the target like any GUID reference, and breaks like one if the target's `.meta` is regenerated.
- Dragging a non-Addressable asset onto an `AssetReference` field makes it Addressable and adds it to the default group. Typed subclasses exist (`AssetReferenceGameObject`, `AssetReferenceSprite`, ...) and `AssetReferenceT<T>`; before Unity 2020.1, or when using a custom property drawer, a concrete subclass is required. ([asset-reference-intro, 2.7](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/asset-reference-intro.html))
- `AssetReferenceUILabelRestriction` is Inspector-only; scripts can still assign unlabelled assets. ([asset-reference-create, 2.7](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/asset-reference-create.html))
- Addresses are strings you can change freely in the Groups window; loading can use an address, label, `AssetReference` or `IResourceLocation`. Unlike `AssetReference`, loading by address string is a string contract: renaming an address breaks string-based loads. ([AddressableAssetsOverview, 2.7](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/AddressableAssetsOverview.html)) The default address is the asset's file path, per the older [migration guide, 1.1](https://docs.unity3d.com/Packages/com.unity.addressables@1.1/manual/AddressableAssetsMigrationGuide.html). **Unverified**: whether moving an asset updates an already-assigned address (assume it does not).
- Making an asset in a `Resources` folder Addressable moves it out of `Resources`. ([get-started-make-addressable, 2.1](https://docs.unity3d.com/Packages/com.unity.addressables@2.1/manual/get-started-make-addressable.html))
- Duplication trap: content referenced both from Addressables and from built-in scene data or `Resources` is duplicated on disk and in memory; a non-Addressable scene referencing an Addressable asset directly gets its own copy. ([migration guide, 2.1](https://docs.unity3d.com/Packages/com.unity.addressables@2.1/manual/AddressableAssetsMigrationGuide.html))
- Unity 6.6 adds a content directory build path and a Content Directory schema in Addressables. ([WhatsNewUnity66](https://docs.unity3d.com/6000.6/Documentation/Manual/WhatsNewUnity66.html))
- **Unverified**: the on-disk layout of Addressables settings (`Assets/AddressableAssetsData`, group `.asset` files listing entries by GUID). Commonly observed, not found in the docs checked.

## 10. Sandbox checks for the `unity-serialization` skill

The five checks the design ([#9](https://github.com/Hissal/mattpocock-skills-unity/issues/9)) requires, plus what surfaced while running them. All **observed** on 2026-09-24 in `unity-sandbox/` (6000.6.2f1, `com.unity.pipeline` through a resident headless editor, CLI 1.0.0-beta.11, Windows 11). Values were read back through `SerializedObject` or reflection after each recompile; files were compared by hash before and after each write.

1. **File-scoped namespace**: a MonoBehaviour and a ScriptableObject declared with `namespace T31.FileScoped;` (their own asmdef, `-langversion:10` in its `csc.rsp`) saved as `m_Script: {fileID: 0}` in a prefab and an `.asset`, with the field data present. `MonoScript.GetClass()` returned null for the file-scoped script, and the saved prefab loaded with one missing script (`GetMonoBehavioursWithMissingScriptCount` 1). A block-namespaced MonoBehaviour in the same asmdef saved its real `m_Script` GUID and loaded clean. The RaveRampage claim holds.
2. **`FormerlySerializedAs` and override `propertyPath`s**: after renaming `oldName` to `newName` with the attribute, a scene instance overriding it (7), a variant (9), and a prefab nesting an instance with an override (5) all read the overridden value; the files on disk still said `propertyPath: oldName`. Unity applies old-path overrides through the attribute on load but writes nothing until a file is rewritten.
3. **Scoped `ForceReserializeAssets(paths)`**: rewrote only the listed files. Of 170 files under `Assets/`, `Packages/` and `ProjectSettings/`, only the listed prefabs changed (their `.meta` files did not). What the rewrite does to overrides:
   - A variant and a prefab nesting an overridden instance gained a `propertyPath: newName` entry and kept the `oldName` entry beside it.
   - A scene holding an overridden prefab instance came out byte-identical: its override stayed on `oldName`. Opening, dirtying and saving the scene also left it on `oldName`. `PrefabUtility.RecordPrefabInstancePropertyModifications` on the instance's component, then a save, added `propertyPath: newName` (keeping the old entry).
   - A scene holding a plain (non-prefab) object with the renamed field was rewritten to `newName`.
   - After the attribute was removed: the prefab (1), variant (9), nested prefab (5) and the re-recorded scene (7) kept their values; the scene that was only force-reserialized lost its override (7 fell back to the prefab's 1).
4. **Auto-property conversions**: field `speed` to `[field: SerializeField] float speed { get; set; }` without an attribute read 0 (was 5); with `[field: FormerlySerializedAs("speed")]` it read 5. Back from `[field: SerializeField] float Speed { get; set; }` to a field `Speed`: without an attribute 0, with `[FormerlySerializedAs("<Speed>k__BackingField")]` 5. The backing field serializes as `<Name>k__BackingField`.
5. **Scope script**: a hand-built case with `B` inside `A` inside MonoBehaviour `C`. `SerializationScope.Find("T31.B")` listed exactly: a prefab with `C`, a prefab nesting it, a variant of it, a scene with a non-prefab `C` object, a scene holding the nesting prefab, a prefab with a subclass of `C`, a ScriptableObject `.asset` with `List<A>`, and two prefabs with a `[SerializeReference] IThing` host (one holding a type that contains `B`, one holding a type that does not: the closure is by host type, so the second is an over-reach). A prefab and a scene using only an unrelated component were left out. It took about 1.3 s through `run_script`; the headless `-executeMethod` entry wrote the same list (exit 0, about 41 s including editor start). Found while running it: without filtering, package types with a `[SerializeReference] object` field that have no script asset showed up as noise, so the script notes missing script assets only for project assemblies. `scope.sh` seeded with `C.cs` alone listed the five files reachable from `C`'s GUID and missed the subclass, `[SerializeReference]` and ScriptableObject hosts, as the design expects; seeded with all four host scripts it matched the Editor list.

End to end with the shipped script (a fresh component `Q`, field `before` renamed to `after` with the attribute): `Find` listed four files (prefab, variant, nested prefab, scene); `Reserialize` with that list changed exactly those four files and re-recorded one scene instance; every override then carried `propertyPath: after`; after removing the attribute every value survived (prefab 1, variant 3, nested 5, scene instance 7, plain scene object 4).

Found alongside:

- `run_script --args` takes a JSON array of positional arguments (`'["T31.B"]'`); a bare string is rejected ("expects JArray"), and a `string[]` parameter takes a nested array.
- `eval` snippets reach project assemblies (`Assembly-CSharp`, `Assembly-CSharp-Editor`) directly.
- Unity 6.6 writes `m_EditorClassIdentifier: <Assembly>::<Namespace.Class>` on MonoBehaviours; the script link is still `m_Script`.
- A `[SerializeReference]` field is written as `rid: <id>` with a `references: version: 2, RefIds:` block whose entries carry `type: {class, ns, asm}`. A ScriptableObject `.asset` holds its main object at `--- !u!114 &11400000`.

## Open questions for the skill

- Whether to recommend agents run `ForceReserializeAssets` (it touches many files and needs the Editor) or leave re-saving to humans.
- How strongly to forbid raw YAML edits, given Unity's own DLL-migration example does a find-and-replace on `m_Script`.
- Whether the skill should branch on Unity version (6.6 dictionaries and analyzer) by reading `ProjectSettings/ProjectVersion.txt`.

## Answers from the skill design

Added after [Design unity-serialization](https://github.com/Hissal/mattpocock-skills-unity/issues/9) resolved; the research above stands as written. The [resolution](https://github.com/Hissal/mattpocock-skills-unity/issues/9#issuecomment-5795568716) holds the full design; each answer here is its gist.

- **`ForceReserializeAssets`:** the agent never runs it unprompted. A field rename ships with `FormerlySerializedAs` in one commit; a second commit, only on the user's okay, reserializes the affected files and removes the attribute. The agent proposes the file list first, built by an Editor script shipped with the skill. New fact behind this: `ForceReserializeAssets()` rewrites the whole project, while `ForceReserializeAssets(IEnumerable<string> assetPaths, ForceReserializeAssetsOptions options)` rewrites only the listed files, so a scoped run is light. ([ForceReserializeAssets, 6000.0](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetDatabase.ForceReserializeAssets.html))
- **Raw YAML edits:** a careful, validated operation, not a ban. The order of preference is the Editor, then an `AssetDatabase`/`SerializedObject` script, then a surgical text edit: line-level find-and-replace on known files, never round-tripped through a YAML library (for example a project-wide GUID swap repairing references). Structural edits prefer the Editor and are allowed without one when simple. Every text edit is followed by validation, or reported as unvalidated.
- **Version branching:** yes. A version-gated rule states its gate inline, and the agent reads `ProjectSettings/ProjectVersion.txt` when the gate matters; the version is not copied into the Unity config.
