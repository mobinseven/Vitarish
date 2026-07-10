#!/usr/bin/env node
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { join, relative, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const webRoot = join(dirname(fileURLToPath(import.meta.url)), "..");
const appRoot = join(webRoot, "apps/private-practice-general");
const root = join(webRoot, "../../..");
const scopes = ["src/app", "src/components", "src/features", "src/design-system"];
const literalPattern = />\s*([A-Z][A-Za-z0-9 ,.'!?&()/-]{2,})\s*</g;
const attrPattern = /\b(placeholder|aria-label|title|label)="([A-Z][^"]{2,})"/g;
const findings = [];

function collect(dir) {
  const files = [];
  for (const entry of readdirSync(dir)) {
    const path = join(dir, entry);
    const stat = statSync(path);
    if (stat.isDirectory()) files.push(...collect(path));
    else if (path.endsWith(".tsx")) files.push(path);
  }
  return files;
}

for (const scope of scopes) {
  const dir = join(appRoot, scope);
  if (!existsSync(dir)) continue;
  for (const file of collect(dir)) {
    const normalizedFile = file.replaceAll("\\", "/");
    if (normalizedFile.includes("/api/generated/") || normalizedFile.includes("/__tests__/")) continue;
    const source = readFileSync(file, "utf8");
    for (const match of source.matchAll(literalPattern)) findings.push({ file, text: match[1].trim() });
    for (const match of source.matchAll(attrPattern)) findings.push({ file, text: `${match[1]}=\"${match[2]}\"` });
  }
}

if (findings.length === 0) {
  console.log("No likely hard-coded React UI strings found.");
  process.exit(0);
}

console.error(`Localization validation failed: ${findings.length} hard-coded React UI string(s) found.`);
for (const finding of findings.slice(0, 200)) console.error(`- ${relative(root, finding.file)}: ${finding.text}`);
if (findings.length > 200) console.error(`...and ${findings.length - 200} more.`);
console.error("Migrate all hard-coded strings to the owning app localization catalog.");
process.exit(1);
