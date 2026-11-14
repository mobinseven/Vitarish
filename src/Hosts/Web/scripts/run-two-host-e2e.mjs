import { spawn } from "node:child_process";
import { createWriteStream, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { dirname, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import http from "node:http";
import https from "node:https";
import net from "node:net";
import { createTwoHostWebRouter } from "./two-host-web-router.mjs";
import crypto from "node:crypto";
import { signInUrlEnvironmentKey } from "../shared/journey/src/signInHost.ts";
import { apiProxyTargetEnvironmentKey, browserApiBaseEnvironmentKey } from "./api-proxy-rewrites.mjs";
import {
  twoHostE2eApiOriginEnvironmentKey,
  twoHostE2eArtifactDirectoryEnvironmentKey,
  twoHostE2eClinicOrigin,
  twoHostE2eGrepEnvironmentKey,
  twoHostE2eFileTransferAuditKeyEnvironmentKey,
  twoHostE2eNextDistDirectoryEnvironmentKey,
  twoHostE2ePlatformAdminOrigin,
  twoHostE2ePatientsOrigin,
  twoHostE2ePatientsHostConfigurationKey,
  twoHostE2eProjectManagementHostConfigurationKey,
  twoHostE2eKnowledgeHostConfigurationKey,
  twoHostE2eProjectManagementOrigin,
  twoHostE2eKnowledgeOrigin,
  twoHostE2eProofScreenshotEnvironmentKey,
  twoHostE2eProofStorageStateEnvironmentKey,
  twoHostE2eSignInOrigin,
  twoHostE2eLandingOrigin,
  twoHostE2eOwnerListenerOrigin,
  twoHostE2eLandingListenerOrigin,
  twoHostE2eSmsCapturePathEnvironmentKey,
  twoHostE2eOwnerUsernameEnvironmentKey,
} from "./two-host-e2e-artifacts.mjs";
import {
  discardPrebuilt,
  fingerprintApp,
  fingerprintSharedInputs,
  isPrebuilt,
  markPrebuilt,
} from "./two-host-e2e-web-build-cache.mjs";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptDirectory, "..");
const repositoryRoot = resolve(webRoot, "..", "..", "..");
const appRoot = resolve(webRoot, "apps", "private-practice-owner");
const platformAdminAppRoot = resolve(webRoot, "apps", "platform-admin");
const e2eProject = resolve(repositoryRoot, "src", "Hosts", "E2e", "E2e.csproj");
const nextCli = resolve(webRoot, "node_modules", "next", "dist", "bin", "next");
const playwrightConfig = resolve(webRoot, "playwright.two-host.config.ts");
const artifactRoot = resolve(webRoot, "artifacts", "two-host-e2e");
const runId = `${new Date().toISOString().replaceAll(":", "-")}-${process.pid}-${crypto.randomBytes(4).toString("hex")}`;
const artifactDirectory = resolve(artifactRoot, "runs", runId);
const proofStorageStatePath = resolve(artifactDirectory, "authenticated-user-security-state.json");
const appTsconfigPath = resolve(appRoot, "tsconfig.json");
const appNextEnvPath = resolve(appRoot, "next-env.d.ts");
// A stable folder, not a per-run one: the build is reused until its inputs change (see the build cache module).
const appNextDistDirectory = ".next-two-host-e2e-prebuilt";
const appNextDistPath = resolve(appRoot, appNextDistDirectory);
const platformAdminTsconfigPath = resolve(platformAdminAppRoot, "tsconfig.json");
const platformAdminNextEnvPath = resolve(platformAdminAppRoot, "next-env.d.ts");
const platformAdminNextDistPath = resolve(platformAdminAppRoot, appNextDistDirectory);
const originalAppTsconfig = readFileSync(appTsconfigPath, "utf8");
const originalAppNextEnv = readFileSync(appNextEnvPath, "utf8");
const originalPlatformAdminTsconfig = readFileSync(platformAdminTsconfigPath, "utf8");
const originalPlatformAdminNextEnv = readFileSync(platformAdminNextEnvPath, "utf8");
const additionalApps = [
  { name: "landing", origin: twoHostE2eLandingListenerOrigin, folder: "vitarish-landing" },
  { name: "patients", origin: twoHostE2ePatientsOrigin, folder: "patients" },
  { name: "project-management", origin: twoHostE2eProjectManagementOrigin, folder: "vitarish-pm" },
].map(app => {
  const root = resolve(webRoot, "apps", app.folder);
  const tsconfig = resolve(root, "tsconfig.json");
  const nextEnv = resolve(root, "next-env.d.ts");
  return { ...app, root, tsconfig, nextEnv, originalTsconfig: readFileSync(tsconfig, "utf8"),
    originalNextEnv: readFileSync(nextEnv, "utf8"), nextDist: resolve(root, appNextDistDirectory) };
});
const mainOrigin = "https://localhost:7272";
const webOrigin = twoHostE2eClinicOrigin;
const platformAdminOrigin = twoHostE2ePlatformAdminOrigin;
const smtpCapturePath = resolve(artifactDirectory, "e2e-smtp-capture.eml");
const smsCapturePath = resolve(artifactDirectory, "e2e-sms-capture.jsonl");
const smtpPort = 2525;
const readinessProbeTimeoutMs = 5000;
const nextReadinessTimeoutMs = 300_000;
// Spec files and flags such as --headed or --ui pass through to Playwright.
const playwrightArguments = process.argv.slice(2);

mkdirSync(resolve(artifactRoot, "runs"), { recursive: true });
mkdirSync(artifactDirectory);

const processes = [];
let stopping = false;
let smtpServer;
let browserRouter;

function startSmtpCapture() {
  smtpServer = net.createServer((socket) => {
    socket.setEncoding("utf8");
    socket.write("220 e2e-smtp.localhost E2E SMTP\r\n");
    let input = "";
    let dataMode = false;
    let authMode = "";
    const message = [];

    const reply = (value) => socket.write(`${value}\r\n`);
    const processLine = (line) => {
      if (dataMode) {
        if (line === ".") {
          writeFileSync(smtpCapturePath, `${message.join("\r\n")}\r\n`, "utf8");
          dataMode = false;
          reply("250 2.0.0 queued");
          return;
        }
        message.push(line.startsWith("..") ? line.slice(1) : line);
        return;
      }

      if (authMode === "username") {
        authMode = "password";
        reply("334 UGFzc3dvcmQ6");
        return;
      }
      if (authMode === "password") {
        authMode = "";
        reply("235 2.7.0 authenticated");
        return;
      }

      const command = line.split(" ", 1)[0].toUpperCase();
      switch (command) {
        case "EHLO":
        case "HELO":
          reply("250-e2e-smtp.localhost");
          reply("250 AUTH PLAIN LOGIN");
          break;
        case "AUTH":
          if (line.toUpperCase().startsWith("AUTH LOGIN")) {
            authMode = "username";
            reply("334 VXNlcm5hbWU6");
          } else {
            reply("235 2.7.0 authenticated");
          }
          break;
        case "MAIL":
        case "RCPT":
          reply("250 2.1.0 accepted");
          break;
        case "DATA":
          dataMode = true;
          reply("354 End data with <CR><LF>.<CR><LF>");
          break;
        case "RSET":
          reply("250 2.0.0 reset");
          break;
        case "QUIT":
          reply("221 2.0.0 closing");
          socket.end();
          break;
        default:
          reply("250 2.0.0 accepted");
          break;
      }
    };

    socket.on("data", (chunk) => {
      input += chunk;
      let lineEnd;
      while ((lineEnd = input.indexOf("\r\n")) >= 0) {
        const line = input.slice(0, lineEnd);
        input = input.slice(lineEnd + 2);
        processLine(line);
      }
    });
  });

  return new Promise((resolveSmtp, rejectSmtp) => {
    smtpServer.once("error", rejectSmtp);
    smtpServer.listen(smtpPort, "127.0.0.1", () => resolveSmtp());
  });
}

function stopSmtpCapture() {
  if (!smtpServer) return Promise.resolve();
  return new Promise((resolveSmtp) => smtpServer.close(() => resolveSmtp()));
}

function startProcess(label, command, args, cwd, environment) {
  const logPath = resolve(artifactDirectory, `${label}.log`);
  const log = createWriteStream(logPath, { flags: "wx" });
  const child = spawn(command, args, {
    cwd,
    env: environment,
    stdio: ["ignore", "pipe", "pipe"],
    windowsHide: true,
    detached: process.platform !== "win32",
  });

  child.stdout.on("data", (data) => {
    log.write(data);
    process.stdout.write(`[${label}] ${data}`);
  });
  child.stderr.on("data", (data) => {
    log.write(data);
    process.stderr.write(`[${label}] ${data}`);
  });
  child.on("close", () => log.end());
  child.logPath = logPath;
  child.label = label;
  processes.push(child);
  return child;
}

function probe(url) {
  return new Promise((resolveProbe) => {
    const client = url.startsWith("https:") ? https : http;
    const requested = new URL(url);
    const connect = new URL(url);
    if (connect.hostname.endsWith(".localhost")) connect.hostname = "localhost";
    const request = client.get(connect, { rejectUnauthorized: false, headers: { Host: requested.host } }, (response) => {
      response.resume();
      resolveProbe(response.statusCode ?? 0);
    });
    request.setTimeout(readinessProbeTimeoutMs, () => request.destroy());
    request.on("error", () => resolveProbe(false));
  });
}

async function waitForHttp(label, child, url, timeoutMs = 120000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (child.exitCode !== null || child.signalCode !== null) {
      throw new Error(`${label} exited before accepting HTTP connections (exit=${child.exitCode}, signal=${child.signalCode}). See ${child.logPath}.`);
    }
    if ((await probe(url)) === 200) return;
    await new Promise((resolveDelay) => setTimeout(resolveDelay, 250));
  }
  throw new Error(`${label} did not accept HTTP connections at ${url} within ${timeoutMs}ms. See ${child.logPath}.`);
}

