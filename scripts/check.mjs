#!/usr/bin/env node
// Enforces the mechanical CLAUDE.md rules, then runs `check-plugin-version`
// and `validate-plugin`. Prints one line per violation, naming the file and
// the rule, and exits 1 on any. Skips `validate-plugin` when the `claude` CLI
// is not on PATH, so the rest still runs on a machine without it.

import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const repo = join(dirname(fileURLToPath(import.meta.url)), "..");
const PROMOTED = ["engineering", "productivity", "unity"];
const NEEDS_DOCS_PAGE = ["engineering", "productivity"];
const EM_DASH = String.fromCharCode(0x2014);

const violations = [];
const fail = (file, rule) => violations.push(`${file}: ${rule}`);

// Tracked files plus untracked ones not ignored, so a new skill is checked
// before it is staged. Files deleted from the working tree are dropped.
const ls = spawnSync("git", ["ls-files", "--cached", "--others", "--exclude-standard", "-z"], {
  cwd: repo,
  encoding: "utf8",
});
if (ls.status !== 0) {
  console.error(ls.stderr || "`git ls-files` failed.");
  process.exit(1);
}
const files = ls.stdout
  .split("\0")
  .filter((f) => f && existsSync(join(repo, f)));

const read = (file) => readFileSync(join(repo, file), "utf8");

for (const file of files) {
  const bytes = readFileSync(join(repo, file));
  if (bytes.includes(0)) continue; // binary
  const lines = bytes.toString("utf8").split("\n");
  lines.forEach((line, i) => {
    if (line.includes(EM_DASH)) fail(`${file}:${i + 1}`, "em-dash (U+2014); rewrite the sentence instead");
  });
}

for (const file of files) {
  const m = file.match(/^\.changeset\/([^/]+\.md)$/);
  if (m && m[1] !== "README.md" && !m[1].startsWith("unity-")) {
    fail(file, "changeset not named unity-*.md");
  }
}

const skills = files
  .map((f) => f.match(/^skills\/([^/]+)\/([^/]+)\/SKILL\.md$/))
  .filter(Boolean)
  .map(([, bucket, name]) => ({ bucket, name }));

const readme = read("README.md");
const pluginSkills = new Set(
  JSON.parse(read(".claude-plugin/plugin.json")).skills.map((s) => s.replace(/^\.\//, "").replace(/\/$/, "")),
);
const bucketReadmes = {};

for (const { bucket, name } of skills) {
  const path = `skills/${bucket}/${name}`;
  if (PROMOTED.includes(bucket)) {
    if (!readme.includes(`](./${path}/SKILL.md)`)) fail("README.md", `promoted skill ${path} not linked to its SKILL.md`);
    if (!pluginSkills.has(path)) fail(".claude-plugin/plugin.json", `promoted skill ${path} missing from skills`);
    const bucketReadme = `skills/${bucket}/README.md`;
    bucketReadmes[bucket] ??= existsSync(join(repo, bucketReadme)) ? read(bucketReadme) : "";
    if (!bucketReadmes[bucket].includes(`](./${name}/SKILL.md)`)) {
      fail(bucketReadme, `promoted skill ${name} not linked to its SKILL.md`);
    }
    if (NEEDS_DOCS_PAGE.includes(bucket) && !existsSync(join(repo, "docs", bucket, `${name}.md`))) {
      fail(`docs/${bucket}/${name}.md`, `missing docs page for ${path}`);
    }
  } else {
    if (readme.includes(`${path}/`) || readme.includes(`${path})`)) {
      fail("README.md", `non-promoted skill ${path} must not be listed`);
    }
    if (pluginSkills.has(path)) fail(".claude-plugin/plugin.json", `non-promoted skill ${path} must not be listed`);
  }
}

for (const v of violations) console.error(v);
let failed = violations.length > 0;
console.log(failed ? `\n${violations.length} rule violation(s).` : "Repo rules passed.");

const run = (cmd) => spawnSync(cmd, { cwd: repo, stdio: "inherit", shell: true }).status === 0;

if (!run("npm run check-plugin-version")) failed = true;

const hasClaude = spawnSync("claude --version", { cwd: repo, stdio: "ignore", shell: true }).status === 0;
if (!hasClaude) console.log("validate-plugin skipped: claude CLI not found, run locally");
else if (!run("npm run validate-plugin")) failed = true;

process.exit(failed ? 1 : 0);
