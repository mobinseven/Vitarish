import { spawn, spawnSync } from "node:child_process";
import { mkdirSync, readdirSync, rmSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";

const webRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const repositoryRoot = resolve(webRoot, "..", "..", "..");
const runner = resolve(webRoot, "scripts", "run-two-host-e2e.mjs");
const signInManagerTests = resolve(repositoryRoot, "tests", "Modules", "Users.Tests", "Users.Tests.csproj");
const evidenceDirectory = resolve(webRoot, "artifacts", "two-host-e2e", "security-audit-evidence");
mkdirSync(evidenceDirectory, { recursive: true });
for (const file of readdirSync(evidenceDirectory, { withFileTypes: true })) {
  if (file.isFile() && file.name.endsWith(".png")) rmSync(resolve(evidenceDirectory, file.name), { force: true });
}

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
    VITARISH_E2E_LOCALE: "fa",
    VITARISH_E2E_AUDIT_EVIDENCE_DIRECTORY: evidenceDirectory,
    VITARISH_E2E_GREP: "real actions create visible platform security-audit evidence",
  },
  stdio: "inherit",
  windowsHide: false,
});

child.on("close", (code, signal) => {
  if (signal) process.kill(process.pid, signal);
  else process.exitCode = code ?? 1;
});
