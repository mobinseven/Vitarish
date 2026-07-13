#!/usr/bin/env node
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { join, relative, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const webRoot = join(dirname(fileURLToPath(import.meta.url)), "..");
const srcDirs = [
  join(webRoot, "apps/private-practice-general/src"),
  join(webRoot, "apps/platform-admin/src"),
];

const nativeDateInputPattern = /<Input\s[^>]*type\s*=\s*["']date["']/i;
const nativeInputDatePattern = /<input\s[^>]*type\s*=\s*["']date["']/i;
const formDateFieldPattern = /function\s+FormDateField\s*\(/;

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

for (const srcDir of srcDirs) {
  if (!existsSync(srcDir)) continue;
  for (const file of collect(srcDir)) {
    const normalizedFile = file.replaceAll("\\", "/");
    if (normalizedFile.includes("/__tests__/") || normalizedFile.includes("/api/generated/")) continue;
    const source = readFileSync(file, "utf8");
    const relPath = relative(webRoot, file);

    if (nativeDateInputPattern.test(source)) {
      const match = source.match(nativeDateInputPattern);
      findings.push({ file: relPath, pattern: match[0].trim() });
    }
    if (nativeInputDatePattern.test(source)) {
      const match = source.match(nativeInputDatePattern);
      findings.push({ file: relPath, pattern: match[0].trim() });
    }
    if (formDateFieldPattern.test(source)) {
      findings.push({ file: relPath, pattern: "FormDateField definition" });
    }
  }
}

if (findings.length === 0) {
  console.log("No native date inputs found. Use LocalizedDateField for date-only fields.");
  process.exit(0);
}

console.error(`Date input validation failed: ${findings.length} violation(s) found.`);
for (const finding of findings) {
  console.error(`- ${finding.file}: ${finding.pattern}`);
}
console.error("Replace native date inputs with LocalizedDateField from @/components/forms/LocalizedDateField.");
process.exit(1);
