#!/usr/bin/env node
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { join, relative, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const webRoot = join(dirname(fileURLToPath(import.meta.url)), "..");
const appRoot = join(webRoot, "apps/private-practice-owner");
const sharedRoot = join(webRoot, "shared");
const root = join(webRoot, "../../..");
const scopes = ["src/app", "src/components", "src/features", "src/design-system"];
const literalPattern = />\s*([A-Z][A-Za-z0-9 ,.'!?&()/-]{2,})\s*<\//g;
const attrPattern = /\b(placeholder|aria-label|title|label)="([A-Z][^"]{2,})"/g;
const findings = [];
const invalidClinicKeyUsages = [];
const clinicErrorLeaks = [];

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

function validateFile(file) {
  const normalizedFile = file.replaceAll("\\", "/");
  if (normalizedFile.includes("/api/generated/") || normalizedFile.includes("/__tests__/") || normalizedFile.includes("/node_modules/")) return;
  const source = readFileSync(file, "utf8");
  for (const match of source.matchAll(literalPattern)) findings.push({ file, text: match[1].trim() });
  for (const match of source.matchAll(attrPattern)) findings.push({ file, text: match[1] + '="' + match[2] + '"' });
  if (normalizedFile.includes("/shared/clinic-web/src/")) {
    for (const match of source.matchAll(/\bt\(["'](clinic\.[^"']+)["']/g)) {
      if (["clinic.procedures.events"].includes(match[1])) invalidClinicKeyUsages.push({ file, text: match[1] });
    }
    if (/\berror\.message\b/.test(source)) clinicErrorLeaks.push(file);
  }
}

for (const scope of scopes) {
  const dir = join(appRoot, scope);
  if (!existsSync(dir)) continue;
  for (const file of collect(dir)) validateFile(file);
}

if (existsSync(sharedRoot)) {
  for (const packageName of readdirSync(sharedRoot)) {
    const src = join(sharedRoot, packageName, "src");
    if (!existsSync(src) || !statSync(src).isDirectory()) continue;
    for (const file of collect(src)) validateFile(file);
  }
}

if (findings.length === 0 && invalidClinicKeyUsages.length === 0 && clinicErrorLeaks.length === 0) {
  console.log("No likely hard-coded React UI strings found.");
  process.exit(0);
}

console.error("Localization validation failed.");
for (const finding of findings.slice(0, 200)) console.error("- " + relative(root, finding.file) + ": " + finding.text);
for (const finding of invalidClinicKeyUsages) console.error("- " + relative(root, finding.file) + ': object namespace key "' + finding.text + '"');
for (const file of clinicErrorLeaks) console.error("- " + relative(root, file) + ": renders or consumes error.message directly");
if (findings.length > 200) console.error("...and " + (findings.length - 200) + " more.");
console.error("Migrate hard-coded strings to the owning localization catalog.");
process.exit(1);
