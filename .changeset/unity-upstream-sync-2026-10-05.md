---
"mattpocock-skills-unity": minor
---

Merge upstream `mattpocock/skills` from `c55ee46` through `4588b32` (v1.3.0, v1.3.1 and later); see [upstream's CHANGELOG](https://github.com/mattpocock/skills/blob/main/CHANGELOG.md) for what changed. A minor bump, not a patch, because the sync removes a skill and renames a convention:

- `resolving-merge-conflicts` is removed, as upstream removed it, along with its Unity delta. Unity asset conflicts still route to `unity-serialization`.
- The domain glossary is now `GLOSSARY.md` (or `GLOSSARY-MAP.md`), not `CONTEXT.md`. Rename yours to keep the skills reading it.
- `pr`, `retro` and `implement-spec` graduate to `engineering/` and ship in the plugin.
