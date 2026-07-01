#!/usr/bin/env node
import { readdirSync, readFileSync, statSync } from "node:fs";
import { join, relative } from "node:path";

const webRoot = process.cwd();
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
  const dir = join(webRoot, scope);
  for (const file of collect(dir)) {
    if (file.includes("/api/generated/") || file.includes("/__tests__/")) continue;
    const source = readFileSync(file, "utf8");
    for (const match of source.matchAll(literalPattern)) findings.push({ file, text: match[1].trim() });
    for (const match of source.matchAll(attrPattern)) findings.push({ file, text: `${match[1]}=\"${match[2]}\"` });
  }
}

if (findings.length === 0) {
  console.log("No likely hard-coded React UI strings found.");
  process.exit(0);
}

console.warn(`Localization validation report: ${findings.length} likely hard-coded React UI string(s) remain.`);
for (const finding of findings.slice(0, 200)) console.warn(`- ${relative(root, finding.file)}: ${finding.text}`);
if (findings.length > 200) console.warn(`...and ${findings.length - 200} more.`);
console.warn("Report mode only: migrate findings to src/Hosts/Web/src/localization before enabling CI failure mode.");
