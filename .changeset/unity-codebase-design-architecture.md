---
"mattpocock-skills-unity": minor
---

`codebase-design` and `improve-codebase-architecture` gain their Unity deltas: in a Unity repo, `codebase-design` calls the `unity` router for designing a module's seams (where the engine sits as a dependency and how to test across it, and seams that are also assembly boundaries), and `improve-codebase-architecture` calls it to keep asset churn out of the hotspot scan, with its explorer sub-agent calling it for Unity friction signals. Each docs page gains one Unity line.
