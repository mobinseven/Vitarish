import { execFileSync } from "node:child_process";
import { request as httpRequest } from "node:http";
import { request as httpsRequest } from "node:https";
import { connect as connectTcp } from "node:net";
import { checkServerIdentity, connect as connectTls } from "node:tls";

const tlsHandshakeTimeoutMilliseconds = 10000;
const httpRequestTimeoutMilliseconds = 10000;
const tcpConnectTimeoutMilliseconds = 3000;
const tcpScanConcurrency = 8;
const sshCommandTimeoutMilliseconds = 30000;
const minimumCertificateValidityDays = 14;
const expectedUnsafeMethodStatus = 405;
const tlsLegacyProtocolCiphers = "DEFAULT@SECLEVEL=0";

export const maximumClockDifferenceSeconds = 5;
export const expectedNtpSynchronized = "yes";
export const certificateHandshakeProtocol = "TLSv1.3";
export const tls12HandshakeProtocol = "TLSv1.2";
const tlsExpectations = Object.freeze({ accepted: "accepted", rejected: "rejected" });
const unsafeHttpMethods = Object.freeze({ trace: "TRACE", connect: "CONNECT" });
export const expectedOpenIngressPorts = Object.freeze([22, 80, 443]);
export const httpRedirectSuccessStatuses = Object.freeze([301, 302, 307, 308]);
export const publicIngressPorts = Object.freeze([
  21, 22, 23, 25, 53, 80, 110, 143, 443, 445,
  3000, 3001, 3002, 3003, 3004, 3005, 3006,
  3306, 5432, 5672, 6379, 8080, 8443, 9000, 9001, 9200, 27017,
]);
export const weakTls12Ciphers = Object.freeze(["AES128-SHA", "DES-CBC3-SHA", "ECDHE-RSA-AES128-SHA"]);
export const tlsProtocolProbes = Object.freeze([
  {
    probe: "TLS 1.0",
    minVersion: "TLSv1",
    maxVersion: "TLSv1",
    expectedProtocol: "TLSv1",
    ciphers: tlsLegacyProtocolCiphers,
    offered: `TLSv1 with ciphers=${tlsLegacyProtocolCiphers}`,
    expected: tlsExpectations.rejected,
  },
  {
    probe: "TLS 1.1",
    minVersion: "TLSv1.1",
    maxVersion: "TLSv1.1",
    expectedProtocol: "TLSv1.1",
    ciphers: tlsLegacyProtocolCiphers,
    offered: `TLSv1.1 with ciphers=${tlsLegacyProtocolCiphers}`,
    expected: tlsExpectations.rejected,
  },
  {
    probe: "TLS 1.2",
    minVersion: tls12HandshakeProtocol,
    maxVersion: tls12HandshakeProtocol,
    expectedProtocol: tls12HandshakeProtocol,
    ciphers: null,
    offered: "TLSv1.2 with default ciphers",
    expected: tlsExpectations.accepted,
  },
  {
    probe: "TLS 1.3",
    minVersion: certificateHandshakeProtocol,
    maxVersion: certificateHandshakeProtocol,
    expectedProtocol: certificateHandshakeProtocol,
    ciphers: null,
    offered: "TLSv1.3 with default ciphers",
    expected: tlsExpectations.accepted,
  },
]);

function errorCodeOf(error) {
  return typeof error === "object" && error !== null && typeof error.code === "string" ? error.code : null;
}

function errorText(error) {
  return error instanceof Error ? error.message : String(error ?? "unknown error");
}

export function isLocalTlsClientLimitation(error) {
  const code = errorCodeOf(error);
  if (code === "ERR_SSL_NO_PROTOCOLS_AVAILABLE" || code === "ERR_TLS_INVALID_PROTOCOL_VERSION" || code === "ERR_SSL_NO_CIPHER_MATCH") {
    return true;
  }
  return /no protocols available|no ciphers available|no cipher match/i.test(errorText(error));
}

