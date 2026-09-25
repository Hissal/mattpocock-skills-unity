# What serializes

## Field rules

A field serializes when it is `public` or `[SerializeField]`, is not `static`, `const` or `readonly`, and has a serializable type:

- primitives, `string`, enums of 32 bits or less, fixed-size buffers;
- Unity built-ins (`Vector3`, `Color`, `Rect`, `AnimationCurve` and the like);
- `[Serializable]` custom classes and structs, including generic ones (`Wrapper<int>`);
- references to `UnityEngine.Object` subclasses (a ScriptableObject, a prefab, a component): only the reference is stored, never the object;
- one-dimensional arrays and `List<T>` of the above.

Not serialized: properties (an auto-property's backing field is, with `[field: SerializeField]`, under the name `<Name>k__BackingField`), multidimensional and jagged arrays, nested containers (wrap the inner list in a `[Serializable]` class), and dictionaries before 6.6.

## Inline versus `[SerializeReference]`

A `[Serializable]` class in a plain field is stored **inline**, by value, as the field's declared type:

- no null: a null field comes back as a new default instance;
- no polymorphism: a derived instance in a base-typed field is saved as the base type, and the derived data is lost;
- no sharing: two fields pointing at one instance load as two copies;
- self-reference cycles stop after 10 levels, with the rest discarded and a warning.

`[SerializeReference]` stores the object **by reference**, in a `references` block after the host's fields, one entry per object with its class, namespace and assembly (this is why renaming or moving the type needs `MovedFrom`). It allows null, polymorphism (interface, abstract or `System.Object` fields), shared references and cycles. The rules:

- The stored object must be a `[Serializable]` class: not a `UnityEngine.Object`, not a value type, not a dictionary.
- Sharing only works within one host object: two ScriptableObjects never share a managed reference.
- A missing type loads as null. The file keeps the stored data only until the host is re-saved, which writes the reference as null and drops the data. `SerializationUtility.HasManagedReferencesWithMissingTypes` is documented to find them but returned false on 6000.6.2f1; the `type:` lines of the file's `references:` block are the reliable check.
- It costs more storage and load time than inline: use it only for what inline cannot do.

## Dictionaries (6.6 and later)

From Unity 6.6, a `Dictionary<TKey, TValue>` field serializes only with `[SerializeField]`: a public dictionary field without it is skipped. Before 6.6, a dictionary silently does not serialize at all, attribute or not; check the version before relying on one.

- The declared type must be exactly `Dictionary<TKey, TValue>`.
- Keys cannot implement `IEnumerable` (`string` excepted); keys and values cannot be interfaces or abstract types (except `UnityEngine.Object` types).
- No `[SerializeReference]` on a dictionary field, and no dictionary directly inside another collection.
- Prefab instances override single entries.

Unity 6.6 also ships a compile-time serialization analyzer (rules UAC1000 to UAC1022): `[SerializeReference]` on a struct or dictionary, public fields skipped because their type lacks `[Serializable]`, cycles, unsupported collections, and a field name clashing with a `FormerlySerializedAs` argument (UAC1018). Its rule list is a useful review checklist on older versions too.

## Callbacks and threading

- Constructors and field initializers of serializable types run on a loading thread: call no Unity API there.
- `ISerializationCallbackReceiver` (`OnBeforeSerialize`, `OnAfterDeserialize`) is the pre-6.6 way to store an unsupported type, such as a dictionary as two lists. It runs off the main thread: touch only the object's own fields. A host's callbacks run before those of its managed references.
- A script edit to a ScriptableObject in Edit mode is saved only after `EditorUtility.SetDirty` and a save. Play mode changes to a ScriptableObject asset persist in the Editor; in a player they are never saved.