async function ensurePortAvailable(label, url) {
  if ((await probe(url)) > 0) {
    throw new Error(`${label} fixed port is already in use at ${url}. Stop the existing process before running the two-host suite.`);
  }
}

function waitForExit(child) {
  return new Promise((resolveExit, rejectExit) => {
    if (child.exitCode !== null || child.signalCode !== null) {
      resolveExit({ code: child.exitCode, signal: child.signalCode });
      return;
    }
    child.once("error", rejectExit);
    child.once("close", (code, signal) => resolveExit({ code, signal }));
  });
}

// `computeFingerprint` is called again after a build: the app's own build steps copy vendor files and fonts into
// its `public` folder, so the inputs later runs will see are those left behind by the build.
async function ensureNextBuild(label, cwd, distPath, environment, computeFingerprint, restoreGeneratedFiles) {
  const fingerprint = computeFingerprint();
  if (isPrebuilt(distPath, fingerprint)) {
    console.log(`[${label}-build] reusing the prebuilt output (inputs ${fingerprint.slice(0, 12)} unchanged).`);
    return;
  }
  // Output from other inputs must not survive a failed rebuild looking current.
  discardPrebuilt(distPath, cwd);
  const child = startProcess(`${label}-build`, process.execPath, [nextCli, "build"], cwd, environment);
  let result;
  try {
    result = await waitForExit(child);
  } catch (error) {
    throw new Error(`${label} build could not start: ${error instanceof Error ? error.message : error}. See ${child.logPath}.`);
  }
  if (result.code !== 0 || result.signal !== null) {
    throw new Error(`${label} build failed (exit=${result.code}, signal=${result.signal}). See ${child.logPath}.`);
  }
  // `next build` rewrites tsconfig.json and next-env.d.ts; the fingerprint must see them as every later run will.
  restoreGeneratedFiles();
  markPrebuilt(distPath, computeFingerprint());
}

