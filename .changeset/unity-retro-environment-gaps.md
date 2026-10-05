---
"mattpocock-skills-unity": patch
---

`retro`: in a Unity repo it now asks the `unity` router for Unity environment gaps in the session: checks it left unvalidated and why, a missing Unity config, no test assembly or asmdefs, or an open editor without `com.unity.pipeline`. The router gains a row for that need, routed to `unity-verification`, `unity-testing` and `unity-assemblies`; a missing Unity config is already reported by the router itself.
