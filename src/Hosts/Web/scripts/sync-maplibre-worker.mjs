import { copyFileSync, existsSync, mkdirSync, readFileSync } from "node:fs";
import { createRequire } from "node:module";
import { dirname, join } from "node:path";

// MapLibre's worker module imports its shared chunk relatively and is located at runtime from a
// computed URL, so bundlers cannot emit it. Hosts that show maps serve both files themselves,
// which also keeps the map within a self-only script and worker policy.
const workerFiles = ["maplibre-gl-worker.mjs", "maplibre-gl-shared.mjs"];

/** The public path the worker is served from; versioned, so a MapLibre upgrade never serves stale files. */
export function mapLibreWorkerPublicPath(version) {
  return `/vendor/maplibre/${version}/maplibre-gl-worker.mjs`;
}

/** Copies MapLibre's worker files into `<appRoot>/public/vendor/maplibre/<version>/` when missing. */
export function syncMapLibreWorker(appRoot) {
  const require = createRequire(join(appRoot, "package.json"));
  const packageJsonPath = require.resolve("maplibre-gl/package.json");
  const { version } = JSON.parse(readFileSync(packageJsonPath, "utf8"));
  const source = join(dirname(packageJsonPath), "dist");
  const target = join(appRoot, "public", "vendor", "maplibre", version);
  mkdirSync(target, { recursive: true });
  for (const file of workerFiles) {
    if (!existsSync(join(target, file))) copyFileSync(join(source, file), join(target, file));
  }
  return version;
}