export function probeTlsProtocol({ host, port = 443, servername, minVersion, maxVersion, ciphers, timeoutMs = tlsHandshakeTimeoutMilliseconds }) {
  return new Promise((resolve) => {
    let settled = false;
    let socket = null;
    const finish = (result) => {
      if (settled) return;
      settled = true;
      if (socket && !socket.destroyed) socket.destroy();
      resolve(result);
    };
    const options = {
      host,
      port,
      servername,
      minVersion,
      maxVersion,
      rejectUnauthorized: false,
    };
    if (ciphers) options.ciphers = ciphers;
    try {
      socket = connectTls(options, () => {
        const cipher = socket.getCipher();
        finish({
          accepted: true,
          negotiatedProtocol: socket.getProtocol(),
          cipher: cipher?.standardName ?? cipher?.name ?? null,
          cipherName: cipher?.name ?? null,
          authorized: socket.authorized === true,
          authorizationError: socket.authorizationError ? errorText(socket.authorizationError) : null,
          peerCertificate: socket.getPeerCertificate(true),
        });
      });
    } catch (error) {
      finish({ accepted: false, errorCode: errorCodeOf(error), errorMessage: errorText(error), clientCannotOffer: isLocalTlsClientLimitation(error) });
      return;
    }
    socket.setTimeout(timeoutMs, () => {
      finish({ accepted: false, errorCode: "ETIMEDOUT", errorMessage: `TLS handshake timed out after ${timeoutMs} ms`, clientCannotOffer: false });
    });
    socket.once("error", (error) => {
      finish({ accepted: false, errorCode: errorCodeOf(error), errorMessage: errorText(error), clientCannotOffer: isLocalTlsClientLimitation(error) });
    });
  });
}

export async function probeTlsProtocols({ host, port = 443, servername, timeoutMs = tlsHandshakeTimeoutMilliseconds }) {
  const attempts = [];
  for (const probe of tlsProtocolProbes) {
    const result = await probeTlsProtocol({
      host,
      port,
      servername,
      minVersion: probe.minVersion,
      maxVersion: probe.maxVersion,
      ciphers: probe.ciphers ?? undefined,
      timeoutMs,
    });
    attempts.push({ ...result, probe: probe.probe, offered: probe.offered, expected: probe.expected, expectedProtocol: probe.expectedProtocol });
  }
  for (const cipher of weakTls12Ciphers) {
    const result = await probeTlsProtocol({
      host,
      port,
      servername,
      minVersion: tls12HandshakeProtocol,
      maxVersion: tls12HandshakeProtocol,
      ciphers: `${cipher}@SECLEVEL=0`,
      timeoutMs,
    });
    attempts.push({
      ...result,
      probe: `TLS 1.2 cipher ${cipher}`,
      offered: `TLSv1.2 with ciphers=${cipher}@SECLEVEL=0`,
      expected: tlsExpectations.rejected,
      expectedProtocol: null,
    });
  }
  return attempts;
}

export function evaluateTlsScan(hostname, attempts) {
  const results = [];
  const failures = [];
  for (const attempt of attempts) {
    const { peerCertificate, ...observations } = attempt;
    let result = "PASS";
    let note = "";
    if (attempt.expected === tlsExpectations.rejected) {
      if (attempt.accepted) {
        result = "FAIL";
        note = `accepted with ${attempt.negotiatedProtocol ?? "unknown"} / ${attempt.cipher ?? "unknown"}`;
        failures.push(`${hostname} accepted ${attempt.probe}; expected rejection (negotiated ${attempt.negotiatedProtocol ?? "unknown"} / ${attempt.cipher ?? "unknown"}).`);
      } else if (attempt.clientCannotOffer) {
        result = "NOT TESTABLE";
        note = "the local TLS client could not offer this protocol or cipher";
      } else {
        note = `rejected${attempt.errorCode ? ` (${attempt.errorCode})` : ""}`;
      }
    } else if (attempt.accepted) {
      if (attempt.expectedProtocol && attempt.negotiatedProtocol !== attempt.expectedProtocol) {
        result = "FAIL";
        note = `negotiated ${attempt.negotiatedProtocol ?? "unknown"}`;
        failures.push(`${hostname} negotiated ${attempt.negotiatedProtocol ?? "unknown"} for ${attempt.probe}; expected ${attempt.expectedProtocol}.`);
      } else {
        note = `accepted with ${attempt.negotiatedProtocol ?? "unknown"} / ${attempt.cipher ?? "unknown"}`;
      }
    } else {
      result = "FAIL";
      note = attempt.errorMessage ?? "no handshake";
      failures.push(`${hostname} rejected ${attempt.probe}; expected acceptance${attempt.errorCode ? ` (${attempt.errorCode})` : ""}.`);
    }
    results.push({ ...observations, result, note });
  }
  return { attempts: results, failures };
}

