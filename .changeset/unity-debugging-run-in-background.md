---
"mattpocock-skills-unity": patch
---

`unity-debugging`: an unfocused GUI editor stalls Play because `runInBackground` is off, not because of focus. The Play loop script now turns `Application.runInBackground` on before every Play and restores the user's value on every exit, so unfocused runs go in real time; stepping frames is a fallback for when frames still do not advance. The skill points at Unity's Play-mode verification loop recipe, and the research records the checks.