function waitForPrematureHostExit(child) {
  return new Promise((_, reject) => {
    child.once("close", (code, signal) => {
      if (!stopping) {
        reject(new Error(`${child.label} exited while the E2E test was running (exit=${code}, signal=${signal}). See ${child.logPath}.`));
      }
    });
  });
}

async function stopProcessTree(child) {
  if (!child || !child.pid || child.exitCode !== null || child.signalCode !== null) return;
  if (process.platform === "win32") {
    await new Promise((resolveKill) => {
      const killer = spawn("taskkill", ["/PID", String(child.pid), "/T", "/F"], { windowsHide: true });
      killer.once("close", resolveKill);
      killer.once("error", resolveKill);
    });
  } else {
    try { process.kill(-child.pid, "SIGTERM"); } catch { child.kill("SIGTERM"); }
  }
  await Promise.race([
    waitForExit(child),
    new Promise((resolveTimeout) => setTimeout(resolveTimeout, 5000)),
  ]);
}

async function stopAll() {
  stopping = true;
  if (browserRouter?.listening) {
    browserRouter.closeAllConnections();
    await new Promise(resolveClose => browserRouter.close(resolveClose));
  }
  await Promise.all(processes.slice().reverse().map(stopProcessTree));
}

function restoreNextTsconfig(tsconfigPath, originalTsconfig) {
  const currentTsconfig = readFileSync(tsconfigPath, "utf8");
  const generatedTypesGlob = `${appNextDistDirectory}/types/**/*.ts`;
  if (!originalTsconfig.includes(generatedTypesGlob) && currentTsconfig.includes(generatedTypesGlob)) {
    writeFileSync(tsconfigPath, originalTsconfig);
  }
}

