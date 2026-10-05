---
"mattpocock-skills-unity": patch
---

`unity-debugging`: the Play loop script's domain-reload wait no longer polls `editor_status`. It pauses 1 s, then checks that the AppDomain mark is gone, since on Pipeline 0.8 the CLI waits through the reload by itself once the reload has started, and a command sent at once fails with a 400. The research records the checks.
