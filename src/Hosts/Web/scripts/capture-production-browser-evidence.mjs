import { execFileSync } from "node:child_process";
import { createInterface } from "node:readline";
import { createReadStream, readFileSync } from "node:fs";
import { mkdir, writeFile } from "node:fs/promises";
import { isIPv4 } from "node:net";
import { basename, dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "@playwright/test";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptDirectory, "..");
const deploymentRoot = resolve(scriptDirectory, "..", "..", "deployment");
const deploymentCommand = readFileSync(resolve(deploymentRoot, "deploy-production.sh"), "utf8");
const remoteDeploymentRoot = /^deployment=(\/[A-Za-z0-9/-]+)$/m.exec(deploymentCommand)?.[1];
const stateFileName = /^state="\$deployment\/([A-Za-z0-9._-]+)"$/m.exec(deploymentCommand)?.[1];
if (!remoteDeploymentRoot || !stateFileName) throw new Error("Production deployment state contract could not be resolved from its owner.");
const authenticationRoutesPath = resolve(deploymentRoot, "..", "..", "Modules", "Authentication", "Contracts", "Routes", "AuthenticationRoutes.cs");
const usage = "Usage: node scripts/capture-production-browser-evidence.mjs PUBLIC_INGRESS_IPV4 SSH_TARGET COMMIT_SHA [SHARED_ENVIRONMENT_PATH] [OUTPUT_ROOT]";
if (process.argv[2] === "--help") {
  console.log(usage);
  process.exit(0);
}

const [publicIngressIPv4, sshTarget, commitArgument, sharedEnvironmentArgument, outputRootArgument] = process.argv.slice(2);
if (!publicIngressIPv4 || !isIPv4(publicIngressIPv4) || sshTarget !== `root@${publicIngressIPv4}`) throw new Error(usage);
if (!commitArgument || !/^[0-9a-f]{40}$/.test(commitArgument)) throw new Error("Provide the full commit SHA deployed by the production workflow.");
const sharedEnvironmentPath = resolve(sharedEnvironmentArgument ?? resolve(deploymentRoot, ".env.shared"));
const outputRoot = resolve(outputRootArgument ?? resolve(webRoot, "artifacts", "deployment-evidence"));
const capturedAt = new Date();
const runDirectory = resolve(outputRoot, capturedAt.toISOString().replace(/[:.]/g, "-"));

async function readHosts(path) {
  const hosts = [];
  const lines = createInterface({ input: createReadStream(path), crlfDelay: Infinity });
  for await (const line of lines) {
    const match = /^(VITARISH_PRODUCTION_([A-Z0-9_]+_HOST))=(.*)$/.exec(line);
    if (!match) continue;
    const [, name, , configuredHost] = match;
    const wildcard = configuredHost.startsWith("*.");
    const hostname = wildcard ? `deployment-probe.${configuredHost.slice(2)}` : configuredHost;
    if (!/^(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$/.test(hostname)) {
      throw new Error(`Invalid hostname configured for ${name}.`);
    }
    hosts.push({ name, environment: "production", hostname, configuredHost, wildcard });
  }
  if (hosts.length !== 8) {
    throw new Error(`Expected 8 configured production host entries; found ${hosts.length}.`);
  }
  if (hosts.filter(({ wildcard }) => wildcard).length !== 2) {
    throw new Error("Expected both configured production wildcard host probes.");
  }
  return hosts.sort((left, right) => left.name.localeCompare(right.name));
}

function readAuthenticationRoutes() {
  const source = readFileSync(authenticationRoutesPath, "utf8");
  const basePath = /const string BasePath = "([^"]+)";/.exec(source)?.[1];
  const profilePath = /const string ProfilePath = "([^"]+)";/.exec(source)?.[1];
  const sessionSuffix = /const string Session = BasePath \+ "([^"]+)";/.exec(source)?.[1];
  if (!basePath || !profilePath || !sessionSuffix) throw new Error("Authentication route constants could not be resolved from their owner.");
  return { profile: `${basePath}${profilePath}`, session: `${basePath}${sessionSuffix}` };
}

