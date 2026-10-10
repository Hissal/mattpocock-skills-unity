# Scope

This is the Unity adaptation of [mattpocock/skills](https://github.com/mattpocock/skills). General skills follow upstream. This fork maintains Unity skills and Unity deltas in upstream skills. Questions about the fork, including Unity deltas in upstream files, belong in [fork Issues](https://github.com/Hissal/mattpocock-skills-unity/issues). General upstream skill questions belong in [upstream Discussions](https://github.com/mattpocock/skills/discussions).

## The bar

Unity behavior changes and bug reports require an observed failure: what you ran, what the skill did, and what you expected. Unity documentation and accuracy corrections, and this fork's own documentation corrections, can use authoritative source or Editor evidence. New Unity capability proposals require a concrete use case and evidence of a gap. Compose existing skills when they already cover the need.

General skills follow upstream's observed-failure bar and contribution process. The evidence exceptions above apply only to Unity work and the fork's own documentation. A real upstream defect may receive a small, well-reasoned temporary fix here. Check upstream for an existing Issue addressing it, consider filing one if absent, and remove the temporary fix once upstream resolves the defect. This permits no general feature development in frozen skills.

Keep the shared method consistent. Preserve repo-specific configuration, including tracker instructions, triage label mappings, domain-doc locations, and the external-PR request-surface flag. Unity config can hold project paths, verification commands, conventions, and execution or approval constraints. Shared behavior-preference switches, such as maximum question counts or question-interface choices, remain out of scope, including in Unity skills.

Harness-specific behavior should generally stay out. Support the harnesses upstream supports; any exception needs a considered justification. An upstream author's personal workflow is not automatically fork policy.

## By bucket

- **General skills in `engineering/`, `productivity/`, and `in-progress/`**: follow upstream's evidence requirements and contribution process; this fork owns their Unity deltas.
- **`unity/` and Unity deltas**: maintained here under the applicable evidence requirements above.
- **`misc/`**: follow upstream's freeze on general feature development, with only the narrow temporary real-defect-fix exception above.
- **New skills**: Unity proposals can be considered with a concrete use case and gap evidence. General new skills remain upstream's responsibility.

## Already decided

The fork dispositions in [`.out-of-scope/`](./.out-of-scope/) implement the [approved scope decision](https://github.com/Hissal/mattpocock-skills-unity/issues/108). Each record identifies its historical upstream text and Issue references. A historical upstream rejection alone is not a new fork decision.

- [`frozen-misc-skills.md`](./.out-of-scope/frozen-misc-skills.md)
- [`harness-name-collisions.md`](./.out-of-scope/harness-name-collisions.md)
- [`lazy-load-writing-for-agents.md`](./.out-of-scope/lazy-load-writing-for-agents.md)
- [`mainstream-issue-trackers-only.md`](./.out-of-scope/mainstream-issue-trackers-only.md)
- [`native-question-tool.md`](./.out-of-scope/native-question-tool.md)
- [`new-skills.md`](./.out-of-scope/new-skills.md)
- [`question-limits.md`](./.out-of-scope/question-limits.md)
- [`setup-skill-verify-mode.md`](./.out-of-scope/setup-skill-verify-mode.md)
- [`subagent-recursion.md`](./.out-of-scope/subagent-recursion.md)

## Filing and information requests

Prefer the Bug and Idea forms when they fit. Blank Issues remain available for questions, cross-skill reports, repository documentation fixes, and work outside those forms. Fork Discussions remain deferred; no repository setting change is required.

Every new Issue enters `needs-triage`. When information is missing, a maintainer asks specific questions and then applies `needs-info`. Open Issues with that label close as `not_planned` after 14 days of inactivity under the action's existing last-update logic. Qualifying human comments and other qualifying Issue updates remove `needs-info` and add `needs-triage` on the next daily or manual run. Requeue requires review; it does not establish that a reply is sufficient.

Reporters may supply missing details on a closed Issue at any later time. A maintainer reviews them and manually reopens when actionable, removes `needs-info`, and applies `needs-triage`. A late comment alone does not reopen an Issue. No maintainer response time is promised. See [triage guidance](./docs/agents/triage-labels.md) for the workflow details and maintainer actions.
