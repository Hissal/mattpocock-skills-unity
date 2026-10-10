# Hard limits on the number of questions during grilling

## Fork disposition

Add no shared question-count setting, including for Unity skills. Natural-language user steering to stop or wrap up remains valid.

Approved in [fork Issue #108](https://github.com/Hissal/mattpocock-skills-unity/issues/108).

## Historical upstream record

The text below records upstream's rationale and prior requests at [49dd158](https://github.com/mattpocock/skills/blob/49dd158d1076134a641b33efb035946536778336/.out-of-scope/question-limits.md). First-person statements and all prior-request references are upstream history. The fork disposition above governs this fork.

The `/grill-me` skill (and grilling sessions inside other skills) does not enforce a maximum number of questions. Requests to add a configurable cap or hard ceiling are out of scope.

## Why this is out of scope

Grilling is intentionally open-ended. The point is to keep digging until each branch of the decision tree is resolved: some plans need three questions, some need fifty. A fixed cap would either cut off useful exploration on hard problems or feel arbitrary on easy ones.

If a session feels too long, the right escape hatches already exist:

- The user can stop the session at any time and accept the current state of the plan.
- The user can tell the model to wrap up, summarise, and move on, since natural-language steering is the intended control surface, not a numeric limit.

Adding a hard cap would also conflate two different failure modes: a model that asks too many questions because the plan is genuinely under-specified (working as intended) vs. a model that asks redundant or low-value questions (a prompt-quality issue, not a quantity issue). The fix for the latter belongs in the skill prompt, not in a counter.

## Prior requests

- [#44](https://github.com/mattpocock/skills/issues/44): "Codex just asked me 200 questions"
