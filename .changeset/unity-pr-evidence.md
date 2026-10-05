---
"mattpocock-skills-unity": patch
---

`pr`: in a Unity repo it now asks the `unity` router how to capture the Game or Scene view and how to read Unity test results for its Evidence section. The router gains a row for each: capture goes to `unity-debugging` (capture fails headless) and the Unity CLI's own skill, read with `unity skill show` and searched with `unity skill show --list`, and test results go to `unity-verification`. The glossary's Unity router entry now names the CLI's skill as a route.
