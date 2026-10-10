import { execFileSync } from "node:child_process";
import { createInterface } from "node:readline";
import { createReadStream, readFileSync } from "node:fs";
import { mkdir, writeFile } from "node:fs/promises";
import { isIPv4 } from "node:net";
import { basename, dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "@playwright/test";
import {
  certificateHandshakeProtocol,
  collectRemoteTimeSync,
  evaluateCertificate,
  evaluateHttpRedirect,
  evaluatePortScan,
  evaluateTimeSync,
  evaluateTlsScan,
  evaluateUnsafeMethods,
  expectedNtpSynchronized,
  httpRedirectSuccessStatuses,
  maximumClockDifferenceSeconds,
  probeHttpRedirect,
  probeTlsProtocols,
  probeUnsafeMethods,
  scanTcpPorts,
  summarizeCertificate,
  summarizeTimeSync,
} from "./production-evidence-checks.mjs";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptDirectory, "..");
const deploymentRoot = resolve(scriptDirectory, "..", "..", "deployment");
const deploymentCommand = readFileSync(resolve(deploymentRoot, "deploy-production.sh"), "utf8");
const remoteDeploymentRoot = /^deployment=(\/[A-Za-z0-9/-]+)$/m.exec(deploymentCommand)?.[1];
const stateFileName = /^state="\$deployment\/([A-Za-z0-9._-]+)"$/m.exec(deploymentCommand)?.[1];
if (!remoteDeploymentRoot || !stateFileName) throw new Error("Production deployment state contract could not be resolved from its owner.");
const authenticationRoutesPath = resolve(deploymentRoot, "..", "..", "Modules", "Authentication", "Contracts", "Routes", "AuthenticationRoutes.cs");
const usage = [
  "Usage: node scripts/capture-production-browser-evidence.mjs PUBLIC_INGRESS_IPV4 SSH_TARGET COMMIT_SHA [SHARED_ENVIRONMENT_PATH] [OUTPUT_ROOT]",
  "Captures UI, API, security-header, TLS protocol/cipher, certificate, HTTP-redirect, unsafe-method, ingress port, and server time-sync evidence for the deployed commit.",
].join("\n");
if (process.argv[2] === "--help") {
  console.log(usage);
  process.exit(0);
}

const [publicIngressIPv4, sshTarget, commitArgument, sharedEnvironmentArgument, outputRootArgument] = process.argv.slice(2);
if (!publicIngressIPv4 || !isIPv4(publicIngressIPv4) || sshTarget !== `root@${publicIngressIPv4}`) throw new Error(usage);
if (!commitArgument || !/^[0-9a-f]{40}$/.test(commitArgument)) throw new Error("Provide the full commit SHA deployed by the production workflow.");
const outputRoot = resolve(outputRootArgument ?? resolve(webRoot, "artifacts", "deployment-evidence"));
const capturedAt = new Date();
const runDirectory = resolve(outputRoot, capturedAt.toISOString().replace(/[:.]/g, "-"));

function parseHosts(configuredHosts) {
  const hosts = Object.entries(configuredHosts)
    .filter(([name]) => /^VITARISH_PRODUCTION_[A-Z0-9_]+_HOST$/.test(name))
    .map(([name, configuredHost]) => {
      const wildcard = configuredHost.startsWith("*.");
      const hostname = wildcard ? `deployment-probe.${configuredHost.slice(2)}` : configuredHost;
      if (!/^(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$/.test(hostname)) {
        throw new Error(`Invalid hostname configured for ${name}.`);
      }
      return { name, environment: "production", hostname, configuredHost, wildcard };
    });
  if (hosts.length !== 8) {
    throw new Error(`Expected 8 configured production host entries; found ${hosts.length}.`);
  }
  if (hosts.filter(({ wildcard }) => wildcard).length !== 2) {
    throw new Error("Expected both configured production wildcard host probes.");
  }
  return hosts.sort((left, right) => left.name.localeCompare(right.name));
}

async function readHosts(path) {
  const configuredHosts = {};
  const lines = createInterface({ input: createReadStream(path), crlfDelay: Infinity });
  for await (const line of lines) {
    const match = /^(VITARISH_PRODUCTION_([A-Z0-9_]+_HOST))=(.*)$/.exec(line);
    if (!match) continue;
    const [, name, , configuredHost] = match;
    if (Object.hasOwn(configuredHosts, name)) throw new Error(`Duplicate production hostname setting: ${name}.`);
    configuredHosts[name] = configuredHost;
  }
  return parseHosts(configuredHosts);
}

function readAuthenticationRoutes() {
  const source = readFileSync(authenticationRoutesPath, "utf8");
  const basePath = /const string BasePath = "([^"]+)";/.exec(source)?.[1];
  const profilePath = /const string ProfilePath = "([^"]+)";/.exec(source)?.[1];
  const sessionSuffix = /const string Session = BasePath \+ "([^"]+)";/.exec(source)?.[1];
  if (!basePath || !profilePath || !sessionSuffix) throw new Error("Authentication route constants could not be resolved from their owner.");
  return { profile: `${basePath}${profilePath}`, session: `${basePath}${sessionSuffix}` };
}