const certificateSignatureAlgorithmNames = Object.freeze({
  "1.2.840.113549.1.1.4": "md5WithRSAEncryption",
  "1.2.840.113549.1.1.5": "sha1WithRSAEncryption",
  "1.2.840.113549.1.1.10": "RSASSA-PSS",
  "1.2.840.113549.1.1.11": "sha256WithRSAEncryption",
  "1.2.840.113549.1.1.12": "sha384WithRSAEncryption",
  "1.2.840.113549.1.1.13": "sha512WithRSAEncryption",
  "1.2.840.10045.4.1": "ecdsa-with-SHA1",
  "1.2.840.10045.4.3.2": "ecdsa-with-SHA256",
  "1.2.840.10045.4.3.3": "ecdsa-with-SHA384",
  "1.2.840.10045.4.3.4": "ecdsa-with-SHA512",
  "1.3.101.112": "Ed25519",
  "1.3.101.113": "Ed448",
});

function readDerElement(buffer, offset) {
  if (offset + 2 > buffer.length) throw new Error("DER element is truncated.");
  const tag = buffer[offset];
  const firstLengthByte = buffer[offset + 1];
  let contentOffset;
  let contentEnd;
  if (firstLengthByte < 0x80) {
    contentOffset = offset + 2;
    contentEnd = contentOffset + firstLengthByte;
  } else {
    const lengthBytes = firstLengthByte & 0x7f;
    if (lengthBytes === 0 || lengthBytes > 4 || offset + 2 + lengthBytes > buffer.length) {
      throw new Error("Unsupported DER length encoding.");
    }
    let length = 0;
    for (let index = 0; index < lengthBytes; index += 1) {
      length = length * 256 + buffer[offset + 2 + index];
    }
    contentOffset = offset + 2 + lengthBytes;
    contentEnd = contentOffset + length;
  }
  if (contentEnd > buffer.length) throw new Error("DER element exceeds the certificate buffer.");
  return { tag, contentOffset, contentEnd, next: contentEnd };
}

function decodeDerObjectIdentifier(bytes) {
  const subIdentifiers = [];
  let value = 0;
  for (const byte of bytes) {
    value = value * 128 + (byte & 0x7f);
    if ((byte & 0x80) === 0) {
      subIdentifiers.push(value);
      value = 0;
    }
  }
  if (subIdentifiers.length === 0) throw new Error("DER object identifier is empty.");
  const first = subIdentifiers[0];
  const firstArc = first < 40 ? 0 : first < 80 ? 1 : 2;
  return [firstArc, first - firstArc * 40, ...subIdentifiers.slice(1)].join(".");
}

export function readCertificateSignatureAlgorithm(der) {
  const buffer = Buffer.isBuffer(der) ? der : Buffer.from(der ?? []);
  try {
    const certificate = readDerElement(buffer, 0);
    if (certificate.tag !== 0x30) return null;
    const tbsCertificate = readDerElement(buffer, certificate.contentOffset);
    if (tbsCertificate.tag !== 0x30) return null;
    const algorithmIdentifier = readDerElement(buffer, tbsCertificate.next);
    if (algorithmIdentifier.tag !== 0x30) return null;
    const algorithm = readDerElement(buffer, algorithmIdentifier.contentOffset);
    if (algorithm.tag !== 0x06) return null;
    const oid = decodeDerObjectIdentifier(buffer.subarray(algorithm.contentOffset, algorithm.contentEnd));
    return { oid, name: certificateSignatureAlgorithmNames[oid] ?? null };
  } catch {
    return null;
  }
}

export function parseCertificateDate(value) {
  if (typeof value !== "string") return Number.NaN;
  return Date.parse(value.replace(/\s+/g, " ").trim());
}

function certificateNameValue(value) {
  if (Array.isArray(value)) return value.join(", ");
  return typeof value === "string" ? value : null;
}

function describeCertificateIssuer(issuer) {
  const commonName = certificateNameValue(issuer?.CN);
  const organization = certificateNameValue(issuer?.O);
  if (commonName && organization) return `${commonName} (${organization})`;
  return commonName ?? organization ?? null;
}

