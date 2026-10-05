---
"mattpocock-skills-unity": patch
---

`implement-spec`: in a Unity repo it now asks the `unity` router how to run implementers in parallel worktrees (a warm `Library/` copy per worktree, or one ask and cold imports one at a time) and checks the integration branch compiles or works before marking it ready or reporting it, naming what stayed unvalidated. The `unity` router gains a row for running work in parallel worktrees, routed to `unity-verification`.