// Only the tracked files `next build` rewrites are restored; the prebuilt output itself is kept for the next run.
function restoreNextGeneratedFiles(nextEnvPath, originalNextEnv) {
  const currentNextEnv = readFileSync(nextEnvPath, "utf8");
  const generatedRoutesReference = `${appNextDistDirectory}/types/routes.d.ts`;
  if (!originalNextEnv.includes(generatedRoutesReference) && currentNextEnv.includes(generatedRoutesReference)) {
    writeFileSync(nextEnvPath, originalNextEnv);
  }
}

function restoreBuildRewrites(tsconfigPath, originalTsconfig, nextEnvPath, originalNextEnv) {
  restoreNextTsconfig(tsconfigPath, originalTsconfig);
  restoreNextGeneratedFiles(nextEnvPath, originalNextEnv);
}

function createEnvironment(overrides) {
  return { ...process.env, ...overrides };
}

function withSystemCa(nodeOptions = "") {
  return /(^|\s)--use-system-ca(\s|$)/.test(nodeOptions) ? nodeOptions : `${nodeOptions} --use-system-ca`.trim();
}

async function run() {
  const configuredProofScreenshotPath = process.env[twoHostE2eProofScreenshotEnvironmentKey];
  const proofScreenshotPath = configuredProofScreenshotPath
    ? resolve(configuredProofScreenshotPath)
    : resolve(artifactDirectory, "authenticated-user-security-proof.png");
  if (existsSync(proofScreenshotPath)) {
    throw new Error(`Refusing to overwrite existing E2E proof screenshot: ${proofScreenshotPath}`);
  }
  mkdirSync(dirname(proofScreenshotPath), { recursive: true });

  const password = `${crypto.randomBytes(24).toString("base64url")}!Aa1`;
  const agentSharedSecret = crypto.randomBytes(32).toString("base64url");
  const fileTransferAuditKey = crypto.randomBytes(32).toString("base64url");
  await startSmtpCapture();
  const mainEnvironment = createEnvironment({
    ASPNETCORE_ENVIRONMENT: "E2E",
    ASPNETCORE_URLS: mainOrigin,
    [twoHostE2ePatientsHostConfigurationKey]: twoHostE2ePatientsOrigin,
    [twoHostE2eProjectManagementHostConfigurationKey]: twoHostE2eProjectManagementOrigin,
    [twoHostE2eKnowledgeHostConfigurationKey]: twoHostE2eKnowledgeOrigin,
    // `dotnet run` builds first; reused MSBuild workers would otherwise stay
    // resident (one per core) and compete with the Web servers for memory.
    MSBUILDDISABLENODEREUSE: "1",
    WebAuthn__ServerDomain: new URL(twoHostE2eSignInOrigin).hostname,
    WebAuthn__Origins__0: webOrigin,
    WebAuthn__Origins__1: platformAdminOrigin,
    WebAuthn__Origins__2: twoHostE2eSignInOrigin,
    [twoHostE2eApiOriginEnvironmentKey]: mainOrigin,
    VITARISH_E2E_WEB_ORIGIN: webOrigin,
    VITARISH_E2E_PLATFORM_ADMIN_ORIGIN: platformAdminOrigin,
    VITARISH_E2E_PASSWORD: password,
    [twoHostE2eFileTransferAuditKeyEnvironmentKey]: fileTransferAuditKey,
    Vitarish__Intelligence__AgentRuntime__SharedSecret: agentSharedSecret,
    Vitarish__Communications__Providers__Smtp__Host: "127.0.0.1",
    Vitarish__Communications__Providers__Smtp__Port: String(smtpPort),
    Vitarish__Communications__Providers__Smtp__EnableSsl: "false",
    Vitarish__Communications__Providers__Smtp__Name: "E2E",
    Vitarish__Communications__Providers__Smtp__Username: "e2e",
    Vitarish__Communications__Providers__Smtp__Password: "e2e",
    VITARISH_E2E_SMTP_CAPTURE_PATH: smtpCapturePath,
    [twoHostE2eSmsCapturePathEnvironmentKey]: smsCapturePath,
    [twoHostE2eArtifactDirectoryEnvironmentKey]: artifactDirectory,
    [twoHostE2eProofScreenshotEnvironmentKey]: proofScreenshotPath,
  });
  // The browser calls /api on its own web origin, as in production; Next proxies it to
  // Main. Node trusts Main's local development certificate through the system store.
  const webEnvironment = createEnvironment({
    [browserApiBaseEnvironmentKey]: "",
    NEXT_PUBLIC_VITARISH_MOCK_API: "0",
    NEXT_PUBLIC_VITARISH_MOCK_SCENARIO: "",
    [apiProxyTargetEnvironmentKey]: mainOrigin,
    [twoHostE2eApiOriginEnvironmentKey]: mainOrigin,
    NODE_USE_SYSTEM_CA: "1",
    NODE_OPTIONS: withSystemCa(process.env.NODE_OPTIONS),
    [twoHostE2eNextDistDirectoryEnvironmentKey]: appNextDistDirectory,
    [signInUrlEnvironmentKey]: twoHostE2eSignInOrigin,
    [twoHostE2eOwnerUsernameEnvironmentKey]: "e2e-owner",
    VITARISH_E2E_EMAIL_CHANGE_USERNAME: "e2e-email-change-user",
    VITARISH_E2E_PASSWORD: password,
    [twoHostE2eFileTransferAuditKeyEnvironmentKey]: fileTransferAuditKey,
    VITARISH_E2E_AGENT_SHARED_SECRET: agentSharedSecret,
    VITARISH_E2E_SMTP_CAPTURE_PATH: smtpCapturePath,
    [twoHostE2eSmsCapturePathEnvironmentKey]: smsCapturePath,
    [twoHostE2eArtifactDirectoryEnvironmentKey]: artifactDirectory,
    [twoHostE2eProofStorageStateEnvironmentKey]: proofStorageStatePath,
    [twoHostE2eProofScreenshotEnvironmentKey]: proofScreenshotPath,
  });
  const nextEnvironment = { ...webEnvironment, NODE_ENV: "production" };
  const buildEnvironment = { ...nextEnvironment };
  // The build sees nothing that changes per run (secrets, capture paths, the artifact folder), so its output
  // can be reused by any later run; those values reach the servers only when they start.
  for (const key of [
    "VITARISH_E2E_PASSWORD",
    "VITARISH_E2E_AGENT_SHARED_SECRET",
    "VITARISH_E2E_SMTP_CAPTURE_PATH",
    twoHostE2eFileTransferAuditKeyEnvironmentKey,
    twoHostE2eSmsCapturePathEnvironmentKey,
    twoHostE2eArtifactDirectoryEnvironmentKey,
    twoHostE2eProofStorageStateEnvironmentKey,
    twoHostE2eProofScreenshotEnvironmentKey,
  ]) {
    delete buildEnvironment[key];
  }
  // Fingerprint only what is set for this runner or steers Next, not the developer's whole shell.
  const buildInputs = Object.fromEntries(
    Object.entries(buildEnvironment).filter(([key]) => key.startsWith("NEXT_") || key.startsWith("VITARISH_") || key === "NODE_ENV" || key.startsWith("Vitarish__") || key.startsWith("WebAuthn__")),
  );
  const appFingerprint = (appRootPath) => () => fingerprintApp({
    webRoot,
    appRoot: appRootPath,
    sharedFingerprint: fingerprintSharedInputs(webRoot),
    buildEnvironment: buildInputs,
  });

  const collection = startProcess("playwright-collection", process.execPath,
    [resolve(webRoot, "node_modules", "@playwright", "test", "cli.js"), "test", "--config", playwrightConfig, "--list", ...playwrightArguments],
    webRoot, webEnvironment);
  const collectionResult = await waitForExit(collection);
  if (collectionResult.code !== 0 || collectionResult.signal !== null)
    throw new Error(`Playwright collection failed. See ${collection.logPath}.`);

  await ensurePortAvailable("E2e", mainOrigin);
  await ensurePortAvailable("Private Practice Owner", webOrigin);
  await ensurePortAvailable("Private Practice Owner listener", twoHostE2eOwnerListenerOrigin);
  await ensurePortAvailable("Platform Admin", platformAdminOrigin);
  for (const app of additionalApps) await ensurePortAvailable(app.name, app.origin);

  // dotnet run's implicit build can start one worker per core; cap the explicit build instead.
  const e2eBuild = startProcess("e2e-build", "dotnet", ["build", e2eProject, "-m:4"], repositoryRoot, mainEnvironment);
  const e2eBuildResult = await waitForExit(e2eBuild);
  if (e2eBuildResult.code !== 0 || e2eBuildResult.signal !== null) {
    throw new Error(`E2e build failed (exit=${e2eBuildResult.code}, signal=${e2eBuildResult.signal}). See ${e2eBuild.logPath}.`);
  }

  const main = startProcess(
    "e2e",
    "dotnet",
    ["run", "--project", e2eProject, "--no-build", "--no-launch-profile", "--urls", mainOrigin],
    repositoryRoot,
    mainEnvironment,
  );
  // Fail on backend composition/fixture errors before spending time building every Web app.
  await waitForHttp("E2e", main, `${mainOrigin}/api/modules/authentication/antiforgery`);

  await ensureNextBuild("web", appRoot, appNextDistPath, buildEnvironment, appFingerprint(appRoot),
    () => restoreBuildRewrites(appTsconfigPath, originalAppTsconfig, appNextEnvPath, originalAppNextEnv));
  await ensureNextBuild("platform-admin", platformAdminAppRoot, platformAdminNextDistPath, buildEnvironment, appFingerprint(platformAdminAppRoot),
    () => restoreBuildRewrites(platformAdminTsconfigPath, originalPlatformAdminTsconfig, platformAdminNextEnvPath, originalPlatformAdminNextEnv));
  for (const app of additionalApps) {
    await ensureNextBuild(app.name, app.root, app.nextDist, buildEnvironment, appFingerprint(app.root),
      () => restoreBuildRewrites(app.tsconfig, app.originalTsconfig, app.nextEnv, app.originalNextEnv));
  }

  const web = startProcess(
    "web",
    process.execPath,
    [nextCli, "start", "--port", new URL(twoHostE2eOwnerListenerOrigin).port],
    appRoot,
    nextEnvironment,
  );
  const platformAdmin = startProcess(
    "platform-admin",
    process.execPath,
    [nextCli, "start", "--port", new URL(platformAdminOrigin).port],
    platformAdminAppRoot,
    nextEnvironment,
  );

  const additionalHosts = additionalApps.map(app => ({ ...app, process: startProcess(app.name,
    process.execPath, [nextCli, "start", "--port", new URL(app.origin).port], app.root, nextEnvironment) }));

  await waitForHttp("Private Practice Owner", web, `${twoHostE2eOwnerListenerOrigin}/`, nextReadinessTimeoutMs);
  await waitForHttp("Platform Admin", platformAdmin, `${platformAdminOrigin}/`, nextReadinessTimeoutMs);
  for (const host of additionalHosts) await waitForHttp(host.name, host.process, `${host.origin}/`, nextReadinessTimeoutMs);
  browserRouter = createTwoHostWebRouter({
    landingHost: new URL(twoHostE2eLandingOrigin).host,
    landingTarget: twoHostE2eLandingListenerOrigin,
    ownerTarget: twoHostE2eOwnerListenerOrigin,
  });
  await new Promise((resolveListen, rejectListen) => {
    browserRouter.once("error", rejectListen);
    browserRouter.listen(new URL(webOrigin).port, resolveListen);
  });

  webEnvironment.PLAYWRIGHT_PLATFORM_ADMIN_ORIGIN = platformAdminOrigin;
  const playwrightEnvironment = {
    ...webEnvironment,
    NODE_OPTIONS: `${webEnvironment.NODE_OPTIONS} --require "${resolve(scriptDirectory, "two-host-loopback-dns.cjs").replaceAll("\\", "/")}"`,
  };

  const playwright = startProcess(
    "playwright",
    process.execPath,
    [resolve(webRoot, "node_modules", "@playwright", "test", "cli.js"), "test", "--config", playwrightConfig, ...(process.env[twoHostE2eGrepEnvironmentKey] ? ["--grep", process.env[twoHostE2eGrepEnvironmentKey]] : []), ...playwrightArguments],
    webRoot,
    playwrightEnvironment,
  );

  const result = await Promise.race([
    waitForExit(playwright),
    waitForPrematureHostExit(main),
    waitForPrematureHostExit(web),
    waitForPrematureHostExit(platformAdmin),
    ...additionalHosts.map(host => waitForPrematureHostExit(host.process)),
  ]);

  if (result.code !== 0 || result.signal !== null) {
    throw new Error(`Two-host Playwright exited with code=${result.code}, signal=${result.signal}. Diagnostics are in ${artifactDirectory}.`);
  }
}

let signalPromise;
const handleSignal = () => {
  if (!signalPromise) {
    signalPromise = stopAll().finally(() => process.exit(130));
  }
};
process.on("SIGINT", handleSignal);
process.on("SIGTERM", handleSignal);

try {
  await run();
} catch (error) {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
} finally {
  await stopAll();
  await stopSmtpCapture();
  restoreNextTsconfig(appTsconfigPath, originalAppTsconfig);
  restoreNextTsconfig(platformAdminTsconfigPath, originalPlatformAdminTsconfig);
  restoreNextGeneratedFiles(appNextEnvPath, originalAppNextEnv);
  restoreNextGeneratedFiles(platformAdminNextEnvPath, originalPlatformAdminNextEnv);
  for (const app of additionalApps) {
    restoreNextTsconfig(app.tsconfig, app.originalTsconfig);
    restoreNextGeneratedFiles(app.nextEnv, app.originalNextEnv);
  }
  if (existsSync(proofStorageStatePath)) {
    rmSync(proofStorageStatePath, { force: true });
  }
}