function collectRemoteState() {
  const command = `
set -e
cd ${remoteDeploymentRoot}
python3 - '${commitArgument}' <<'PY'
import hashlib, json, re, subprocess, sys
from datetime import datetime, timezone
from pathlib import Path

root = Path('${remoteDeploymentRoot}')
commit = sys.argv[1]
record = json.loads((root / '${stateFileName}').read_text())
files = {'Caddyfile', 'docker-compose.ingress.yml', 'docker-compose.test.yml', 'docker-compose.production.yml', '.env.shared', '.env.production'}
if record.get('commit') != commit or not re.fullmatch(r'[0-9a-f]{40}', commit) or set(record.get('sha256', {})) != files:
    raise SystemExit('Deployed workflow identity or configuration record does not match.')
for name, expected in record['sha256'].items():
    if hashlib.sha256((root / name).read_bytes()).hexdigest() != expected:
        raise SystemExit('Deployed configuration changed after the recorded workflow run.')

def compose(bundle, env_names, *arguments):
    command = ['docker', 'compose']
    for name in env_names:
        command += ['--env-file', str(root / name)]
    command += ['-f', str(root / ('docker-compose.' + bundle + '.yml'))]
    return subprocess.check_output(command + list(arguments), text=True).strip()

stacks = {}
for bundle, env_names in [('ingress', ['.env.shared']), ('production', ['.env.shared', '.env.production'])]:
    configuration = json.loads(compose(bundle, env_names, 'config', '--format', 'json'))
    services = []
    for service, configured in configuration['services'].items():
        if service == 'caddy-config-validator':
            continue
        ids = compose(bundle, env_names, 'ps', '-q', service).splitlines()
        if len(ids) != 1:
            raise SystemExit('A deployed Compose service is missing or has unexpected replicas.')
        detail = json.loads(subprocess.check_output(['docker', 'inspect', ids[0]], text=True))[0]
        image = detail['Config']['Image']
        if image != configured['image']:
            raise SystemExit('A running container image differs from its protected Compose reference.')
        if ('/vitarish/' in image or '/aistor/' in image) and not re.search(r'@sha256:(?!0{64}$)[0-9a-f]{64}$', image):
            raise SystemExit('A deployed product image lacks an immutable digest.')
        health = detail['State'].get('Health', {}).get('Status')
        services.append({'service': service, 'image': image, 'health': health, 'status': detail['State']['Status']})
    stacks[bundle] = services

hosts = {}
for line in (root / '.env.shared').read_text().splitlines():
    name, separator, value = line.partition('=')
    if separator and re.fullmatch(r'VITARISH_PRODUCTION_[A-Z0-9_]+_HOST', name):
        hosts[name] = value
print(json.dumps({'commit': commit, 'runId': record.get('runId'),
                  'capturedAtUtc': datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ'),
                  'configuredHosts': hosts, 'stacks': stacks}))
PY
`;
  const output = execFileSync("ssh", ["-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=yes", sshTarget, command], {
    encoding: "utf8", stdio: ["ignore", "pipe", "pipe"], timeout: 30000,
  });
  const state = JSON.parse(output);
  if (state.commit !== commitArgument || !/^[1-9][0-9]*$/.test(state.runId) || !state.capturedAtUtc) {
    throw new Error("Remote workflow release identity or service-state timestamp was missing.");
  }
  for (const host of hosts) {
    if (state.configuredHosts?.[host.name] !== host.configuredHost) {
      throw new Error("Operator and server production host configuration differ.");
    }
  }
  for (const services of Object.values(state.stacks)) if (services.length === 0) throw new Error("Remote Compose service state was incomplete.");
  return state;
}

