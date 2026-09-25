---
"mattpocock-skills-unity": minor
---

Add the `unity-code-lifecycle` skill: build as if domain reload is off, pick the lifecycle API by Unity version and the module's existing pattern, give every static, singleton and static event a reset path (`[AutoStaticsCleanup]`, `[OnEnteringPlayMode]`, or the pre-6.5 fallbacks) or a deliberate `[NoAutoStaticsCleanup]`, and avoid the load-code hazards (import workers, `[InitializeOnLoad]` before import completes). The `unity` router gains its rows and `ask-matt` its line.
