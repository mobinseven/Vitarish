#!/usr/bin/env node

import { readdirSync, readFileSync, statSync } from "node:fs";
import path from "node:path";

const root = process.cwd();
const sourceRoot = path.join(root, "src");
const strict = process.argv.includes("--strict");
const extensions = new Set([".ts", ".tsx"]);
const ignoredSegments = [
  `${path.sep}api${path.sep}generated${path.sep}`,
  `${path.sep}i18n${path.sep}`,
  `${path.sep}.next${path.sep}`,
];

const jsxTextPattern = />\s*([A-Z][A-Za-z0-9 ,.!?:;'"()/-]{2,})\s*</g;
const userFacingPropPattern =
  /\b(title|label|message|placeholder|description|aria-label|children)=["']([A-Z][^"']{2,})["']/g;
const statusMessagePattern =
  /\b(StatusMessage|toast|notification|Snackbar|Alert)[\s\S]*?["']([A-Z][^"']{2,})["']/g;

const findings = [];

for (const file of walk(sourceRoot)) {
  const content = readFileSync(file, "utf8");
  scan(file, content, jsxTextPattern, "JSX text");
  scan(file, content, userFacingPropPattern, "user-facing prop", 2);
  scan(file, content, statusMessagePattern, "status or notification text", 2);
}

if (findings.length === 0) {
  console.log("No candidate hard-coded React UI strings found.");
  process.exit(0);
}

console.log("Candidate hard-coded React UI strings:");
console.log("");

for (const finding of findings) {
  console.log(
    `- ${finding.relativePath}:${finding.line} [${finding.kind}] ${JSON.stringify(
      finding.text,
    )}`,
  );
}

console.log("");
console.log(
  strict
    ? "Strict mode failed. Move user-facing strings to src/i18n/messages.ts and use useLocalizer().t(...)."
    : "Report mode only. Re-run with --strict to fail on candidates after existing pages are migrated.",
);

process.exit(strict ? 1 : 0);

function* walk(directory) {
  for (const entry of readdirSync(directory)) {
    const fullPath = path.join(directory, entry);
    const stats = statSync(fullPath);

    if (stats.isDirectory()) {
      if (shouldIgnore(fullPath)) {
        continue;
      }

      yield* walk(fullPath);
      continue;
    }

    if (!stats.isFile() || !extensions.has(path.extname(fullPath))) {
      continue;
    }

    if (shouldIgnore(fullPath)) {
      continue;
    }

    yield fullPath;
  }
}

function shouldIgnore(fullPath) {
  return ignoredSegments.some((segment) => fullPath.includes(segment));
}

function scan(file, content, pattern, kind, captureGroup = 1) {
  for (const match of content.matchAll(pattern)) {
    const text = normalizeCandidate(match[captureGroup]);

    if (!text || isTechnicalLiteral(text)) {
      continue;
    }

    const before = content.slice(0, match.index ?? 0);
    const line = before.split("\n").length;
    const lineText = content.split("\n")[line - 1] ?? "";

    if (lineText.includes("t(") || lineText.includes("translate(")) {
      continue;
    }

    findings.push({
      relativePath: path.relative(root, file),
      line,
      kind,
      text,
    });
  }
}

function normalizeCandidate(value) {
  return value?.replace(/\s+/g, " ").trim();
}

function isTechnicalLiteral(value) {
  return (
    value.length < 3 ||
    value.includes("http") ||
    value.includes("/") ||
    value.includes("@") ||
    /^[A-Z0-9_]+$/.test(value) ||
    /^fa-[a-z-]+\s/.test(value) ||
    /^ds-/.test(value)
  );
}
