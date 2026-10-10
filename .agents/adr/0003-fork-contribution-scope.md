---
status: accepted
---

# Keep general skills aligned with upstream and own the Unity adaptation

The maintainer approved this decision on 2026-10-10 after the [Issue #108](https://github.com/Hissal/mattpocock-skills-unity/issues/108) interview. It determines the contribution scope for the [pinned upstream sync](https://github.com/Hissal/mattpocock-skills-unity/issues/107). General skills follow upstream so this fork remains a Unity adaptation rather than a separate general skill catalog. Unity skills and Unity deltas remain under this fork's control.

## Settled boundary

The maintainer confirmed that general skills follow upstream. This fork manages Unity skills and Unity deltas in upstream skills. New skill proposals are limited to Unity skills; general skills remain an adaptation of whatever upstream is doing.

Harness-specific behavior should generally stay out. The fork should support the harnesses upstream supports. Any harness-specific exception needs a considered justification.

## Evidence and temporary upstream fixes

For Unity skills and Unity deltas, bug reports and behavior changes require an observed failure. Documentation and accuracy corrections can use direct source or Editor evidence. New Unity capability proposals need a concrete use case and evidence of a gap. The evidence exceptions apply only to Unity work and the fork's own documentation; general skills follow upstream's evidence bar and contribution process.

Unity skills and Unity deltas are maintained by this fork. Real upstream defects may receive a small, well-reasoned temporary upstream fix. Check upstream for an existing issue addressing the defect; if none exists, consider filing one. Remove the temporary fix once upstream resolves the defect. This exception does not authorize general feature development in frozen upstream skills.

## Configuration

Preserve upstream's repo-specific configuration. Apply the same approach to Unity config for project paths, verification commands, conventions, and execution or approval constraints. Keep the skills' shared method consistent. Do not add preference switches such as maximum question counts or question-interface choices, including in Unity skills.

Investigation of [upstream setup](https://github.com/mattpocock/skills/blob/49dd158d1076134a641b33efb035946536778336/skills/engineering/setup-matt-pocock-skills/SKILL.md) confirms custom triage label mappings, tracker instructions, domain-doc locations, and an editable external-PR request-surface flag. Its [docs](https://github.com/mattpocock/skills/blob/49dd158d1076134a641b33efb035946536778336/docs/engineering/setup-matt-pocock-skills.md) distinguish those inputs from rejected per-user behavior preferences such as interview cadence, question format, and tone. Repo-specific configuration is already part of upstream's design.

## Incoming rejection records

These are approved implementation dispositions derived from the settled upstream-alignment boundary. Preserve the records' upstream provenance and historical issue references. A historical upstream rejection is not by itself a new fork decision.

- `frozen-misc-skills`: follow upstream's freeze for general feature development, with the maintainer's narrow temporary real-defect-fix exception recorded above.
- `new-skills`: adapt the blanket prohibition to permit Unity skill proposals with a concrete use case and gap evidence. Compose existing skills when they already cover the need; general new skills remain upstream's responsibility.
- `harness-name-collisions`: follow upstream's namespaced invocation approach rather than rename skills or introduce aliases for collisions.
- `mainstream-issue-trackers-only`: follow upstream's existing templates and freeform Other route rather than add first-class templates for every tracker.
- `native-question-tool`: follow upstream's plain-chat questioning; add no shared question-interface preference, including for Unity skills.
- `question-limits`: add no shared question-count setting, including for Unity skills. Natural-language user steering to stop or wrap up remains valid.
- `setup-skill-verify-mode`: follow upstream's existing setup invocation with a natural-language verification request rather than introduce a separate flag or skill.
- `subagent-recursion`: follow upstream's reliance on harness recursion controls rather than add per-skill depth or leaf guards.
- `lazy-load-writing-for-agents`: retain upstream's unconditional writing-for-agents load in retro.

The [pinned upstream records](https://github.com/mattpocock/skills/tree/49dd158d1076134a641b33efb035946536778336/.out-of-scope) are the source of these concepts. No other Unity-specific exception is proposed. The general instruction to avoid harness-specific behavior permits a considered exception when required for supported Unity operation; it does not adopt an upstream author's personal workflow as the fork's policy.

## Completion

The maintainer confirmed shared understanding, including the incoming-record dispositions derived from the upstream-alignment rule. Issue #108 is the tracker record of the approved decision. Contributor-guidance implementation remains separate work.

Issue-routing and information-request closure decisions belong to their separate decision work under Issue #107.
