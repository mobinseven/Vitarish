import { randomUUID } from "node:crypto";
import { readFileSync } from "node:fs";
import http from "node:http";
import https from "node:https";
import net from "node:net";
import { dirname, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { cspBrowserEnvironment } from "./csp-browser-environment.cjs";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptDirectory, "..");
const deploymentRoot = resolve(webRoot, "..", "deployment");
const caddyPath = resolve(deploymentRoot, "Caddyfile");
const ingressComposePath = resolve(deploymentRoot, "docker-compose.ingress.yml");
const apiPrefix = "/api/";
const hopByHopHeaders = new Set(["connection", "keep-alive", "proxy-authenticate", "proxy-authorization", "te", "trailer", "transfer-encoding", "upgrade"]);

function assertSharedIngressContract(caddy, compose) {
  const requestHeader = /^\s*request_header Content-Security-Policy "\{\$VITARISH_CONTENT_SECURITY_POLICY\}"\s*$/m.test(caddy);
  const responseHeader = /^\s*Content-Security-Policy-Report-Only "\{\$VITARISH_CONTENT_SECURITY_POLICY\}"\s*$/m.test(caddy);
  const enforcingResponseHeader = /^\s*Content-Security-Policy\s+/m.test(caddy);
  const policyLine = /^\s*VITARISH_CONTENT_SECURITY_POLICY:\s*"([^"]+)"\s*$/m.exec(compose)?.[1];
  if (!requestHeader || !responseHeader || enforcingResponseHeader || !policyLine) {
    throw new Error("The ingress must share one request CSP and Report-Only response policy without an enforcing response header.");
  }
  return policyLine;
}

function validatePolicy(policy) {
  const directives = new Map(policy.split(";").map((part) => part.trim()).filter(Boolean).map((part) => {
    const [name, ...sources] = part.split(/\s+/u);
    return [name.toLowerCase(), sources];
  }));
  const exact = (directive, sources) => {
    const actual = directives.get(directive);
    if (!actual || actual.join(" ") !== sources.join(" ")) {
      throw new Error(`The local CSP proxy requires the ingress ${directive} contract to remain ${sources.join(" ")}.`);
    }
  };
  exact("default-src", ["'self'"]);
  exact("script-src", ["'self'", "'nonce-{http.request.uuid}'"]);
  exact("style-src-elem", ["'self'", "'nonce-{http.request.uuid}'"]);
  exact("style-src-attr", ["'unsafe-inline'"]);
  exact("img-src", ["'self'", "data:", "blob:"]);
  exact("media-src", ["'self'", "blob:"]);
  exact("frame-ancestors", ["'none'"]);
  exact("object-src", ["'none'"]);
  if (/\*|'unsafe-eval'|sha(?:256|384|512)-/iu.test(policy)) {
    throw new Error("The ingress CSP contains a wildcard, unsafe-eval, or a hash exception.");
  }
  return directives;
}

export function readIngressPolicy(caddy = readFileSync(caddyPath, "utf8"), compose = readFileSync(ingressComposePath, "utf8")) {
  const policyTemplate = assertSharedIngressContract(caddy, compose);
  validatePolicy(policyTemplate);
  return policyTemplate;
}

export function createRequestPolicy(policyTemplate, nonce = randomUUID()) {
  if (!/^[A-Za-z0-9_-]{32,}$/u.test(nonce)) throw new Error("A fresh request nonce is required.");
  const policy = policyTemplate.replaceAll("{http.request.uuid}", nonce);
  validatePolicy(policy.replaceAll(nonce, "{http.request.uuid}"));
  return { nonce, policy };
}

function isHtmlDocument(request) {
  return request.method === "GET" && String(request.headers.accept ?? "").includes("text/html") && request.headers.rsc !== "1";
}

function sanitizedPath(requestUrl) {
  try {
    return new URL(requestUrl, "http://localhost").pathname;
  } catch {
    return "/";
  }
}

function forwardHeaders(source, destinationHost, requestPolicy) {
  const headers = {};
  for (const [name, value] of Object.entries(source)) {
    if (value === undefined || hopByHopHeaders.has(name.toLowerCase()) || name.toLowerCase() === "host") continue;
    headers[name] = value;
  }
  headers.host = destinationHost;
  if (requestPolicy) headers["content-security-policy"] = requestPolicy;
  return headers;
}

function apiTarget(apiOrigin, requestUrl) {
  const url = new URL(requestUrl, apiOrigin);
  return {
    protocol: url.protocol,
    hostname: url.hostname,
    port: url.port,
    path: `${url.pathname}${url.search}`,
    hostHeader: url.host,
  };
}

