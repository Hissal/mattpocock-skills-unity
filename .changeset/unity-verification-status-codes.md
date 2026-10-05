---
"mattpocock-skills-unity": patch
---

`unity-verification`: editor detection reads `unity status`'s `errors[0].code` rather than its exit code, and waits with `unity status --until-ready`. A new pending-editor section covers `STATUS_PIPELINE_LOAD_PENDING`, including Assets > Refresh when Auto Refresh is off. Safe Mode is recognised by a pending state a refresh does not clear plus the project log or window title, not by `unity pipeline list`, and the editor leaves it after a fix and a refresh, with no restart. The exit-code table's 6 and 7 rows match. `unity-verification` and `unity-debugging` both point to `unity commands --grep` for CLI syntax. The research is re-pinned to CLI 1.0.0-beta.12 with the checks behind these changes.