function escapeHtml(value) {
  return String(value).replace(/[&<>"']/g, (character) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[character]);
}

function evidenceTable(title, headings, rows, capturedAtUtc) {
  const header = headings.map((heading) => `<th>${escapeHtml(heading)}</th>`).join("");
  const body = rows.map((row) => `<tr>${row.map((cell) => `<td>${escapeHtml(cell)}</td>`).join("")}</tr>`).join("");
  return `<!doctype html><html><head><meta charset="utf-8"><style>*{box-sizing:border-box}body{margin:0;padding:36px;background:#f4f6f8;color:#17212b;font:15px/1.5 Arial,sans-serif}main{max-width:1320px;margin:0 auto;background:#fff;border:1px solid #d8dee5;border-radius:12px;padding:28px}h1{font-size:25px;margin:0 0 6px}p{color:#526170;margin:0 0 22px}table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:11px 12px;border-bottom:1px solid #e4e8ed;overflow-wrap:anywhere}th{background:#edf1f5;font-weight:700}</style></head><body><main><h1>${escapeHtml(title)}</h1><p>Captured ${escapeHtml(capturedAtUtc)} · Commit ${escapeHtml(commitArgument)}</p><table><thead><tr>${header}</tr></thead><tbody>${body}</tbody></table></main></body></html>`;
}

const hosts = await readHosts(sharedEnvironmentPath);
const authenticationRoutes = readAuthenticationRoutes();
await mkdir(runDirectory, { recursive: true });
const serviceState = collectRemoteState();
const serviceStatePath = resolve(runDirectory, "service-status.json");
await writeFile(serviceStatePath, `${JSON.stringify(serviceState, null, 2)}\n`, "utf8");

const serviceRows = Object.entries(serviceState.stacks).flatMap(([stack, services]) =>
  services.map(({ service, image, health, status }) => [stack, service, image, health ?? "not reported", status]),
);
const statusBrowser = await chromium.launch({ headless: true });
try {
  const page = await statusBrowser.newPage({ viewport: { width: 1500, height: 1000 } });
  await page.setContent(evidenceTable("Remote Compose service state", ["Stack", "Service", "Image", "Health", "State"], serviceRows, serviceState.capturedAtUtc));
  await page.screenshot({ path: resolve(runDirectory, "service-status.png"), fullPage: true });
} finally {
  await statusBrowser.close();
}

const failures = [];
for (const [stackName, services] of Object.entries(serviceState.stacks)) {
  for (const service of services) {
    const healthy = service.health === "healthy" || (stackName === "ingress" && service.service === "caddy" && service.status === "running");
    if (service.status !== "running" || !healthy) failures.push(`${stackName}/${service.service} is ${service.health ?? service.status}.`);
  }
}

const resolverRules = hosts.map(({ hostname }) => `MAP ${hostname} ${publicIngressIPv4}`).join(", ");
const browser = await chromium.launch({ headless: true, args: [`--host-resolver-rules=${resolverRules}, EXCLUDE localhost`, "--no-proxy-server"] });
const productionApiResults = [];
const uiResults = [];
try {
  for (const host of hosts) {
    const url = `https://${host.hostname}/`;
    const slug = host.name.replace(/^VITARISH_PRODUCTION_/, "").replace(/_HOST$/, "").toLowerCase().replaceAll("_", "-");
    const screenshotName = `${host.environment}-${slug}.png`;
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    try {
      const page = await context.newPage();
      let cspViolationCount = 0;
      page.on("console", (message) => {
        if (/content security policy/i.test(message.text())) cspViolationCount += 1;
      });
      const response = await page.goto(url, { waitUntil: "domcontentloaded", timeout: 60000 });
      const uiStatus = response?.status() ?? null;
      await page.waitForTimeout(1500);
      const pageTextLength = await page.locator("body").innerText().then((text) => text.trim().length);
      await page.screenshot({ path: resolve(runDirectory, screenshotName), fullPage: true });
      const api = await page.evaluate(async ({ profilePath, sessionPath }) => {
        const requests = [
          { name: "authentication-profile", path: profilePath, expectedStatus: 200 },
          { name: "authentication-session", path: sessionPath, expectedStatus: 401 },
        ];
        const results = [];
        for (const request of requests) {
          try {
            const response = await fetch(request.path, { method: "GET", credentials: "omit", cache: "no-store", redirect: "manual" });
            results.push({ ...request, status: response.status, tlsVerified: window.isSecureContext });
          } catch {
            results.push({ ...request, status: null, tlsVerified: window.isSecureContext });
          }
        }
        return results;
      }, { profilePath: authenticationRoutes.profile, sessionPath: authenticationRoutes.session });

      uiResults.push({ environment: host.environment, configuredHost: host.name, hostname: host.hostname, wildcardProbe: host.wildcard, request: url, status: uiStatus, pageTextLength, cspViolationCount, tlsVerified: page.url().startsWith("https://") && await page.evaluate(() => window.isSecureContext), screenshot: screenshotName, capturedAtUtc: new Date().toISOString() });
      const apiResult = {
        environment: host.environment, configuredHost: host.name, hostname: host.hostname, wildcardProbe: host.wildcard,
        tlsVerified: api.every(({ tlsVerified }) => tlsVerified),
        requests: api.map(({ name, path, status, expectedStatus, tlsVerified }) => ({ name, path, status, expectedStatus, tlsVerified })),
      };
      productionApiResults.push(apiResult);
      if (uiStatus === null || uiStatus < 200 || uiStatus >= 400) failures.push(`${host.hostname} UI returned HTTP ${uiStatus ?? "no response"}.`);
      if (pageTextLength === 0) failures.push(`${host.hostname} rendered no visible page text.`);
      if (cspViolationCount > 0) failures.push(`${host.hostname} reported ${cspViolationCount} Content Security Policy console violation(s).`);
      for (const request of apiResult.requests) if (request.status !== request.expectedStatus) failures.push(`${host.hostname} ${request.name} returned HTTP ${request.status ?? "no response"}, expected ${request.expectedStatus}.`);
      if (!apiResult.tlsVerified) failures.push(`${host.hostname} API request did not use a secure browser context.`);
      console.log(`${host.environment} ${host.hostname}: UI ${uiStatus ?? "no response"}; profile ${api[0].status ?? "no response"}; session ${api[1].status ?? "no response"}`);
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      failures.push(`${host.hostname}: ${message}`);
      console.error(`Capture failed for ${host.hostname}: ${message}`);
    } finally {
      await context.close();
    }
  }
} finally {
  await browser.close();
}

let apiScreenshotPath = null;
const apiScreenshotBrowser = await chromium.launch({ headless: true });
try {
  const filename = "production-api-results.png";
  const page = await apiScreenshotBrowser.newPage({ viewport: { width: 1500, height: 1100 } });
  const rows = productionApiResults.flatMap(({ hostname, tlsVerified, requests }) =>
    requests.map(({ name, path, status, expectedStatus }) => [hostname, `${path} (${name})`, status ?? "no response", expectedStatus, tlsVerified ? "verified" : "failed", status === expectedStatus && tlsVerified ? "PASS" : "FAIL"]),
  );
  await page.setContent(evidenceTable("Production anonymous API checks", ["Host", "GET request", "Observed status", "Expected", "TLS", "Result"], rows, capturedAt.toISOString()));
  await page.screenshot({ path: resolve(runDirectory, filename), fullPage: true });
  apiScreenshotPath = filename;
  await page.close();
} finally {
  await apiScreenshotBrowser.close();
}

const manifest = { capturedAtUtc: capturedAt.toISOString(), commit: serviceState.commit, workflowRunId: serviceState.runId, publicIngressIPv4, remoteServiceStatus: basename(serviceStatePath), serviceStatusScreenshot: "service-status.png", uiResults, apiResults: productionApiResults, apiResultScreenshot: apiScreenshotPath, failures };
const manifestPath = resolve(runDirectory, "manifest.json");
await writeFile(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
console.log(`Evidence manifest: ${manifestPath}`);
if (failures.length > 0) throw new Error(`Cloud deployment evidence has ${failures.length} failed check(s).`);