function proxyHttpRequest(request, response, upstream, policyTemplate) {
  const isApiRequest = request.url?.startsWith(apiPrefix) ?? false;
  const documentPolicy = !isApiRequest && isHtmlDocument(request) ? createRequestPolicy(policyTemplate) : null;
  const target = isApiRequest ? apiTarget(upstream.apiOrigin, request.url ?? "/") : {
    protocol: "http:",
    hostname: "127.0.0.1",
    port: upstream.nextPort,
    path: request.url ?? "/",
    hostHeader: `127.0.0.1:${upstream.nextPort}`,
  };
  const transport = target.protocol === "https:" ? https : http;
  const proxyRequest = transport.request({
    protocol: target.protocol,
    hostname: target.hostname,
    port: target.port,
    path: target.path,
    method: request.method,
    headers: forwardHeaders(request.headers, target.hostHeader, documentPolicy?.policy),
    rejectUnauthorized: target.protocol !== "https:",
  }, (proxyResponse) => {
    const headers = { ...proxyResponse.headers };
    if (documentPolicy) headers["content-security-policy-report-only"] = documentPolicy.policy;
    response.writeHead(proxyResponse.statusCode ?? 502, headers);
    proxyResponse.pipe(response);
  });
  proxyRequest.on("error", (error) => {
    if (!response.headersSent) response.writeHead(502, { "content-type": "text/plain; charset=utf-8" });
    response.end("CSP browser proxy upstream failure.");
    process.stderr.write(`Proxy failure for ${sanitizedPath(request.url ?? "/")}: ${error.message}\n`);
  });
  request.pipe(proxyRequest);
}

function proxyWebSocket(request, socket, head, upstream) {
  const target = apiTarget(upstream.apiOrigin, request.url ?? "/");
  if (target.protocol !== "https:") {
    socket.destroy(new Error("The E2E API target must use HTTPS."));
    return;
  }
  const proxyRequest = https.request({
    hostname: target.hostname,
    port: target.port,
    path: target.path,
    headers: {
      ...forwardHeaders(request.headers, target.hostHeader),
      connection: "Upgrade",
      upgrade: request.headers.upgrade,
    },
    // The E2E host uses its local development certificate on loopback only.
    rejectUnauthorized: false,
  });
  proxyRequest.on("upgrade", (proxyResponse, proxySocket, proxyHead) => {
    const statusText = proxyResponse.statusMessage ?? "Switching Protocols";
    socket.write(`HTTP/1.1 ${proxyResponse.statusCode ?? 101} ${statusText}\r\n`);
    for (const [name, value] of Object.entries(proxyResponse.headers)) {
      if (value !== undefined) socket.write(`${name}: ${Array.isArray(value) ? value.join(", ") : value}\r\n`);
    }
    socket.write("\r\n");
    if (proxyHead.length > 0) socket.write(proxyHead);
    if (head.length > 0) proxySocket.write(head);
    socket.pipe(proxySocket).pipe(socket);
  });
  proxyRequest.on("response", (proxyResponse) => {
    socket.write(`HTTP/1.1 ${proxyResponse.statusCode ?? 502} ${proxyResponse.statusMessage ?? "Bad Gateway"}\r\n\r\n`);
    socket.end();
  });
  proxyRequest.on("error", () => socket.destroy());
  proxyRequest.end();
}

export function createProxyServers(appTargets, apiOrigin, policyTemplate = readIngressPolicy()) {
  const parsedApiOrigin = new URL(apiOrigin);
  if (parsedApiOrigin.protocol !== "https:" || !["localhost", "127.0.0.1", "[::1]"].includes(parsedApiOrigin.hostname)) {
    throw new Error("The CSP browser proxy only accepts the loopback HTTPS E2E API host.");
  }
  validatePolicy(policyTemplate);
  return Object.entries(appTargets).map(([appName, target]) => {
    const upstream = { apiOrigin: parsedApiOrigin, nextPort: target.nextPort };
    const server = http.createServer((request, response) => proxyHttpRequest(request, response, upstream, policyTemplate));
    server.on("upgrade", (request, socket, head) => proxyWebSocket(request, socket, head, upstream));
    return { appName, port: target.proxyPort, server };
  });
}

async function startFromEnvironment() {
  const appTargets = JSON.parse(process.env[cspBrowserEnvironment.proxyAppTargets] ?? "{}");
  const apiOrigin = process.env[cspBrowserEnvironment.proxyApiOrigin];
  if (!apiOrigin || Object.keys(appTargets).length !== 6) throw new Error("Provide all six app proxy targets and the local E2E API origin.");
  const servers = createProxyServers(appTargets, apiOrigin);
  await Promise.all(servers.map(({ appName, port, server }) => new Promise((resolveListen, rejectListen) => {
    server.once("error", rejectListen);
    server.listen(port, "127.0.0.1", () => {
      server.removeListener("error", rejectListen);
      process.stdout.write(`${appName} CSP proxy listening on ${port}.\n`);
      resolveListen();
    });
  })));
  const stop = () => Promise.all(servers.map(({ server }) => new Promise((resolveClose) => server.close(() => resolveClose()))));
  process.once("SIGINT", () => { void stop().then(() => process.exit(130)); });
  process.once("SIGTERM", () => { void stop().then(() => process.exit(143)); });
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  startFromEnvironment().catch((error) => {
    process.stderr.write(`${error instanceof Error ? error.message : String(error)}\n`);
    process.exitCode = 1;
  });
}