function collectRemoteState(expectedHosts) {
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
  if (expectedHosts) {
    for (const host of expectedHosts) {
      if (state.configuredHosts?.[host.name] !== host.configuredHost) {
        throw new Error("Operator and server production host configuration differ.");
      }
    }
  }
  for (const services of Object.values(state.stacks)) if (services.length === 0) throw new Error("Remote Compose service state was incomplete.");
  return state;
}

function escapeHtml(value) {
  return String(value).replace(/[&<>"']/g, (character) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[character]);
}

const revalidatedCacheControl = "no-cache, no-store, must-revalidate";
const revalidatedPragma = "no-cache";
const staticAssetCacheControlMinimumAge = 31536000;
const tlsScanScreenshotName = "tls-scan.png";
const certificateScreenshotName = "certificate.png";
const httpRedirectScreenshotName = "http-redirect.png";
const unsafeMethodScreenshotName = "unsafe-methods.png";
const portScanScreenshotName = "port-scan.png";
const timeSyncScreenshotName = "time-sync.png";
const requiredSecurityHeaders = {
  "strict-transport-security": "max-age=31536000",
  "x-content-type-options": "nosniff",
  "x-frame-options": "DENY",
  "x-xss-protection": "1; mode=block",
};

function headerValue(headers, name) {
  const value = headers?.[name];
  return typeof value === "string" ? value.trim() : null;
}

function securityHeaderFailures(kind, headers) {
  const failures = [];
  for (const [name, expected] of Object.entries(requiredSecurityHeaders)) {
    const actual = headerValue(headers, name);
    if (actual !== expected) failures.push(`${name} is ${actual === null ? "missing" : `"${actual}"`}, expected "${expected}"`);
  }
  if (kind === "static") {
    const cacheControl = headerValue(headers, "cache-control") ?? "";
    const maxAge = /(?:^|,)\s*max-age=(\d+)/i.exec(cacheControl);
    if (!/immutable/i.test(cacheControl) || !maxAge || Number(maxAge[1]) < staticAssetCacheControlMinimumAge || /no-store/i.test(cacheControl)) {
      failures.push(`cache-control is "${cacheControl}", expected the upstream immutable max-age=${staticAssetCacheControlMinimumAge} value to be preserved`);
    }
  } else {
    const cacheControl = headerValue(headers, "cache-control");
    if (cacheControl !== revalidatedCacheControl) failures.push(`cache-control is ${cacheControl === null ? "missing" : `"${cacheControl}"`}, expected "${revalidatedCacheControl}"`);
    const pragma = headerValue(headers, "pragma");
    if (pragma !== revalidatedPragma) failures.push(`pragma is ${pragma === null ? "missing" : `"${pragma}"`}, expected "${revalidatedPragma}"`);
  }
  return failures;
}

function evidenceTable(title, headings, rows, capturedAtUtc) {
  const header = headings.map((heading) => `<th>${escapeHtml(heading)}</th>`).join("");
  const body = rows.map((row) => `<tr>${row.map((cell) => `<td>${escapeHtml(cell)}</td>`).join("")}</tr>`).join("");
  return `<!doctype html><html><head><meta charset="utf-8"><style>*{box-sizing:border-box}body{margin:0;padding:36px;background:#f4f6f8;color:#17212b;font:15px/1.5 Arial,sans-serif}main{max-width:1320px;margin:0 auto;background:#fff;border:1px solid #d8dee5;border-radius:12px;padding:28px}h1{font-size:25px;margin:0 0 6px}p{color:#526170;margin:0 0 22px}table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:11px 12px;border-bottom:1px solid #e4e8ed;overflow-wrap:anywhere}th{background:#edf1f5;font-weight:700}</style></head><body><main><h1>${escapeHtml(title)}</h1><p>Captured ${escapeHtml(capturedAtUtc)}</p><table><thead><tr>${header}</tr></thead><tbody>${body}</tbody></table></main></body></html>`;
}

async function captureEvidenceTable(filename, title, headings, rows, viewportWidth = 1500) {
  const evidenceBrowser = await chromium.launch({ headless: true });
  try {
    const page = await evidenceBrowser.newPage({ viewport: { width: viewportWidth, height: 1100 } });
    await page.setContent(evidenceTable(title, headings, rows, capturedAt.toISOString()));
    await page.screenshot({ path: resolve(runDirectory, filename), fullPage: true });
    await page.close();
  } finally {
    await evidenceBrowser.close();
  }
  return filename;
}

const expectedHosts = sharedEnvironmentArgument ? await readHosts(resolve(sharedEnvironmentArgument)) : null;
const authenticationRoutes = readAuthenticationRoutes();
await mkdir(runDirectory, { recursive: true });
const serviceState = collectRemoteState(expectedHosts);
const hosts = expectedHosts ?? parseHosts(serviceState.configuredHosts);
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
const securityHeadersResults = [];
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
        const staticAsset = document.querySelector('script[src*="/_next/static/"]')?.getAttribute("src")
          ?? document.querySelector('link[rel="stylesheet"][href*="/_next/static/"]')?.getAttribute("href");
        const staticAssetPath = staticAsset ? new URL(staticAsset, window.location.origin).pathname : null;
        const requests = [
          { name: "authentication-profile", path: profilePath, expectedStatus: 200 },
          { name: "authentication-session", path: sessionPath, expectedStatus: 401 },
          ...(staticAssetPath ? [{ name: "immutable-static-asset", path: staticAssetPath, expectedStatus: 200 }] : []),
        ];
        const results = [];
        for (const request of requests) {
          try {
            const response = await fetch(request.path, { method: "GET", credentials: "omit", cache: "no-store", redirect: "manual" });
            results.push({ ...request, status: response.status, tlsVerified: window.isSecureContext, headers: Object.fromEntries(response.headers.entries()) });
          } catch {
            results.push({ ...request, status: null, tlsVerified: window.isSecureContext, headers: {} });
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
      const profileRequest = api.find(({ name }) => name === "authentication-profile");
      const staticAssetRequest = api.find(({ name }) => name === "immutable-static-asset");
      const securityHeaderResponses = [
        { kind: "document", path: new URL(url).pathname, status: uiStatus, headers: response?.headers() ?? {} },
        { kind: "api", path: profileRequest?.path ?? authenticationRoutes.profile, status: profileRequest?.status ?? null, headers: profileRequest?.headers ?? {} },
        { kind: "static", path: staticAssetRequest?.path ?? null, status: staticAssetRequest?.status ?? null, headers: staticAssetRequest?.headers ?? {} },
      ];
      const securityFailures = securityHeaderResponses.flatMap(({ kind, path, headers }) =>
        securityHeaderFailures(kind, headers).map((message) => `${host.hostname} ${kind} response ${path ?? "(no /_next/static/ asset found)"}: ${message}`),
      );
      if (!staticAssetRequest) securityFailures.push(`${host.hostname}: no /_next/static/ asset was found on the document to verify immutable caching.`);
      const securityHeaderSummary = securityHeaderResponses.map(({ kind, path, status, headers }) => ({
        kind,
        path,
        status,
        "cache-control": headerValue(headers, "cache-control"),
        pragma: headerValue(headers, "pragma"),
        "x-content-type-options": headerValue(headers, "x-content-type-options"),
        "x-frame-options": headerValue(headers, "x-frame-options"),
        "x-xss-protection": headerValue(headers, "x-xss-protection"),
        "strict-transport-security": headerValue(headers, "strict-transport-security"),
      }));
      securityHeadersResults.push({ environment: host.environment, configuredHost: host.name, hostname: host.hostname, responses: securityHeaderSummary, result: securityFailures.length === 0 ? "PASS" : "FAIL" });
      failures.push(...securityFailures);
      if (uiStatus === null || uiStatus < 200 || uiStatus >= 400) failures.push(`${host.hostname} UI returned HTTP ${uiStatus ?? "no response"}.`);
      if (pageTextLength === 0) failures.push(`${host.hostname} rendered no visible page text.`);
      if (cspViolationCount > 0) failures.push(`${host.hostname} reported ${cspViolationCount} Content Security Policy console violation(s).`);
      for (const request of apiResult.requests) if (request.status !== request.expectedStatus) failures.push(`${host.hostname} ${request.name} returned HTTP ${request.status ?? "no response"}, expected ${request.expectedStatus}.`);
      if (!apiResult.tlsVerified) failures.push(`${host.hostname} API request did not use a secure browser context.`);
      console.log(`${host.environment} ${host.hostname}: UI ${uiStatus ?? "no response"}; profile ${api[0].status ?? "no response"}; session ${api[1].status ?? "no response"}; static ${staticAssetRequest?.status ?? "no response"}`);
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

let securityHeaderScreenshotPath = null;
const securityHeaderBrowser = await chromium.launch({ headless: true });
try {
  const filename = "production-security-response-headers.png";
  const page = await securityHeaderBrowser.newPage({ viewport: { width: 1700, height: 1100 } });
  const rows = securityHeadersResults.flatMap(({ hostname, responses, result }) =>
    responses.map((response) => [
      hostname, `${response.kind} (${response.status ?? "no response"})`, response.path ?? "not found",
      response["cache-control"] ?? "missing",
      response.pragma ?? "missing",
      response["x-content-type-options"] ?? "missing",
      response["x-frame-options"] ?? "missing",
      response["x-xss-protection"] ?? "missing",
      response["strict-transport-security"] ?? "missing",
      result,
    ]),
  );
  await page.setContent(evidenceTable(
    "Production NSMAIL-1 security and cache response headers",
    ["Host", "Response", "Path", "Cache-Control", "Pragma", "X-Content-Type-Options", "X-Frame-Options", "X-XSS-Protection", "Strict-Transport-Security", "Result"],
    rows,
    capturedAt.toISOString()));
  await page.screenshot({ path: resolve(runDirectory, filename), fullPage: true });
  securityHeaderScreenshotPath = filename;
  await page.close();
} finally {
  await securityHeaderBrowser.close();
}

const tlsScanResults = [];
const certificateResults = [];
const httpRedirectResults = [];
const unsafeMethodResults = [];
for (const host of hosts) {
  try {
    const attempts = await probeTlsProtocols({ host: publicIngressIPv4, servername: host.hostname });
    const tlsEvaluation = evaluateTlsScan(host.hostname, attempts);
    tlsScanResults.push({ environment: host.environment, configuredHost: host.name, hostname: host.hostname, wildcardProbe: host.wildcard, attempts: tlsEvaluation.attempts });
    failures.push(...tlsEvaluation.failures);

    const tls13Attempt = attempts.find(({ expectedProtocol }) => expectedProtocol === certificateHandshakeProtocol);
    const certificateEvaluation = evaluateCertificate(host.hostname, summarizeCertificate(host.hostname, tls13Attempt));
    certificateResults.push({ environment: host.environment, configuredHost: host.name, hostname: host.hostname, wildcardProbe: host.wildcard, ...certificateEvaluation.certificate });
    failures.push(...certificateEvaluation.failures);

    const redirectProbe = await probeHttpRedirect({ host: publicIngressIPv4, hostname: host.hostname });
    const redirectEvaluation = evaluateHttpRedirect(host.hostname, redirectProbe);
    httpRedirectResults.push({ environment: host.environment, configuredHost: host.name, hostname: host.hostname, wildcardProbe: host.wildcard, requestUrl: `http://${host.hostname}/`, ...redirectEvaluation.redirect });
    failures.push(...redirectEvaluation.failures);

    const methodProbes = await probeUnsafeMethods({ host: publicIngressIPv4, servername: host.hostname, paths: ["/", authenticationRoutes.profile] });
    const methodEvaluation = evaluateUnsafeMethods(host.hostname, methodProbes);
    unsafeMethodResults.push({ environment: host.environment, configuredHost: host.name, hostname: host.hostname, wildcardProbe: host.wildcard, requests: methodEvaluation.requests, note: methodEvaluation.note });
    failures.push(...methodEvaluation.failures);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    failures.push(`${host.hostname} transport and hardening evidence capture failed: ${message}`);
    console.error(`Transport and hardening evidence capture failed for ${host.hostname}: ${message}`);
  }
}

let portScanResults = [];
try {
  const portScanEvaluation = evaluatePortScan(publicIngressIPv4, await scanTcpPorts({ host: publicIngressIPv4 }));
  portScanResults = portScanEvaluation.ports;
  failures.push(...portScanEvaluation.failures);
} catch (error) {
  const message = error instanceof Error ? error.message : String(error);
  failures.push(`Ingress port scan of ${publicIngressIPv4} failed: ${message}`);
  console.error(`Ingress port scan failed: ${message}`);
}

let timeSyncResult;
try {
  const timeSyncEvaluation = evaluateTimeSync(summarizeTimeSync(collectRemoteTimeSync({ sshTarget })));
  timeSyncResult = { sshTarget, ...timeSyncEvaluation };
  failures.push(...timeSyncEvaluation.failures);
} catch (error) {
  const message = error instanceof Error ? error.message : String(error);
  const syncFailure = `Remote server time synchronisation could not be read: ${message}`;
  timeSyncResult = { sshTarget, result: "FAIL", failures: [syncFailure] };
  failures.push(syncFailure);
}

const tlsScanScreenshot = await captureEvidenceTable(
  tlsScanScreenshotName,
  "Production TLS protocol and cipher scan",
  ["Host", "Probe", "Client offer", "Expected", "Observed", "Negotiated protocol", "Negotiated cipher", "Result"],
  // The screenshot is document evidence; probes this client cannot offer stay in the manifest only.
  tlsScanResults.flatMap(({ hostname, attempts }) => attempts.filter((attempt) => !attempt.clientCannotOffer).map((attempt) => [
    hostname,
    attempt.probe,
    attempt.offered,
    attempt.expected,
    attempt.accepted ? "accepted" : "rejected",
    attempt.negotiatedProtocol ?? "n/a",
    attempt.cipher ?? "n/a",
    attempt.result,
  ])),
);

const certificateScreenshot = await captureEvidenceTable(
  certificateScreenshotName,
  "Production TLS certificate evidence",
  ["Host", "Subject CN", "Subject alternative names", "Issuer", "Valid from", "Valid to", "Expires in (days)", "Key", "Signature algorithm", "Chain authorized", "Hostname match", "Result"],
  certificateResults.map((certificate) => [
    certificate.hostname,
    certificate.subjectCommonName ?? "not available",
    certificate.subjectAlternativeNames ?? "not reported",
    certificate.issuer ?? "not available",
    certificate.validFrom ?? "unknown",
    certificate.validTo ?? "unknown",
    certificate.expiresInDays === null ? "unknown" : `${certificate.expiresInDays}`,
    certificate.key ?? "unknown",
    certificate.signatureAlgorithm ?? "unknown",
    certificate.authorized ? "verified against the system trust store" : `not verified${certificate.authorizationError ? ` (${certificate.authorizationError})` : ""}`,
    certificate.hostnameMatches ? "match" : "mismatch",
    certificate.result,
  ]),
  1900,
);

const httpRedirectScreenshot = await captureEvidenceTable(
  httpRedirectScreenshotName,
  "Production plain HTTP to HTTPS redirect",
  ["Host", "Plain HTTP request", "Observed status", "Location", "Expected", "Result"],
  httpRedirectResults.map((redirect) => [
    redirect.hostname,
    redirect.requestUrl,
    redirect.status === null || redirect.status === undefined ? "no response" : `${redirect.status}`,
    redirect.location ?? redirect.errorMessage ?? "missing",
    `${httpRedirectSuccessStatuses.join("/")} to https://${redirect.hostname}/`,
    redirect.result,
  ]),
);

const unsafeMethodScreenshot = await captureEvidenceTable(
  unsafeMethodScreenshotName,
  "Production ingress unsafe-method guard",
  ["Host", "Request", "Method", "Observed status", "Expected", "Result", "Note"],
  unsafeMethodResults.flatMap(({ hostname, requests }) => requests.map((request) => [
    hostname,
    request.path,
    request.method,
    request.omitted ? "omitted" : request.status === null || request.status === undefined ? "no response" : `${request.status}`,
    `${request.expectedStatus}`,
    request.result,
    request.note ?? "",
  ])),
);

const portScanScreenshot = await captureEvidenceTable(
  portScanScreenshotName,
  "Production public ingress port scan",
  ["Address", "Port", "State", "Expectation", "Result", "Detail"],
  portScanResults.map((port) => [
    publicIngressIPv4,
    `${port.port}`,
    port.state,
    port.expected,
    port.result,
    port.errorCode ?? "",
  ]),
);

const clockDifference = timeSyncResult.clockDifferenceSeconds;
const clockDifferenceWithinTolerance = Number.isFinite(clockDifference) && Math.abs(clockDifference) < maximumClockDifferenceSeconds;
const timeSyncRows = [
  [
    "NTP synchronised",
    timeSyncResult.ntpSynchronized ?? "unknown",
    expectedNtpSynchronized,
    timeSyncResult.ntpSynchronized === expectedNtpSynchronized ? "PASS" : "FAIL",
    [
      timeSyncResult.timezone ? `timezone ${timeSyncResult.timezone}` : null,
      timeSyncResult.timeUsec ? `TimeUSec ${timeSyncResult.timeUsec}` : null,
      timeSyncResult.syncSource ? `source ${timeSyncResult.syncSource}` : null,
      timeSyncResult.syncServer ? `server ${timeSyncResult.syncServer}` : null,
      timeSyncResult.syncOffset ? `offset ${timeSyncResult.syncOffset}` : null,
    ].filter(Boolean).join("; ") || "not reported",
  ],
  [
    "Clock difference (server - runner)",
    Number.isFinite(clockDifference) ? `${clockDifference} s` : "unknown",
    `absolute difference under ${maximumClockDifferenceSeconds} s`,
    clockDifferenceWithinTolerance ? "PASS" : "FAIL",
    timeSyncResult.serverTimeUtc ? `server ${timeSyncResult.serverTimeUtc}; runner ${timeSyncResult.runnerTimeUtc}` : "server time could not be read",
  ],
];
const timeSyncScreenshot = await captureEvidenceTable(
  timeSyncScreenshotName,
  "Production server time synchronisation",
  ["Item", "Observed", "Expected", "Result", "Detail"],
  timeSyncRows,
);

const manifest = { capturedAtUtc: capturedAt.toISOString(), commit: serviceState.commit, workflowRunId: serviceState.runId, publicIngressIPv4, remoteServiceStatus: basename(serviceStatePath), serviceStatusScreenshot: "service-status.png", uiResults, apiResults: productionApiResults, apiResultScreenshot: apiScreenshotPath, securityHeaderResults: securityHeadersResults, securityHeaderScreenshot: securityHeaderScreenshotPath, tlsScanResults, tlsScanScreenshot, certificateResults, certificateScreenshot, httpRedirectResults, httpRedirectScreenshot, unsafeMethodResults, unsafeMethodScreenshot, portScanResults, portScanScreenshot, timeSyncResult, timeSyncScreenshot, failures };
const manifestPath = resolve(runDirectory, "manifest.json");
await writeFile(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
console.log(`Evidence manifest: ${manifestPath}`);
if (failures.length > 0) throw new Error(`Cloud deployment evidence has ${failures.length} failed check(s).`);
