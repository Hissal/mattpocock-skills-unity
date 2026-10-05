---
"mattpocock-skills-unity": patch
---

`implement-spec`: in a Unity repo it now asks the `unity` router how to set up implementers' parallel worktrees, which `unity-verification`'s new-checkout rule answers, and how to verify the integration branch before marking it ready or reporting it, naming what stayed unvalidated. The `unity` router gains a row for running work in parallel worktrees, routed to `unity-verification`.
