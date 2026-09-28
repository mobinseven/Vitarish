import { spawn } from "node:child_process";
import { cpSync, existsSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { dirname, isAbsolute, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { cspBrowserEnvironment } from "./csp-browser-environment.cjs";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptDirectory, "..");
const appDefinitions = JSON.parse(readFileSync(resolve(scriptDirectory, "csp-browser-apps.json"), "utf8"));
const distDir = appDefinitions.buildDistDir;
if (!/^\.[A-Za-z0-9-]+$/u.test(distDir)) throw new Error("The CSP browser build directory must be a repository-local hidden directory name.");

const apps = Object.entries(appDefinitions).filter(([name]) => name !== "buildDistDir").map(([name, definition]) => {
  const appRoot = resolve(webRoot, definition.directory);
  const appRelativePath = relative(webRoot, appRoot);
  if (!appRelativePath || appRelativePath === ".." || appRelativePath.startsWith(`..${sep}`) || isAbsolute(appRelativePath) || !existsSync(resolve(appRoot, "package.json"))) {
    throw new Error(`The CSP browser app path is invalid for ${name}.`);
  }
  const distPath = resolve(appRoot, distDir);
  const distRelativePath = relative(appRoot, distPath);
  if (!distRelativePath || distRelativePath === ".." || distRelativePath.startsWith(`..${sep}`) || isAbsolute(distRelativePath)) throw new Error(`The CSP browser build path is invalid for ${name}.`);
  return { name, ...definition, appRoot, distPath };
});
if (apps.length !== 6) throw new Error(`Expected six browser apps; found ${apps.length}.`);
for (const app of apps) if (existsSync(app.distPath)) throw new Error(`Refusing to replace an existing build output at ${app.distPath}.`);

const generatedPaths = apps.flatMap(({ appRoot }) => [resolve(appRoot, "tsconfig.json"), resolve(appRoot, "next-env.d.ts")]);
const generatedSnapshots = new Map(generatedPaths.map((path) => [path, existsSync(path) ? readFileSync(path) : null]));
const nextCli = resolve(webRoot, "node_modules", "next", "dist", "bin", "next");
const e2eRunner = resolve(scriptDirectory, "run-two-host-e2e.mjs");

function runNode(label, script, args = [], environment = process.env, cwd = webRoot) {
  return new Promise((resolveRun, rejectRun) => {
    const child = spawn(process.execPath, [script, ...args], { cwd, env: environment, stdio: "inherit", windowsHide: true });
    child.once("error", rejectRun);
    child.once("close", (code, signal) => {
      if (code === 0 && signal === null) resolveRun();
      else rejectRun(new Error(`${label} exited with code=${code}, signal=${signal}.`));
    });
  });
}

function restoreGeneratedFiles() {
  for (const [path, original] of generatedSnapshots) {
    if (original === null) {
      if (existsSync(path)) rmSync(path, { force: true });
      continue;
    }
    const current = existsSync(path) ? readFileSync(path) : null;
    if (current === null || !current.equals(original)) writeFileSync(path, original);
  }
}

try {
  for (const app of apps) {
    process.stdout.write(`Building ${app.workspace} with the isolated production output.\n`);
    await runNode(app.workspace, nextCli, ["build", app.appRoot], {
      ...process.env,
      [cspBrowserEnvironment.nextDistDir]: distDir,
      NEXT_PUBLIC_API_BASE_URL: "",
    }, app.appRoot);
    const standaloneAppDirectory = resolve(app.distPath, "standalone", app.directory);
    const standaloneServer = resolve(standaloneAppDirectory, "server.js");
    if (!existsSync(standaloneServer)) throw new Error(`The standalone server output is missing for ${app.workspace}.`);
    const staticDirectory = resolve(app.distPath, "static");
    if (existsSync(staticDirectory)) cpSync(staticDirectory, resolve(standaloneAppDirectory, distDir, "static"), { recursive: true, force: true });
    const publicDirectory = resolve(app.appRoot, "public");
    if (existsSync(publicDirectory)) cpSync(publicDirectory, resolve(standaloneAppDirectory, "public"), { recursive: true, force: true });
  }
  await runNode("CSP browser E2E", e2eRunner, [], {
    ...process.env,
    [cspBrowserEnvironment.nextDistDir]: distDir,
    NEXT_PUBLIC_API_BASE_URL: "",
    [cspBrowserEnvironment.browserMode]: cspBrowserEnvironment.browserModeEnabled,
  });
} finally {
  for (const app of apps) {
    if (existsSync(app.distPath)) rmSync(app.distPath, { recursive: true, force: true });
  }
  restoreGeneratedFiles();
}
