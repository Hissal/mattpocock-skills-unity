# Unity

Unity mechanics true for any Unity repo: never facts about a specific repo, which live in that repo's Unity config (`docs/agents/unity.md`).

## User-invoked

None. Every Unity skill is model-invoked, so the agent and the other skills can reach it.

## Model-invoked

Model- or user-reachable (rich trigger phrasing so the model can reach for them).

- **[unity](./unity/SKILL.md)**: Unity knowledge entry point. Detects a Unity repo, reads its Unity config, and routes a need to the Unity skills that answer it.
