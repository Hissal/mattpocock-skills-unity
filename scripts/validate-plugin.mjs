#!/usr/bin/env node
// Runs `claude plugin validate .` and fails on any error or warning, like
// `--strict`, except the warnings about CLAUDE.md and CLAUDE.local.md at the
// plugin root. That CLAUDE.md is contributor context for this repo, not plugin
// context: the plugin never meant to ship it, and the manifest has no field to
// exclude a file, so the warning is expected. Upstream's root shares the same
// file. CLAUDE.local.md is a contributor's gitignored local instructions file:
// it exists only on their machine, never in CI or the plugin, so its warning
// is expected too.

import { spawnSync } from "node:child_process";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const repo = join(dirname(fileURLToPath(import.meta.url)), "..");
const EXPECTED = /^CLAUDE(\.local)?\.md at the plugin root is not loaded as project context\./;

const run = spawnSync("claude plugin validate . --json", {
  cwd: repo,
  encoding: "utf8",
  shell: true,
});
let report;
try {
  report = JSON.parse(run.stdout);
} catch {
  console.error(run.stdout + run.stderr);
  console.error("Could not parse `claude plugin validate --json` output.");
  process.exit(1);
}

const problems = [];
for (const result of [report.manifest, ...(report.contents ?? [])]) {
  if (!result) continue;
  for (const e of result.errors) problems.push(`error: ${result.file}: ${e.path}: ${e.message}`);
  for (const w of result.warnings) {
    if (w.path === "root" && EXPECTED.test(w.message)) continue;
    problems.push(`warning: ${result.file}: ${w.path}: ${w.message}`);
  }
}

if (problems.length > 0 || !report.success) {
  console.error(problems.join("\n") || "Validation failed.");
  process.exit(1);
}
console.log("Plugin validation passed (strict, ignoring the root CLAUDE.md and CLAUDE.local.md warnings).");
