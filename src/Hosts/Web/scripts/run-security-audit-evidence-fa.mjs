import { spawn } from "node:child_process";
import { readdirSync, rmSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";

const webRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const runner = resolve(webRoot, "scripts", "run-two-host-e2e.mjs");
const evidenceDirectory = resolve(webRoot, "artifacts", "two-host-e2e", "security-audit-evidence");
for (const file of readdirSync(evidenceDirectory, { withFileTypes: true })) {
  if (file.isFile() && file.name.endsWith(".png")) rmSync(resolve(evidenceDirectory, file.name), { force: true });
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
