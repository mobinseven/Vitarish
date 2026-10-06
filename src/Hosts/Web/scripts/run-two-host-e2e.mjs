import { spawn } from "node:child_process";
import { createWriteStream, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import http from "node:http";
import https from "node:https";
import net from "node:net";
import crypto from "node:crypto";
import { apiProxyTargetEnvironmentKey } from "./api-proxy-rewrites.mjs";
import {
  twoHostE2eApiOriginEnvironmentKey,
  twoHostE2eArtifactDirectoryEnvironmentKey,
  twoHostE2eGrepEnvironmentKey,
  twoHostE2eNextDistDirectoryEnvironmentKey,
  twoHostE2eProofScreenshotEnvironmentKey,
  twoHostE2eProofStorageStateEnvironmentKey,
} from "./two-host-e2e-artifacts.mjs";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptDirectory, "..");
const repositoryRoot = resolve(webRoot, "..", "..", "..");
const appRoot = resolve(webRoot, "apps", "private-practice-owner");
const platformAdminAppRoot = resolve(webRoot, "apps", "platform-admin");
const e2eProject = resolve(repositoryRoot, "src", "Hosts", "E2e", "E2e.csproj");
const playwrightConfig = resolve(webRoot, "playwright.two-host.config.ts");
const artifactRoot = resolve(webRoot, "artifacts", "two-host-e2e");
const runId = `${new Date().toISOString().replaceAll(":", "-")}-${process.pid}-${crypto.randomBytes(4).toString("hex")}`;
const artifactDirectory = resolve(artifactRoot, "runs", runId);
const proofStorageStatePath = resolve(artifactDirectory, "authenticated-user-security-state.json");
const appTsconfigPath = resolve(appRoot, "tsconfig.json");
const appNextEnvPath = resolve(appRoot, "next-env.d.ts");
const appNextDistDirectory = `.next-two-host-e2e-${runId}`;
const appNextDistPath = resolve(appRoot, appNextDistDirectory);
const platformAdminTsconfigPath = resolve(platformAdminAppRoot, "tsconfig.json");
const platformAdminNextEnvPath = resolve(platformAdminAppRoot, "next-env.d.ts");
const platformAdminNextDistPath = resolve(platformAdminAppRoot, appNextDistDirectory);
const originalAppTsconfig = readFileSync(appTsconfigPath, "utf8");
const originalAppNextEnv = readFileSync(appNextEnvPath, "utf8");
const originalPlatformAdminTsconfig = readFileSync(platformAdminTsconfigPath, "utf8");
const originalPlatformAdminNextEnv = readFileSync(platformAdminNextEnvPath, "utf8");
const mainOrigin = "https://localhost:7272";
const webOrigin = "http://localhost:3005";
const platformAdminOrigin = "http://localhost:3006";
const smtpCapturePath = resolve(artifactDirectory, "e2e-smtp-capture.eml");
const smtpPort = 2525;
const readinessProbeTimeoutMs = 5000;

mkdirSync(resolve(artifactRoot, "runs"), { recursive: true });
mkdirSync(artifactDirectory);

