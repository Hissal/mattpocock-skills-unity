---
"mattpocock-skills-unity": patch
---

`unity-verification`: a new checkout or worktree now gets its `Library/` copied from a warm checkout of the same project by default, with a fast multithreaded copy, and the copy drops the source editor's instance files when an editor holds it. Without a warm checkout, a run that creates several worktrees asks once for all of them and runs their cold imports one at a time, since concurrent cold imports ran out of memory in the sandbox checks. `unity-debugging`'s bisection row points at the copy. The research records the checks.
