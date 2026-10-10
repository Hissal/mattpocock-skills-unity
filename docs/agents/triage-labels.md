# Triage labels

The skills speak in terms of five canonical triage roles. This file maps those roles to the actual label strings used in this repo's issue tracker.

| Label in mattpocock/skills | Label in our tracker | Meaning                                  |
| -------------------------- | -------------------- | ---------------------------------------- |
| `needs-triage`             | `needs-triage`       | Maintainer needs to evaluate this issue  |
| `needs-info`               | `needs-info`         | Waiting on reporter for more information |
| `ready-for-agent`          | `ready-for-agent`    | Fully specified, ready for an AFK agent  |
| `ready-for-human`          | `ready-for-human`    | Requires human implementation            |
| `wontfix`                  | `wontfix`            | Will not be actioned                     |

When a skill mentions a role (e.g. "apply the AFK-ready triage label"), use the corresponding label string from this table.

Judge every issue against [`SCOPE.md`](../../SCOPE.md) before picking a state. Rejections close as "not planned".

## Information requests

The [approved fork policy](https://github.com/Hissal/mattpocock-skills-unity/issues/110) uses `actions/stale@v10`. It runs daily at 01:30 UTC and can also be run manually. It targets only open Issues with `needs-info`, never PRs, and does not mark other Issues stale.

Ask for specific missing information, then apply `needs-info`. Applying the label after asking matters because a subsequent maintainer comment can itself trigger requeue. After 14 days of inactivity under the action's existing last-update logic, an Issue closes as `not_planned`, with `needs-info` retained. Closure means work stopped while awaiting information, not that the report was resolved.

Qualifying human comments and other qualifying Issue updates remove `needs-info` and add `needs-triage` on the next scheduled or manual run. This is not reporter-only or instantaneous and does not assess reply content. An acknowledgment can requeue an Issue without supplying the requested details. Review the reply; if information remains missing, explain what is still needed and then reapply `needs-info`. Otherwise choose the appropriate triage outcome.

Closed Issues are skipped. Reporters may provide missing details at any later time. A maintainer reviews them and reopens when actionable, manually removing `needs-info` and applying `needs-triage`. A late comment alone does not reopen the Issue; an acknowledgment alone does not require reopening. No maintainer response time is promised.

New-Issue labeling handles `opened` only, so manual reopening must include the label changes above. The forms also add `needs-triage`; blank Issues are caught by the labeling workflow.