function describeCertificateKey(certificate) {
  const curve = certificate.nistCurve ?? certificate.asn1Curve;
  if (curve) return `EC ${curve}${certificate.bits ? ` (${certificate.bits} bits)` : ""}`;
  if (certificate.modulus) return `RSA${certificate.bits ? ` (${certificate.bits} bits)` : ""}`;
  if (certificate.bits) return `${certificate.bits}-bit key`;
  return null;
}

export function summarizeCertificate(hostname, tls13Attempt) {
  const unavailable = (reason) => ({
    hostname,
    available: false,
    reason,
    subjectCommonName: null,
    subjectAlternativeNames: null,
    issuer: null,
    validFrom: null,
    validTo: null,
    expiresInDays: null,
    key: null,
    signatureAlgorithm: null,
    authorized: false,
    authorizationError: null,
    hostnameMatches: false,
  });
  if (!tls13Attempt) return unavailable("the TLS 1.3 probe result was missing");
  if (!tls13Attempt.accepted) {
    return unavailable(tls13Attempt.clientCannotOffer ? "TLS 1.3 is not testable from this client" : tls13Attempt.errorMessage ?? "the TLS 1.3 handshake was not accepted");
  }
  const certificate = tls13Attempt.peerCertificate;
  if (!certificate || Object.keys(certificate).length === 0) return unavailable("the TLS 1.3 handshake returned no peer certificate");
  const validToMilliseconds = parseCertificateDate(certificate.valid_to);
  const signature = readCertificateSignatureAlgorithm(certificate.raw);
  let hostnameMatches = false;
  try {
    hostnameMatches = checkServerIdentity(hostname, certificate) === undefined;
  } catch {
    hostnameMatches = false;
  }
  return {
    hostname,
    available: true,
    reason: null,
    subjectCommonName: certificateNameValue(certificate.subject?.CN),
    subjectAlternativeNames: certificate.subjectaltname ?? null,
    issuer: describeCertificateIssuer(certificate.issuer),
    validFrom: certificate.valid_from ?? null,
    validTo: certificate.valid_to ?? null,
    expiresInDays: Number.isFinite(validToMilliseconds) ? Math.floor((validToMilliseconds - Date.now()) / 86400000) : null,
    key: describeCertificateKey(certificate),
    signatureAlgorithm: signature ? (signature.name ? `${signature.name} (${signature.oid})` : signature.oid) : null,
    authorized: tls13Attempt.authorized === true,
    authorizationError: tls13Attempt.authorizationError ?? null,
    hostnameMatches,
  };
}

export function evaluateCertificate(hostname, certificate) {
  const failures = [];
  if (!certificate.available) {
    failures.push(`${hostname} certificate could not be read from a TLS 1.3 handshake: ${certificate.reason}.`);
    return { certificate: { ...certificate, result: "FAIL" }, failures };
  }
  if (certificate.authorized !== true) {
    failures.push(`${hostname} certificate chain did not verify against the system trust store${certificate.authorizationError ? ` (${certificate.authorizationError})` : ""}.`);
  }
  if (certificate.hostnameMatches !== true) {
    failures.push(`${hostname} certificate does not match the hostname ${hostname}.`);
  }
  if (certificate.expiresInDays === null) {
    failures.push(`${hostname} certificate expiry could not be read from "${certificate.validTo ?? "an unreadable date"}".`);
  } else if (certificate.expiresInDays < minimumCertificateValidityDays) {
    failures.push(`${hostname} certificate expires in ${certificate.expiresInDays} day(s); expected at least ${minimumCertificateValidityDays}.`);
  }
  return { certificate: { ...certificate, result: failures.length === 0 ? "PASS" : "FAIL" }, failures };
}

export function probeHttpRedirect({ host, port = 80, hostname, path = "/", timeoutMs = httpRequestTimeoutMilliseconds }) {
  return new Promise((resolve) => {
    let settled = false;
    let request = null;
    const finish = (result) => {
      if (settled) return;
      settled = true;
      if (request && !request.destroyed) request.destroy();
      resolve(result);
    };
    try {
      request = httpRequest({ host, port, path, method: "GET", headers: { Host: hostname, Connection: "close" } }, (response) => {
        response.resume();
        finish({
          status: response.statusCode ?? null,
          location: typeof response.headers.location === "string" ? response.headers.location : null,
        });
      });
    } catch (error) {
      finish({ status: null, location: null, errorCode: errorCodeOf(error), errorMessage: errorText(error) });
      return;
    }
    request.setTimeout(timeoutMs, () => {
      finish({ status: null, location: null, errorCode: "ETIMEDOUT", errorMessage: `HTTP redirect probe timed out after ${timeoutMs} ms` });
    });
    request.once("error", (error) => {
      finish({ status: null, location: null, errorCode: errorCodeOf(error), errorMessage: errorText(error) });
    });
    request.end();
  });
}

