---
"mattpocock-skills-unity": patch
---

Unity verification no longer runs every cheaper check before the one a change needs: `unity-verification` picks each check by the risk it establishes, `unity-testing` narrows when the full EditMode suite and PlayMode tests run, and a visual or content edit is checked by inspection instead of test suites.
