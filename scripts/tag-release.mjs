#!/usr/bin/env node
// Tags the current package.json version as `unity-v<version>`, pushes the tag,
// and creates its GitHub Release with the version's CHANGELOG.md section as notes.
// Runs as the changesets action's `publish` step, in place of `changeset tag`,
// whose `v<version>` tags would collide with upstream's `v1.x` release tags.
// The action only creates releases for `New tag: <name>@<version>` lines, which
// `unity-v<version>` never matches, so the release is created here instead.
// Skips the tag or the release when it already exists.

import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const repo = join(dirname(fileURLToPath(import.meta.url)), "..");
const { version } = JSON.parse(readFileSync(join(repo, "package.json"), "utf8"));
const tag = `unity-v${version}`;

const run = (cmd, args, input) =>
  execFileSync(cmd, args, { cwd: repo, encoding: "utf8", input }).trim();
const git = (...args) => run("git", args);

if (git("ls-remote", "--tags", "origin", `refs/tags/${tag}`)) {
  console.log(`${tag} already exists`);
} else {
  git("tag", tag);
  git("push", "origin", tag);
  console.log(`New tag: ${tag}`);
}

try {
  run("gh", ["release", "view", tag]);
  console.log(`Release ${tag} already exists`);
} catch {
  const changelog = readFileSync(join(repo, "CHANGELOG.md"), "utf8");
  const section = changelog.split(/^## /m).find((s) => s.split(/\r?\n/, 1)[0].trim() === version);
  const notes = section ? section.slice(section.search(/\r?\n/)).trim() : "";
  run("gh", ["release", "create", tag, "--verify-tag", "--title", tag, "--notes-file", "-"], notes);
  console.log(`New release: ${tag}`);
}