export function evaluateHttpRedirect(hostname, redirect) {
  const failures = [];
  if (!httpRedirectSuccessStatuses.includes(redirect.status)) {
    failures.push(`${hostname} plain HTTP request returned ${redirect.status ?? "no response"}${redirect.errorCode ? ` (${redirect.errorCode})` : ""}; expected one of ${httpRedirectSuccessStatuses.join("/")}.`);
  }
  let target = null;
  if (typeof redirect.location === "string") {
    try {
      target = new URL(redirect.location);
    } catch {
      target = null;
    }
  }
  if (!target) {
    failures.push(`${hostname} plain HTTP redirect did not provide a valid Location header (${redirect.location ?? "missing"}).`);
  } else {
    if (target.protocol !== "https:") {
      failures.push(`${hostname} plain HTTP redirect points to ${target.protocol}//${target.host}; expected https.`);
    }
    if (target.hostname.toLowerCase() !== hostname.toLowerCase()) {
      failures.push(`${hostname} plain HTTP redirect points to ${target.hostname}; expected the same host.`);
    }
  }
  return { redirect: { ...redirect, result: failures.length === 0 ? "PASS" : "FAIL" }, failures };
}

function requestHttpsMethod({ host, port = 443, servername, method, path, timeoutMs = httpRequestTimeoutMilliseconds }) {
  return new Promise((resolve) => {
    let settled = false;
    let request = null;
    let tunnelSocket = null;
    const finish = (result) => {
      if (settled) return;
      settled = true;
      if (tunnelSocket && !tunnelSocket.destroyed) tunnelSocket.destroy();
      if (request && !request.destroyed) request.destroy();
      resolve(result);
    };
    try {
      request = httpsRequest({ host, port, servername, method, path, headers: { Host: servername, Connection: "close" } }, (response) => {
        response.resume();
        finish({ status: response.statusCode ?? null, tunnelEstablished: false });
      });
    } catch (error) {
      finish({ status: null, tunnelEstablished: false, omitted: true, errorCode: errorCodeOf(error), errorMessage: errorText(error) });
      return;
    }
    request.once("connect", (response, socket) => {
      tunnelSocket = socket;
      const status = response?.statusCode ?? null;
      finish({ status, tunnelEstablished: typeof status === "number" && status >= 200 && status < 300 });
    });
    request.setTimeout(timeoutMs, () => {
      finish({ status: null, tunnelEstablished: false, errorCode: "ETIMEDOUT", errorMessage: `${method} ${path} timed out after ${timeoutMs} ms` });
    });
    request.once("error", (error) => {
      finish({ status: null, tunnelEstablished: false, errorCode: errorCodeOf(error), errorMessage: errorText(error) });
    });
    request.end();
  });
}

export async function probeUnsafeMethods({ host, port = 443, servername, paths, timeoutMs = httpRequestTimeoutMilliseconds }) {
  const results = [];
  for (const path of paths) {
    const trace = await requestHttpsMethod({ host, port, servername, method: unsafeHttpMethods.trace, path, timeoutMs });
    results.push({ method: unsafeHttpMethods.trace, path, expectedStatus: expectedUnsafeMethodStatus, ...trace });
  }
  const connect = await requestHttpsMethod({ host, port, servername, method: unsafeHttpMethods.connect, path: "/", timeoutMs });
  results.push({ method: unsafeHttpMethods.connect, path: "/", expectedStatus: expectedUnsafeMethodStatus, ...connect });
  return results;
}