const processes = [];
let stopping = false;
let smtpServer;

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
    const request = client.get(url, { rejectUnauthorized: false }, (response) => {
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
  return new Promise((resolveExit) => {
    if (child.exitCode !== null || child.signalCode !== null) {
      resolveExit({ code: child.exitCode, signal: child.signalCode });
      return;
    }
    child.once("close", (code, signal) => resolveExit({ code, signal }));
  });
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
  if (!child || child.exitCode !== null || child.signalCode !== null) return;
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
  await Promise.all(processes.slice().reverse().map(stopProcessTree));
}

function restoreNextTsconfig(tsconfigPath, originalTsconfig) {
  const currentTsconfig = readFileSync(tsconfigPath, "utf8");
  const generatedTypesGlob = `${appNextDistDirectory}/types/**/*.ts`;
  if (!originalTsconfig.includes(generatedTypesGlob) && currentTsconfig.includes(generatedTypesGlob)) {
    writeFileSync(tsconfigPath, originalTsconfig);
  }
}

function restoreNextGeneratedFiles(nextEnvPath, originalNextEnv, nextDistPath) {
  const currentNextEnv = readFileSync(nextEnvPath, "utf8");
  const generatedRoutesReference = `${appNextDistDirectory}/types/routes.d.ts`;
  if (!originalNextEnv.includes(generatedRoutesReference) && currentNextEnv.includes(generatedRoutesReference)) {
    writeFileSync(nextEnvPath, originalNextEnv);
  }

  if (existsSync(nextDistPath)) {
    rmSync(nextDistPath, { recursive: true, force: true });
  }
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
  await startSmtpCapture();
  const mainEnvironment = createEnvironment({
    ASPNETCORE_ENVIRONMENT: "E2E",
    ASPNETCORE_URLS: mainOrigin,
    WebAuthn__Origins__0: webOrigin,
    WebAuthn__Origins__1: platformAdminOrigin,
    [twoHostE2eApiOriginEnvironmentKey]: mainOrigin,
    VITARISH_E2E_WEB_ORIGIN: webOrigin,
    VITARISH_E2E_PLATFORM_ADMIN_ORIGIN: platformAdminOrigin,
    VITARISH_E2E_PASSWORD: password,
    Vitarish__Intelligence__AgentRuntime__SharedSecret: agentSharedSecret,
    Vitarish__Communications__Providers__Smtp__Host: "127.0.0.1",
    Vitarish__Communications__Providers__Smtp__Port: String(smtpPort),
    Vitarish__Communications__Providers__Smtp__EnableSsl: "false",
    Vitarish__Communications__Providers__Smtp__Name: "E2E",
    Vitarish__Communications__Providers__Smtp__Username: "e2e",
    Vitarish__Communications__Providers__Smtp__Password: "e2e",
    VITARISH_E2E_SMTP_CAPTURE_PATH: smtpCapturePath,
    [twoHostE2eArtifactDirectoryEnvironmentKey]: artifactDirectory,
    [twoHostE2eProofScreenshotEnvironmentKey]: proofScreenshotPath,
  });
  // The browser calls /api on its own web origin, as in production; Next proxies it to
  // Main. Node trusts Main's local development certificate through the system store.
  const webEnvironment = createEnvironment({
    NEXT_PUBLIC_API_BASE_URL: "",
    [apiProxyTargetEnvironmentKey]: mainOrigin,
    [twoHostE2eApiOriginEnvironmentKey]: mainOrigin,
    NODE_USE_SYSTEM_CA: "1",
    NODE_OPTIONS: withSystemCa(process.env.NODE_OPTIONS),
    [twoHostE2eNextDistDirectoryEnvironmentKey]: appNextDistDirectory,
    VITARISH_E2E_USERNAME: "e2e-owner",
    VITARISH_E2E_EMAIL_CHANGE_USERNAME: "e2e-email-change-user",
    VITARISH_E2E_PASSWORD: password,
    VITARISH_E2E_AGENT_SHARED_SECRET: agentSharedSecret,
    VITARISH_E2E_SMTP_CAPTURE_PATH: smtpCapturePath,
    [twoHostE2eArtifactDirectoryEnvironmentKey]: artifactDirectory,
    [twoHostE2eProofStorageStateEnvironmentKey]: proofStorageStatePath,
    [twoHostE2eProofScreenshotEnvironmentKey]: proofScreenshotPath,
  });
  delete webEnvironment.NEXT_PUBLIC_VITARISH_MOCK_API;
  delete webEnvironment.NEXT_PUBLIC_VITARISH_MOCK_SCENARIO;

  await ensurePortAvailable("E2e", mainOrigin);
  await ensurePortAvailable("Private Practice Owner", webOrigin);
  await ensurePortAvailable("Platform Admin", platformAdminOrigin);

  const main = startProcess(
    "e2e",
    "dotnet",
    ["run", "--project", e2eProject, "--no-launch-profile", "--urls", mainOrigin],
    repositoryRoot,
    mainEnvironment,
  );
  const web = startProcess(
    "web",
    process.execPath,
    [resolve(webRoot, "node_modules", "next", "dist", "bin", "next"), "dev", "--port", "3005"],
    appRoot,
    webEnvironment,
  );
  const platformAdmin = startProcess(
    "platform-admin",
    process.execPath,
    [resolve(webRoot, "node_modules", "next", "dist", "bin", "next"), "dev", "--port", "3006"],
    platformAdminAppRoot,
    webEnvironment,
  );

  await waitForHttp("E2e", main, `${mainOrigin}/api/modules/authentication/antiforgery`);
  await waitForHttp("Private Practice Owner", web, `${webOrigin}/`);
  await waitForHttp("Platform Admin", platformAdmin, `${platformAdminOrigin}/`);

  webEnvironment.PLAYWRIGHT_PLATFORM_ADMIN_ORIGIN = platformAdminOrigin;

  const playwright = startProcess(
    "playwright",
    process.execPath,
    [resolve(webRoot, "node_modules", "@playwright", "test", "cli.js"), "test", "--config", playwrightConfig, ...(process.env[twoHostE2eGrepEnvironmentKey] ? ["--grep", process.env[twoHostE2eGrepEnvironmentKey]] : [])],
    webRoot,
    webEnvironment,
  );

  const result = await Promise.race([
    waitForExit(playwright),
    waitForPrematureHostExit(main),
    waitForPrematureHostExit(web),
    waitForPrematureHostExit(platformAdmin),
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
  restoreNextGeneratedFiles(appNextEnvPath, originalAppNextEnv, appNextDistPath);
  restoreNextGeneratedFiles(platformAdminNextEnvPath, originalPlatformAdminNextEnv, platformAdminNextDistPath);
  if (existsSync(proofStorageStatePath)) {
    rmSync(proofStorageStatePath, { force: true });
  }
}
