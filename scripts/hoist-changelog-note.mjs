#!/usr/bin/env node
// Moves CHANGELOG.md's note back up under the `# title` line.
// Runs as part of `npm run version`, after `changeset version`, which inserts
// each new version right after the title line and so pushes the note down to
// the end of the new version's section. Changesets writes a section as only
// `### ` headings and `- ` entries (with indented continuations), so any plain
// paragraph trailing the newest section is the displaced note.
// Does nothing when no note trails the newest section.

import { readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const repo = join(dirname(fileURLToPath(import.meta.url)), "..");
const changelogPath = join(repo, "CHANGELOG.md");

const source = readFileSync(changelogPath, "utf8");
const eol = source.includes("\r\n") ? "\r\n" : "\n";
const lines = source.split(/\r?\n/);

const headings = lines.flatMap((line, i) => (line.startsWith("## ") ? [i] : []));
if (headings.length < 2) {
  console.log("CHANGELOG.md note is in place (fewer than two versions)");
  process.exit(0);
}
const [newest, next] = headings;

// Walk back from the next version's heading over the trailing plain paragraphs.
let start = next;
for (let i = next - 1; i > newest; i--) {
  const line = lines[i];
  if (line === "") continue;
  if (line.startsWith("### ") || line.startsWith("- ") || /^\s/.test(line)) break;
  start = i;
}

if (start === next) {
  console.log("CHANGELOG.md note is in place");
  process.exit(0);
}

const note = lines.slice(start, next);
const rest = lines.slice(newest, start);
while (rest.at(-1) === "") rest.pop();

const updated = [
  ...lines.slice(0, newest),
  ...note,
  ...rest,
  "",
  ...lines.slice(next),
].join(eol);

writeFileSync(changelogPath, updated);
console.log(`CHANGELOG.md note moved back above ${lines[newest].slice(3)}`);