export function evaluateUnsafeMethods(hostname, requests) {
  const failures = [];
  const evaluated = requests.map((request) => {
    const label = `${request.method} ${request.path}`;
    if (request.omitted) {
      if (request.method === unsafeHttpMethods.connect) {
        return { ...request, result: "NOT TESTABLE", note: `CONNECT was omitted: ${request.errorMessage ?? "the local client could not issue it"}.` };
      }
      failures.push(`${hostname} ${label} could not be issued: ${request.errorMessage ?? "unknown local failure"}.`);
      return { ...request, result: "FAIL", note: request.errorMessage ?? "not issued" };
    }
    if (request.status === expectedUnsafeMethodStatus) {
      return { ...request, expectedStatus: expectedUnsafeMethodStatus, result: "PASS", note: "rejected by the ingress method guard" };
    }
    const tunnel = request.tunnelEstablished ? " and a tunnel was established (the socket was destroyed immediately)" : "";
    const observed = request.status === null || request.status === undefined ? request.errorCode ?? "no response" : request.status;
    failures.push(`${hostname} ${label} returned ${observed}${tunnel}; expected ${expectedUnsafeMethodStatus}.`);
    return { ...request, expectedStatus: expectedUnsafeMethodStatus, result: "FAIL", note: `${observed}${tunnel}` };
  });
  const connectOmitted = evaluated.some((request) => request.method === unsafeHttpMethods.connect && request.result === "NOT TESTABLE");
  return {
    requests: evaluated,
    note: connectOmitted
      ? "CONNECT could not be issued without a tunnel by this client and was omitted."
      : "CONNECT was issued only to observe the ingress answer; any established tunnel was destroyed before data flowed.",
    failures,
  };
}

export function probeTcpPort({ host, port, timeoutMs = tcpConnectTimeoutMilliseconds }) {
  return new Promise((resolve) => {
    let settled = false;
    let socket = null;
    const finish = (state, detail = {}) => {
      if (settled) return;
      settled = true;
      if (socket && !socket.destroyed) socket.destroy();
      resolve({ port, state, ...detail });
    };
    try {
      socket = connectTcp({ host, port });
    } catch (error) {
      finish("filtered", { errorCode: errorCodeOf(error), errorMessage: errorText(error) });
      return;
    }
    socket.setTimeout(timeoutMs, () => finish("filtered", { errorCode: "ETIMEDOUT", errorMessage: `TCP connect timed out after ${timeoutMs} ms` }));
    socket.once("connect", () => finish("open"));
    socket.once("error", (error) => {
      const code = errorCodeOf(error);
      if (code === "ECONNREFUSED") finish("closed", { errorCode: code });
      else finish("filtered", { errorCode: code, errorMessage: errorText(error) });
    });
  });
}

export async function scanTcpPorts({ host, ports = publicIngressPorts, timeoutMs = tcpConnectTimeoutMilliseconds, concurrency = tcpScanConcurrency }) {
  const results = new Array(ports.length);
  let nextIndex = 0;
  const worker = async () => {
    while (nextIndex < ports.length) {
      const index = nextIndex;
      nextIndex += 1;
      results[index] = await probeTcpPort({ host, port: ports[index], timeoutMs });
    }
  };
  const workerCount = Math.max(1, Math.min(concurrency, ports.length));
  await Promise.all(Array.from({ length: workerCount }, worker));
  return results;
}

export function evaluatePortScan(address, results) {
  const failures = [];
  const expectedOpen = new Set(expectedOpenIngressPorts);
  const ports = results.map((result) => {
    const shouldBeOpen = expectedOpen.has(result.port);
    let outcome = "PASS";
    if (result.state === "open" && !shouldBeOpen) {
      outcome = "FAIL";
      failures.push(`${address} port ${result.port} is open; expected only ${expectedOpenIngressPorts.join(", ")}.`);
    } else if (result.state !== "open" && shouldBeOpen) {
      outcome = "FAIL";
      failures.push(`${address} port ${result.port} is ${result.state}; expected it to be open.`);
    }
    return { ...result, expected: shouldBeOpen ? "open" : "closed", result: outcome };
  });
  return { ports, failures };
}

export function collectRemoteTimeSync({ sshTarget, timeoutMs = sshCommandTimeoutMilliseconds }) {
  const command = [
    "timedatectl show -p NTPSynchronized -p TimeUSec -p Timezone",
    "timedatectl timesync-status 2>/dev/null || chronyc tracking 2>/dev/null || true",
    "date -u +%s.%N",
  ].join("; ");
  const startedAt = new Date();
  const output = execFileSync("ssh", ["-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=yes", sshTarget, command], {
    encoding: "utf8",
    stdio: ["ignore", "pipe", "pipe"],
    timeout: timeoutMs,
  });
  const finishedAt = new Date();
  return { output, startedAt: startedAt.toISOString(), finishedAt: finishedAt.toISOString() };
}

