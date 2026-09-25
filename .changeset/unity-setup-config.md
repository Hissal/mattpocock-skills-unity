---
"mattpocock-skills-unity": minor
---

`setup-matt-pocock-skills` writes the Unity config in a Unity repo: it discovers the project path and shape, the repo's Unity conventions docs, its check runners, CI workflow, test filters and build settings, its assembly naming, where project-wide defines live and any third-party serializer, confirms them in one list, asks the Unity policies as one "keep the defaults?" question, and writes `docs/agents/unity.md` holding only what differs from the Unity skills' defaults (repo-relative paths only, updated in place on re-run). Non-Unity repos see nothing new. `unity-verification` now reads the config's build, test filter and heavy-run warning fields.
