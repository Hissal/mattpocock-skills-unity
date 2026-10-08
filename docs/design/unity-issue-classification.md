# Unity issue classification

Design agreed on 2026-10-08. The implemented rules live in [Unity verification's issue classification reference](../../skills/unity/unity-verification/ISSUE-CLASSIFICATION.md); this note records the scope and reasons behind the design.

## Purpose and scope

Issue triage and selection need to distinguish Unity requirements from human participation. Classification covers the whole issue through required verification. Interactive Unity work may be performed by an agent, while the repo's human-involvement and readiness policies determine who must participate.

This is an optional convention for consuming Unity repos. Setup offers adoption or a disabled choice, reuses existing conventions, and preserves the repo's choice when re-run. The configuration field is documented in the [Unity config seed](../../skills/engineering/setup-matt-pocock-skills/unity.md).

## Configuration choice

Use Markdown rules, tables and pointers to canonical repo conventions. The skills are prompt-driven and already interpret repo mappings, so a new expression language or general inference framework would add machinery without a current deterministic consumer.

RaveRampage demonstrates the existing flexibility. Its `docs/agents/triage-labels.md` maps upstream roles to Project status and human-involvement labels rather than dedicated readiness labels. Todo with review involvement permits agent work; Backlog does not; Todo with decide involvement requires a human decision. Its config also points at the owning conventions for reading and updating those fields.

The Unity addition uses that configuration approach. It preserves upstream readiness definitions and consuming-repo policies, and keeps upstream edits to short Unity router pointers and their documentation.

## Classification and selection choices

An absent classification means unknown. Treating absence as no Unity requirement would make filters unreliable. Workflows classify only when requirements provide enough evidence, and record a short reason through their normal write process.

Selection reports unknown candidates separately and excludes inconsistent classifications from filters that depend on a trustworthy level. It checks readiness, blockers and current capabilities separately; changing the current agent's tools does not change the issue's requirements.

## RaveRampage follow-up

The existing RaveRampage rubric conflates interactive Unity requirements with a human looking or playing. [Issue #730](https://github.com/Hissal/RaveRampage-Unity/issues/730) requests the same Unity meanings as this addition while preserving its independent human-involvement and readiness policies. It was filed in Triage; correcting that repo's rubric is separate work.
