# Contributor policy slice 114

Implementation of [#114](https://github.com/Hissal/mattpocock-skills-unity/issues/114), under [#107](https://github.com/Hissal/mattpocock-skills-unity/issues/107). Approved decision bodies [#108](https://github.com/Hissal/mattpocock-skills-unity/issues/108), [#109](https://github.com/Hissal/mattpocock-skills-unity/issues/109), and [#110](https://github.com/Hissal/mattpocock-skills-unity/issues/110) were fetched and read before implementation. A ready label was not treated as policy approval.

## Integration and disposition

The implementer worktree started at integration commit `d1f332e2e3ba7c9a45a6d3e5b25d24abbfd9781a`, on `feat/adapt-contributor-triage`. Approved acceptance seams were scope/form/workflow agreement and source/configuration validation, without disposable tracker writes. No Unity mechanic or skill behavior changed.

Incoming content was reapplied from pinned upstream `49dd158d1076134a641b33efb035946536778336`, rather than merging later upstream commits. `SCOPE.md` is the fork's contribution policy. All nine rejection records retain upstream rationale and historical references, with an explicit fork disposition above it. Upstream Issue references now link upstream, and first-person historical statements remain identified as upstream statements. The blanket new-skill and frozen-skill restrictions are adapted to the approved Unity proposal and temporary real-defect-fix exceptions. Shared preference switches remain out of scope while repo-specific configuration is retained.

Bug and Idea forms point to fork scope. The Idea evidence field permits a concrete Unity use case and gap evidence, or source/Editor-backed Unity accuracy work without a failed agent session. Blank Issues stay enabled for other work and fork questions. The contact links separate fork/Unity questions from general upstream questions. Fork Discussions and all repository setting changes remain deferred; no destination was created.

Both forms label new Issues `needs-triage`; the upstream `opened` workflow catches other creation paths. The needs-info workflow retains all upstream action inputs other than the exact approved closure message. The optional guidance-link sentence is omitted as permitted by #110. `SCOPE.md` and `docs/agents/triage-labels.md` explain inactivity timing, qualifying updates, review after requeue, retained closure label, and manual reopening. The labeling workflow remains byte-equivalent to the pinned upstream file after newline normalization. README and CLAUDE additions are short policy pointers; installation content is untouched.

## Verification on 2026-10-10

- TDD at the confirmed configuration seam used one-off drivers in `%TEMP%`: `issue-114-policy-check.py` and `issue-114-agreement.py`. The workflow check first failed because the deferred workflow did not exist, then passed after implementation. The form check first failed because the chooser config did not exist, then passed after adaptation. No generic test framework was added.
- Parsed all repository workflow YAML and form YAML. Checked daily `30 1 * * *` and manual triggers, new-Issue `opened` event, unique form IDs, supported field shapes, blank-Issue availability, destinations, labels, and scope/evidence-field agreement. These checks passed.
- Compared every needs-info action input other than its message to pinned upstream and asserted the exact approved folded message. Checked unchanged action version and byte-equivalent triage workflow. Passed.
- Rechecked live labels using `gh label list --limit 100 --json name`: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`, and `bug` are present. Form and workflow labels agree with the inventory. No labels or Issues were created or changed.
- Rechecked `actions/stale@v10`, resolving to `1e223db275d687790206a7acac4d1a11bd6fe629`. Read its [Issue processor](https://github.com/actions/stale/blob/1e223db275d687790206a7acac4d1a11bd6fe629/src/classes/issues-processor.ts). Source confirms closed-Issue skip, qualifying comment/update requeue, `updated_at` close-window logic, `not_planned`, and retention of the stale label on closure. This preserves upstream's mutable version tag and maintenance tradeoff.
- Checked relative links in the touched guidance and rejection records, including the intentional new triage guidance file. Historical prior-request references all identify upstream. Passed.
- `npm run check`: passed, including plugin-version and strict plugin validation with only documented root-context warnings ignored. Package and plugin remain `1.1.0`.
- `git diff --check` and staged whitespace check: passed.

`docs/agents/` was excluded by local `.git/info/exclude`; only the intentional `triage-labels.md` policy file was force-added. Other local configuration files remain excluded.

## Limits

Live chooser rendering, scheduled labeling, reply/requeue, closure, and manual reopening remain Unvalidated. No authorized disposable Issue was available and existing user Issues were not touched. YAML parsing, configuration comparisons, source review, and label inventory establish configuration agreement, not exercised hosted transitions. Workflow checks were local; `actionlint` was not installed. Automation starts only when these workflows reach the default branch; this implementation branch remains unmerged.

No repository settings, Discussions, tracker status, release files, plugin manifests, global installations, or Unity sandbox were changed. The terminal sandbox could not start commands in this session; approved elevated execution ran the checks. Unity Editor execution is not required for this contributor-policy change.
