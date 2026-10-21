---
"mattpocock-skills-unity": patch
---

`unity-verification` picks each check by the risk it establishes instead of running every cheaper rung first: the full EditMode suite runs once at final review, PlayMode only for a contract that needs the real engine, and a visual or content edit gets an import and a focused inspection with no test suite. `unity-testing` drops "the module already has PlayMode tests" as a PlayMode trigger, and `unity-prototyping` separates its Play hand-off check from the PlayMode rung.
