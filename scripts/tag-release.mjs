#!/usr/bin/env node
// Tags the current package.json version as `unity-v<version>` and pushes the tag.
// Runs as the changesets action's `publish` step, in place of `changeset tag`,
// whose `v<version>` tags would collide with upstream's `v1.x` release tags.
// Does nothing when the tag already exists.

import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const repo = join(dirname(fileURLToPath(import.meta.url)), "..");
const { version } = JSON.parse(readFileSync(join(repo, "package.json"), "utf8"));
const tag = `unity-v${version}`;

const git = (...args) =>
  execFileSync("git", args, { cwd: repo, encoding: "utf8" }).trim();

if (git("ls-remote", "--tags", "origin", `refs/tags/${tag}`)) {
  console.log(`${tag} already exists`);
  process.exit(0);
}

git("tag", tag);
git("push", "origin", tag);
console.log(`New tag: ${tag}`);
