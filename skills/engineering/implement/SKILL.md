---
name: implement
description: "Implement a piece of work based on a spec or set of tickets."
disable-model-invocation: true
---

Implement the work described by the user in the spec or tickets.

If the user passes a ticket reference, fetch it from the issue tracker and state its title before starting. If the reference is ambiguous, ask.

Call the Skill tool with "tdd" where possible, at pre-agreed seams.

Run typechecking regularly, single test files regularly, and the full test suite once at the end.

In a Unity repo, call the Skill tool with "unity" for how often to run the tests and checks, and for checking a change compiles or works before calling it done.

Once done, call the Skill tool with "code-review" to review the work.

Commit your work to the current branch.
