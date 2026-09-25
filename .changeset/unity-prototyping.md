---
"mattpocock-skills-unity": minor
---

Add the `unity-prototyping` skill: prototypes in the engine by default (HTML offered in one line only for a flat screen layout, or for logic when the judge will not open the editor), each in `<prototype folder>/<name>/` with an asmdef of `autoReferenced: false` and `defineConstraints: ["UNITY_EDITOR"]` (or `#if UNITY_EDITOR` when the code it needs is in `Assembly-CSharp`), a prototype scene with the host scene opened additively and never saved, one `Prototypes/<name>` menu item with a bottom-centre variant switcher or an EditorWindow logic driver, and capture and removal behind a GUID grep. Setup now always asks the prototype folder and creates a new default folder self-ignoring. The `unity` router gains its row and `ask-matt` its line.
