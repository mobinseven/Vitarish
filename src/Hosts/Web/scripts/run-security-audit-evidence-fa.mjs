import { spawn, spawnSync } from "node:child_process";
import { mkdirSync } from "node:fs";
import { randomBytes } from "node:crypto";
import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";
import {
  twoHostE2eAuditEvidenceDirectoryEnvironmentKey,
  twoHostE2eGrepEnvironmentKey,
  twoHostE2eLocaleEnvironmentKey,
} from "./two-host-e2e-artifacts.mjs";

const webRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const repositoryRoot = resolve(webRoot, "..", "..", "..");
const runner = resolve(webRoot, "scripts", "run-two-host-e2e.mjs");
const signInManagerTests = resolve(repositoryRoot, "tests", "Modules", "Users.Tests", "Users.Tests.csproj");
const evidenceRunId = `${new Date().toISOString().replaceAll(":", "-")}-${process.pid}-${randomBytes(4).toString("hex")}`;
const evidenceDirectory = resolve(webRoot, "artifacts", "two-host-e2e", "security-audit-evidence", "runs", evidenceRunId);
mkdirSync(evidenceDirectory, { recursive: true });

// The browser flow can prove only the public password paths. Keep the seven
// AuditingSignInManager overrides and their result, attribution, correlation,
// idempotency, redaction, exception, and audit-store-outage contracts in the
// same evidence command through the focused owner test before capturing the
// durable browser-visible records.
const signInManagerResult = spawnSync("dotnet", [
  "test",
  signInManagerTests,
  "--no-restore",
  "--filter",
  "FullyQualifiedName~AuditingSignInManagerTests",
], {
  cwd: repositoryRoot,
  stdio: "inherit",
  windowsHide: false,
});

if (signInManagerResult.status !== 0) {
  process.exit(signInManagerResult.status ?? 1);
}

const child = spawn(process.execPath, [runner], {
  cwd: webRoot,
  env: {
    ...process.env,
    [twoHostE2eLocaleEnvironmentKey]: "fa",
    [twoHostE2eAuditEvidenceDirectoryEnvironmentKey]: evidenceDirectory,
    [twoHostE2eGrepEnvironmentKey]: "real actions create visible platform security-audit evidence",
  },
  stdio: "inherit",
  windowsHide: false,
});

child.on("close", (code, signal) => {
  if (signal) process.kill(process.pid, signal);
  else process.exitCode = code ?? 1;
});
