import { createHash } from "node:crypto";
import { existsSync, readdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { join, relative, resolve, sep } from "node:path";

/**
 * The two-host E2E serves each Web app from a prebuilt `next build` output. The output is reused while the
 * fingerprint of everything the build reads is unchanged, so only an app whose inputs changed is rebuilt.
 *
 * The fingerprint covers the app's sources, every shared workspace package (conservatively, because the apps
 * import them), the generated API clients (they are gitignored but compiled in), the lockfile, the build
 * environment and the Node version. Anything missing from it would serve a stale build, so it errs towards
 * rebuilding: test files are the only sources left out, because a build never reads them.
 */
const ignoredDirectories = new Set(["node_modules", "artifacts", "coverage", "test-results", "__tests__", "__fixtures__"]);
const ignoredFilePattern = /\.(test|spec)\.[cm]?[jt]sx?$/;
// Rewritten by `next build` itself, and restored afterwards.
const generatedFiles = new Set(["next-env.d.ts"]);
const metadataFile = ".prebuilt.json";

function isIgnoredDirectory(name) {
  return ignoredDirectories.has(name) || name.startsWith(".next");
}

function listSourceFiles(directory) {
  const files = [];
  const walk = (current) => {
    for (const entry of readdirSync(current, { withFileTypes: true }).sort((a, b) => a.name.localeCompare(b.name))) {
      const path = join(current, entry.name);
      if (entry.isDirectory()) {
        if (!isIgnoredDirectory(entry.name)) walk(path);
      } else if (entry.isFile() && !ignoredFilePattern.test(entry.name) && !generatedFiles.has(entry.name)) {
        files.push(path);
      }
    }
  };
  if (existsSync(directory)) walk(directory);
  return files;
}

function hashFiles(root, files, hash) {
  for (const file of files) {
    hash.update(relative(root, file).split(sep).join("/"));
    hash.update("\0");
    hash.update(readFileSync(file));
    hash.update("\0");
  }
}

/** Digest of the inputs every app shares; compute once per run. */
export function fingerprintSharedInputs(webRoot) {
  const hash = createHash("sha256");
  const files = [
    ...listSourceFiles(resolve(webRoot, "shared")),
    ...listSourceFiles(resolve(webRoot, "src")),
    ...["package.json", "package-lock.json", "scripts/api-proxy-rewrites.mjs"]
      .map((name) => resolve(webRoot, name))
      .filter(existsSync),
    // The licensed UI package is not in the lockfile, so its own manifest stands in for its version.
    ...[resolve(webRoot, "node_modules", "@heroui-pro", "react", "package.json")].filter(existsSync),
  ];
  hashFiles(webRoot, files, hash);
  return hash.digest("hex");
}

/**
 * Digest for one app: its sources, the shared digest, the build environment and the runtime that builds it.
 * `buildEnvironment` holds only values the build can read; per-run paths and secrets must be left out by the caller.
 */
export function fingerprintApp({ webRoot, appRoot, sharedFingerprint, buildEnvironment }) {
  const hash = createHash("sha256");
  hash.update(sharedFingerprint);
  hash.update("\0");
  hashFiles(webRoot, listSourceFiles(appRoot), hash);
  hash.update(process.version);
  hash.update("\0");
  for (const [key, value] of Object.entries(buildEnvironment).sort(([a], [b]) => a.localeCompare(b))) {
    hash.update(`${key}=${value}\0`);
  }
  return hash.digest("hex");
}

/** True when `distPath` holds a finished build made from exactly these inputs. */
export function isPrebuilt(distPath, fingerprint) {
  const metadataPath = join(distPath, metadataFile);
  if (!existsSync(join(distPath, "BUILD_ID")) || !existsSync(metadataPath)) return false;
  try {
    return JSON.parse(readFileSync(metadataPath, "utf8")).fingerprint === fingerprint;
  } catch {
    return false;
  }
}

/** Discards output built from other inputs, so a failed rebuild can never leave a mix that looks current. */
export function discardPrebuilt(distPath, appRoot) {
  if (!resolve(distPath).startsWith(resolve(appRoot) + sep)) {
    throw new Error("The prebuilt Next output must live inside its owning app.");
  }
  rmSync(distPath, { recursive: true, force: true });
}

export function markPrebuilt(distPath, fingerprint) {
  writeFileSync(join(distPath, metadataFile), JSON.stringify({ fingerprint, builtAt: new Date().toISOString() }));
}
