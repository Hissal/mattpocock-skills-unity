# Unity

Unity mechanics true for any Unity repo: never facts about a specific repo, which live in that repo's Unity config (`docs/agents/unity.md`).

## User-invoked

None. Every Unity skill is model-invoked, so the agent and the other skills can reach it.

## Model-invoked

Model- or user-reachable (rich trigger phrasing so the model can reach for them).

- **[unity](./unity/SKILL.md)**: Unity knowledge entry point. Detects a Unity repo, reads its Unity config, and routes a need to the Unity skills that answer it.
- **[unity-verification](./unity-verification/SKILL.md)**: Unity verification rules. Picks the cheapest check on the Verification ladder that can catch a change's risk, runs it in the connected editor or headless, reads the result, and reports what stayed unvalidated.
- **[unity-assemblies](./unity-assemblies/SKILL.md)**: Unity assembly and compilation rules. Keeps code in the right assembly, the Editor/runtime split and asmdef GUID references intact, proposes asmdef changes rather than making them, and maps assembly names and GUIDs with a script.
- **[unity-serialization](./unity-serialization/SKILL.md)**: Unity serialization rules. Keeps `.meta` GUIDs and serialized values intact through renames, moves and deletes, scopes the reserialize that follows a rename, edits scene and prefab text surgically, and reviews big asset diffs.
- **[unity-testing](./unity-testing/SKILL.md)**: Unity test rules. Picks EditMode or PlayMode, keeps MonoBehaviours humble so logic tests in EditMode, avoids the engine's test traps, sets up test assemblies, and chooses which tests run and how often.
- **[unity-code-lifecycle](./unity-code-lifecycle/SKILL.md)**: Unity code lifecycle rules. Gives every static, singleton and static event a reset path that holds with domain reload off, picks the lifecycle API by Unity version and the module's existing pattern, and flags load-code hazards.