export function parseTimedatectlShow(output) {
  const values = {};
  for (const line of String(output).split(/\r?\n/)) {
    const match = /^([A-Za-z][A-Za-z0-9]*)=(.*)$/.exec(line);
    if (match) values[match[1]] = match[2].trim();
  }
  return {
    ntpSynchronized: values.NTPSynchronized ?? null,
    timeUsec: values.TimeUSec ?? null,
    timezone: values.Timezone ?? null,
  };
}

export function parseTimeSyncStatus(output) {
  const text = String(output);
  const timedatectlServer = /^\s*Server:\s*(.+?)\s*$/m.exec(text)?.[1] ?? null;
  const timedatectlOffset = /^\s*Offset:\s*(.+?)\s*$/m.exec(text)?.[1] ?? null;
  const chronyServer = /^\s*Reference ID\s*:\s*(.+?)\s*$/m.exec(text)?.[1] ?? null;
  const chronyOffset = /^\s*Last offset\s*:\s*(.+?)\s*$/m.exec(text)?.[1]
    ?? /^\s*System time\s*:\s*(.+?)\s*$/m.exec(text)?.[1]
    ?? null;
  return {
    source: timedatectlServer || timedatectlOffset ? "timedatectl" : chronyServer || chronyOffset ? "chronyc" : null,
    server: timedatectlServer ?? chronyServer,
    offset: timedatectlOffset ?? chronyOffset,
  };
}

export function parseServerEpochSeconds(output) {
  const lines = String(output).split(/\r?\n/).map((line) => line.trim()).filter(Boolean);
  for (let index = lines.length - 1; index >= 0; index -= 1) {
    if (/^\d{9,}(?:\.\d+)?$/.test(lines[index])) return Number(lines[index]);
  }
  return Number.NaN;
}

export function summarizeTimeSync({ output, startedAt, finishedAt }) {
  const timedatectlShow = parseTimedatectlShow(output);
  const timeSyncStatus = parseTimeSyncStatus(output);
  const runnerMidpointEpochMilliseconds = (Date.parse(startedAt) + Date.parse(finishedAt)) / 2;
  const serverEpochSeconds = parseServerEpochSeconds(output);
  const clockDifferenceSeconds = Number.isFinite(serverEpochSeconds) && Number.isFinite(runnerMidpointEpochMilliseconds)
    ? Number((serverEpochSeconds - runnerMidpointEpochMilliseconds / 1000).toFixed(3))
    : null;
  return {
    ntpSynchronized: timedatectlShow.ntpSynchronized,
    timeUsec: timedatectlShow.timeUsec,
    timezone: timedatectlShow.timezone,
    syncSource: timeSyncStatus.source,
    syncServer: timeSyncStatus.server,
    syncOffset: timeSyncStatus.offset,
    serverTimeUtc: Number.isFinite(serverEpochSeconds) ? new Date(serverEpochSeconds * 1000).toISOString() : null,
    runnerTimeUtc: Number.isFinite(runnerMidpointEpochMilliseconds) ? new Date(runnerMidpointEpochMilliseconds).toISOString() : null,
    clockDifferenceSeconds,
  };
}

export function evaluateTimeSync(summary) {
  const failures = [];
  if (summary.ntpSynchronized !== expectedNtpSynchronized) {
    failures.push(`Remote clock NTP synchronisation is ${summary.ntpSynchronized === null || summary.ntpSynchronized === undefined ? "unknown" : `"${summary.ntpSynchronized}"`}; expected "${expectedNtpSynchronized}".`);
  }
  if (!Number.isFinite(summary.clockDifferenceSeconds)) {
    failures.push("Remote clock difference between the server and the runner could not be determined.");
  } else if (Math.abs(summary.clockDifferenceSeconds) >= maximumClockDifferenceSeconds) {
    failures.push(`Remote clock differs from the runner by ${summary.clockDifferenceSeconds} s; expected less than ${maximumClockDifferenceSeconds} s.`);
  }
  return { ...summary, result: failures.length === 0 ? "PASS" : "FAIL", failures };
}
