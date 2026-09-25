# Unity setup

Section D of setup, and only in a Unity repo: exploration found a folder holding `ProjectSettings/ProjectVersion.txt` (a Unity project) or a `package.json` with a top-level `unity` field (a UPM package). In any other repo, skip all of this silently: no question, no `docs/agents/unity.md`, no `### Unity` sub-block.

It writes the **Unity config**, `docs/agents/unity.md`, which the `unity` router and every Unity skill read. The seed [unity.md](./unity.md) is the field reference: every field, what it means, and the Unity skill that owns it.

## Rules for what gets written

- **Only what differs.** A field is written only when its value differs from the owning Unity skill's default; a missing field means that default. Defaults live in the owning skill, never in the config and never in this file. **Project** is the exception: always written.
- **Pointers, not copies.** **Conventions** points at the repo's own Unity docs by path; it never restates them.
- **Repo-relative paths only.** An absolute path (a drive letter, a leading `/` or `~`) is refused, with the reason: the config is committed, and every other checkout, worktree and CI runner reads it where that path does not exist. Offer the repo-relative form when the path is inside the repo; otherwise leave the value out.
- **Commands run from the repo root**, so a command inside a nested project folder carries that folder in its path.
- **No machine data**: no editor install paths (the `unity` CLI finds the editor), usernames or licence data. There is no local override file: a machine-specific need is resolved at run time, in the session.
- **No discovery script.** Explore by reading, like the rest of setup.

## 1. Explore, silently

Find each fact without asking. The same detection as the `unity` router: the repo root first, then a glob below it that skips `.git/`, `Library/`, `Temp/`, `Logs/`, `obj/` and `node_modules/`.

| Fact | Where to look | Field |
|---|---|---|
| Project path(s) and shape | the detection above: a project, a package, or both | Project |
| Unity conventions docs | `AGENTS.md` / `CLAUDE.md` sections and task tables, `docs/`, `CONTRIBUTING.md`, READMEs inside the project folder. Prefer one existing index (a task table that already routes to the docs) over listing every doc | Conventions |
| `com.unity.pipeline` | `Packages/manifest.json` of each project | shown, not written: `unity-verification` reads the manifest itself |
| Check runners | scripts that run the repo's text checks, compile or Unity tests (`Tools/`, `scripts/`, `package.json` scripts, a `Makefile`), and the ladder rung each performs | Verification: rung commands |
| CI workflows | `.github/workflows/`, `.gitlab-ci.yml` and the like, for jobs that run Unity (`game-ci/`, `unity test`, `-runTests`, `-executeMethod`) | Verification: authoritative CI workflow |
| Test filters | the filters or categories those runners and CI jobs pass (`-testFilter`, `-testCategory`, `-assemblyNames`) | Verification: test filters |
| Test assembly layout | the folders and `name`s of existing test asmdefs (those referencing `nunit.framework.dll`, `UnityEngine.TestRunner` or `"optionalUnityReferences": ["TestAssemblies"]`), and any test layout rule in the conventions docs | Testing: test assembly layout |
| Mocking library | a mocking DLL or package (`NSubstitute.dll`, `Moq.dll`, a `package.json` or `manifest.json` entry) referenced by test asmdefs | Testing: mocking library |
| `com.unity.test-framework` | `Packages/manifest.json` of each project | shown, not written: `unity-testing` reads the manifest itself |
| Build targets and method or profile | CI build steps (`targetPlatform`, `buildMethod`), build scripts calling `BuildPipeline.BuildPlayer`, Build Profile assets | Verification: build |
| Assembly naming | the `name` of each existing `.asmdef`, and any naming rule in the conventions docs | Assemblies: naming convention |
| Project-wide defines | `-define:` lines in a `csc.rsp` under `Assets/`, `scriptingDefineSymbols` in `ProjectSettings/ProjectSettings.asset`, defines in Build Profile assets | Assemblies: project-wide defines |
| Third-party serializer | a serializer package or plugin folder (Odin: `Assets/Plugins/Sirenix/`, `using Sirenix.Serialization`), and a conventions doc covering it | Serialization: third-party serializer |

No editor version: the Unity skills read `ProjectVersion.txt`, or the package's `unity` field, when a version gate matters.

If `docs/agents/unity.md` already exists, read it too: its values, and every line in it that did not come from a seed field.

## 2. Confirm the facts in one list

Present every fact as one list, each with where it came from and whether it will be written. With an existing config, put its value beside the discovered one wherever they differ: the repo is current, so propose the discovered value. The user corrects the list in one reply; don't ask field by field.

## 3. Ask the policies as one question

The policies, each read from its owning skill so the question states the default that skill really uses:

- `unity-verification`: allowed environments, cold import ok, resident headless ok, heavy-run warning.
- `unity-assemblies`: layout policy.
- `unity-code-lifecycle`: reload does not matter, lifecycle API.

Ask exactly one question, listing each policy with its default:

> Keep the Unity defaults for these policies? (recommended: **yes**)

With an existing config, a policy it already sets is listed with that value instead of the default, and a **yes** keeps it.

On **yes**, write no policy beyond those. Only on **no**, ask which to change, then take each of those one at a time. A policy the user sets back to its default is not written.

## 4. Draft, confirm, write

Draft `docs/agents/unity.md` from the seed: keep its title and header, then each section that has at least one value, holding only the field lines with values. Drop the owner lines, the placeholder meanings and every empty field and section. Show the draft with the rest of setup's step 3, alongside the `### Unity` sub-block, and let the user edit before writing.

**Re-running**: update the file in place. Change only field lines whose value changed, add new ones, and remove ones now equal to the default. Every other line (a note, a field this procedure does not know, anything hand-written) stays where it is, word for word.

## 5. Done

Add to setup's closing message: the `unity` router and the Unity skills now read `docs/agents/unity.md`, it can be edited by hand, and re-running setup after a skills update picks up any new Unity fields.
