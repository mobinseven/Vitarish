#!/usr/bin/env node
/**
 * Validates cross-app and public-import boundaries.
 *
 * Enforces:
 *   - apps may import @vitarish/web-ui, @vitarish/web-api-client, @vitarish/web-localization
 *   - apps must not import package internals (src/**)
 *   - apps must not import other apps' private files
 *   - app-to-app relative imports are forbidden
 *   - generated code remains package-internal
 */

import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const appsDir = resolve(__dirname, "../apps");
const migratedWorkflowFolders = ["account", "auth", "clinic", "journey", "sea"];

const apps = [];
for (const entry of readdirSync(appsDir)) {
  const fullPath = resolve(appsDir, entry);
  if (statSync(fullPath).isDirectory()) {
    apps.push({ name: entry, path: fullPath });
  }
}

let failed = false;

for (const app of apps) {
  const srcDir = resolve(app.path, "src");
  if (!existsSync(srcDir) || !statSync(srcDir).isDirectory()) continue;

  const featuresDir = resolve(srcDir, "features");
  if (existsSync(featuresDir)) {
    for (const feature of migratedWorkflowFolders) {
      if (existsSync(resolve(featuresDir, feature))) {
        console.error(`Migrated workflow folder in ${app.name}/src/features/${feature}: use @vitarish/${feature}-web`);
        failed = true;
      }
    }
  }

  function walk(dir, prefix = "") {
    for (const entry of readdirSync(dir)) {
      const fullPath = resolve(dir, entry);
      const relativePath = prefix ? `${prefix}/${entry}` : entry;
      if (statSync(fullPath).isDirectory()) {
        walk(fullPath, relativePath);
        continue;
      }
      if (!entry.endsWith(".ts") && !entry.endsWith(".tsx")) continue;

      const text = readFileSync(fullPath, "utf8");
      const normalizedPath = relativePath.replaceAll("\\", "/");
      const isClinicRoutePage =
        app.name === "private-practice-general" &&
        (/^app\/(owner|user)\/clinic\/.+\/page\.tsx$/.test(normalizedPath) ||
          normalizedPath === "app/owner/clinic/page.tsx");

      if (isClinicRoutePage) {
        const forbidden = [
          [/from\s+["']@heroui(?:-pro)?\/react["']/, "HeroUI import"],
          [/\buse(?:State|Effect)\b/, "workflow state hook"],
          [/\buseLocalizedText\b/, "localization hook"],
          [/\buse(?:Owner|Patient|Service|ClinicOwnerReminder|Bookable|Agenda)[A-Z]\w*\b/, "clinic workflow hook"],
          [/api\/generated|\b(?:Patient|Shift|Reservation|Service)(?:Read|View|Write)?Dto\b/, "clinic/generated DTO"],
        ];
        for (const [pattern, label] of forbidden) {
          if (pattern.test(text)) {
            console.error(`Clinic route ${app.name}/${relativePath} contains forbidden ${label}`);
            failed = true;
          }
        }
        const imports = [...text.matchAll(/import\s*\{([^}]+)\}\s*from\s*["']@vitarish\/clinic-web["']/g)]
          .flatMap((match) => match[1].split(",").map((name) => name.trim()).filter(Boolean));
        for (const imported of imports) {
          if (!/^Clinic[A-Z]\w+Page$/.test(imported)) {
            console.error(`Clinic route ${app.name}/${relativePath} imports non-page clinic surface ${imported}`);
            failed = true;
          }
        }
      }

      // Block deep imports into any @vitarish package
      const packageDeepImport = text.match(/from\s+["']@vitarish\/([^"'/]+)\/src\//);
      if (packageDeepImport) {
        console.error(`Deep import into @vitarish/${packageDeepImport[1]}/src in ${app.name}/${relativePath}: use public exports only`);
        failed = true;
      }

      // Block imports from other apps
      for (const otherApp of apps) {
        if (otherApp.name === app.name) continue;
        const importPattern = new RegExp(`from\\s+["'].*\\.\\.\\/${otherApp.name}\\/`, "g");
        if (importPattern.test(text)) {
          console.error(`Cross-app import in ${app.name}/${relativePath}: must not import from ${otherApp.name}`);
          failed = true;
        }
        const directImportPattern = new RegExp(`from\\s+["'].*\\/${otherApp.name}\\/src\\/`, "g");
        if (directImportPattern.test(text)) {
          console.error(`Direct app import in ${app.name}/${relativePath}: must not import from ${otherApp.name}/src/`);
          failed = true;
        }
      }
    }
  }

  walk(srcDir);
}

if (failed) {
  console.error("\nApp boundary validation failed.");
  process.exit(1);
} else {
  console.log("App boundary validation passed.");
}
