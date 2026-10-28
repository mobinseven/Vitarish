import assert from "node:assert/strict";
import { createServer as createHttpServer } from "node:http";
import { createServer as createTcpServer } from "node:net";
import test from "node:test";
import { rootCertificates } from "node:tls";
import { X509Certificate } from "node:crypto";

import {
  evaluateCertificate,
  evaluateHttpRedirect,
  evaluatePortScan,
  evaluateTimeSync,
  evaluateTlsScan,
  evaluateUnsafeMethods,
  expectedOpenIngressPorts,
  isLocalTlsClientLimitation,
  parseCertificateDate,
  parseServerEpochSeconds,
  parseTimeSyncStatus,
  parseTimedatectlShow,
  probeHttpRedirect,
  probeTlsProtocol,
  readCertificateSignatureAlgorithm,
  readCertificateCrlDistributionPoints,
  scanTcpPorts,
  summarizeCertificate,
  summarizeTimeSync,
  tlsProtocolProbes,
  weakTls12Ciphers,
} from "./production-evidence-checks.mjs";

test("the transport policy lists hold the released TLS, cipher, and port expectations", () => {
  assert.deepEqual(
    tlsProtocolProbes.filter(({ expected }) => expected === "accepted").map(({ probe }) => probe),
    ["TLS 1.2", "TLS 1.3"],
  );
  assert.ok(tlsProtocolProbes.filter(({ expected }) => expected === "rejected").every(({ ciphers }) => ciphers === "DEFAULT@SECLEVEL=0"));
  assert.deepEqual([...weakTls12Ciphers], ["AES128-SHA", "DES-CBC3-SHA", "ECDHE-RSA-AES128-SHA"]);
  assert.deepEqual([...expectedOpenIngressPorts], [22, 80, 443]);
});

test("TLS scan evaluation passes the hardened protocol and cipher policy", () => {
  const result = evaluateTlsScan("app.example.test", [
    { probe: "TLS 1.0", accepted: false, expected: "rejected", clientCannotOffer: false, errorCode: "ERR_SSL_TLSV1_ALERT_PROTOCOL_VERSION" },
    { probe: "TLS 1.1", accepted: false, expected: "rejected", clientCannotOffer: false, errorCode: "ERR_SSL_TLSV1_ALERT_PROTOCOL_VERSION" },
    { probe: "TLS 1.2", accepted: true, expected: "accepted", expectedProtocol: "TLSv1.2", negotiatedProtocol: "TLSv1.2", cipher: "TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384" },
    { probe: "TLS 1.3", accepted: true, expected: "accepted", expectedProtocol: "TLSv1.3", negotiatedProtocol: "TLSv1.3", cipher: "TLS_AES_256_GCM_SHA384" },
    { probe: "TLS 1.2 cipher AES128-SHA", accepted: false, expected: "rejected", clientCannotOffer: false, errorCode: "ERR_SSL_SSLV3_ALERT_HANDSHAKE_FAILURE" },
  ]);
  assert.deepEqual(result.failures, []);
  assert.deepEqual(result.attempts.map((attempt) => attempt.result), ["PASS", "PASS", "PASS", "PASS", "PASS"]);
});

test("TLS scan evaluation fails accepted legacy protocols, rejected required protocols, and accepted weak ciphers", () => {
  const result = evaluateTlsScan("app.example.test", [
    { probe: "TLS 1.0", accepted: true, expected: "rejected", negotiatedProtocol: "TLSv1", cipher: "TLS_RSA_WITH_AES_128_CBC_SHA" },
    { probe: "TLS 1.2", accepted: false, expected: "accepted", expectedProtocol: "TLSv1.2", errorCode: "ECONNRESET", errorMessage: "socket hang up" },
    { probe: "TLS 1.2 cipher AES128-SHA", accepted: true, expected: "rejected", negotiatedProtocol: "TLSv1.2", cipher: "AES128-SHA" },
  ]);
  assert.equal(result.failures.length, 3);
  assert.match(result.failures[0], /accepted TLS 1\.0/);
  assert.match(result.failures[1], /rejected TLS 1\.2/);
  assert.match(result.failures[2], /accepted TLS 1\.2 cipher AES128-SHA/);
  assert.deepEqual(result.attempts.map((attempt) => attempt.result), ["FAIL", "FAIL", "FAIL"]);
});

test("TLS scan evaluation records a locally unsupported protocol as not testable", () => {
  const result = evaluateTlsScan("app.example.test", [
    { probe: "TLS 1.0", accepted: false, expected: "rejected", clientCannotOffer: true, errorCode: "ERR_SSL_NO_PROTOCOLS_AVAILABLE", errorMessage: "No protocols available" },
  ]);
  assert.deepEqual(result.failures, []);
  assert.equal(result.attempts[0].result, "NOT TESTABLE");
});

test("local TLS client limitation classification separates client support from server rejection", () => {
  assert.equal(isLocalTlsClientLimitation(Object.assign(new Error("No protocols available"), { code: "ERR_SSL_NO_PROTOCOLS_AVAILABLE" })), true);
  assert.equal(isLocalTlsClientLimitation(Object.assign(new Error("no cipher match"), { code: "ERR_SSL_NO_CIPHER_MATCH" })), true);
  assert.equal(isLocalTlsClientLimitation(Object.assign(new Error("write EPROTO alert protocol version"), { code: "EPROTO" })), false);
  assert.equal(isLocalTlsClientLimitation(undefined), false);
});

function derElement(tag, content) {
  if (content.length < 0x80) return Buffer.concat([Buffer.from([tag, content.length]), content]);
  const lengthBytes = [];
  let length = content.length;
  while (length > 0) {
    lengthBytes.unshift(length & 0xff);
    length >>= 8;
  }
  return Buffer.concat([Buffer.from([tag, 0x80 | lengthBytes.length, ...lengthBytes]), content]);
}

test("certificate signature algorithm is decoded from the DER certificate structure", () => {
  const ecdsaWithSha256 = Buffer.from([0x2a, 0x86, 0x48, 0xce, 0x3d, 0x04, 0x03, 0x02]);
  const algorithmIdentifier = derElement(0x30, derElement(0x06, ecdsaWithSha256));
  const tbsCertificate = derElement(0x30, Buffer.alloc(200, 0x01));
  const certificate = derElement(0x30, Buffer.concat([tbsCertificate, algorithmIdentifier]));
  assert.deepEqual(readCertificateSignatureAlgorithm(certificate), { oid: "1.2.840.10045.4.3.2", name: "ecdsa-with-SHA256" });
});

test("certificate signature algorithm decoding refuses malformed input", () => {
  assert.equal(readCertificateSignatureAlgorithm(Buffer.from([0x30, 0x03, 0x06, 0x01, 0x2a])), null);
  assert.equal(readCertificateSignatureAlgorithm(Buffer.alloc(0)), null);
  assert.equal(readCertificateSignatureAlgorithm(undefined), null);
});

test("certificate validity dates use the OpenSSL text format", () => {
  assert.equal(parseCertificateDate("Dec  1 12:00:00 2026 GMT"), Date.UTC(2026, 11, 1, 12, 0, 0));
  assert.ok(Number.isNaN(parseCertificateDate(null)));
});

test("certificate evaluation requires trust, hostname match, and at least fourteen days of validity", () => {
  const certificate = { available: true, authorized: true, hostnameMatches: true, expiresInDays: 30, validTo: "Dec  1 12:00:00 2026 GMT" };
  assert.deepEqual(evaluateCertificate("app.example.test", { ...certificate }).failures, []);
  assert.match(evaluateCertificate("app.example.test", { ...certificate, authorized: false, authorizationError: "UNABLE_TO_VERIFY_LEAF_SIGNATURE" }).failures[0], /trust store/);
  assert.match(evaluateCertificate("app.example.test", { ...certificate, hostnameMatches: false }).failures[0], /does not match the hostname/);
  assert.match(evaluateCertificate("app.example.test", { ...certificate, expiresInDays: 10 }).failures[0], /expires in 10 day/);
  assert.match(evaluateCertificate("app.example.test", { available: false, reason: "TLS 1.3 handshake was not accepted" }).failures[0], /could not be read/);
});

test("certificate evidence reports public usage, issuer and revocation sources without inferring unavailable data", () => {
  const certificate = summarizeCertificate("app.example.test", {
    accepted: true,
    authorized: true,
    peerCertificate: {
      ext_key_usage: ["1.3.6.1.5.5.7.3.1"],
      issuerCertificate: { ca: true },
      infoAccess: { "OCSP - URI": ["https://ocsp.example.test/"] },
    },
  });
  assert.deepEqual(certificate.extendedKeyUsage, ["1.3.6.1.5.5.7.3.1"]);
  assert.equal(certificate.issuerCa, true);
  assert.equal(certificate.issuerBasicConstraints, null);
  assert.equal(certificate.issuerInspectionError, "certificate DER is unavailable");
  assert.deepEqual(certificate.ocspSources, ["https://ocsp.example.test/"]);
  assert.equal(certificate.crlDistributionPoints, null);
  assert.equal(certificate.crlInspectionError, "certificate DER is unavailable");
  assert.match(evaluateCertificate("app.example.test", certificate).failures.join(" "), /CRL distribution points could not be inspected/);
  assert.match(evaluateCertificate("app.example.test", certificate).failures.join(" "), /issuer certificate basic constraints could not be inspected/);

  const missing = summarizeCertificate("app.example.test", { accepted: true, peerCertificate: { subject: {} } });
  assert.equal(missing.extendedKeyUsage, null);
  assert.equal(missing.issuerCa, null);
  assert.equal(missing.ocspSources, null);
  assert.deepEqual(readCertificateCrlDistributionPoints(undefined), { value: null, error: "certificate DER is unavailable" });
});

test("CRL extension inspection accepts a real public certificate and distinguishes malformed DER", () => {
  const publicCertificate = new X509Certificate(rootCertificates[0]);
  const inspected = readCertificateCrlDistributionPoints(publicCertificate.raw);
  assert.equal(inspected.error, null);
  assert.ok(inspected.value === null || /CRL Distribution Points/i.test(inspected.value));
  assert.ok(readCertificateCrlDistributionPoints(Buffer.from([0])).error);
});

test("HTTP redirect evaluation requires a redirect from the same host to HTTPS", () => {
  assert.deepEqual(evaluateHttpRedirect("app.example.test", { status: 308, location: "https://app.example.test/" }).failures, []);
  assert.match(evaluateHttpRedirect("app.example.test", { status: 200, location: null }).failures[0], /expected one of 301\/302\/307\/308/);
  assert.match(evaluateHttpRedirect("app.example.test", { status: 302, location: "https://other.example.test/" }).failures[0], /expected the same host/);
  assert.match(evaluateHttpRedirect("app.example.test", { status: 301, location: "http://app.example.test/" }).failures[0], /expected https/);
  assert.match(evaluateHttpRedirect("app.example.test", { status: 302, location: null }).failures[0], /valid Location/);
});

test("unsafe-method evaluation requires 405 for TRACE and CONNECT without an accepted tunnel", () => {
  const allowed = evaluateUnsafeMethods("app.example.test", [
    { method: "TRACE", path: "/", status: 405, tunnelEstablished: false },
    { method: "TRACE", path: "/api/modules/authentication/profile", status: 405, tunnelEstablished: false },
    { method: "CONNECT", path: "/", status: 405, tunnelEstablished: false },
  ]);
  assert.deepEqual(allowed.failures, []);
  assert.deepEqual(allowed.requests.map((request) => request.result), ["PASS", "PASS", "PASS"]);

  const rejected = evaluateUnsafeMethods("app.example.test", [
    { method: "TRACE", path: "/", status: 200, tunnelEstablished: false },
    { method: "CONNECT", path: "/", status: 200, tunnelEstablished: true },
  ]);
  assert.equal(rejected.failures.length, 2);
  assert.match(rejected.failures[0], /TRACE \/ returned 200/);
  assert.match(rejected.failures[1], /CONNECT \/ returned 200 and a tunnel was established/);

  const omitted = evaluateUnsafeMethods("app.example.test", [
    { method: "TRACE", path: "/", status: 405, tunnelEstablished: false },
    { method: "CONNECT", path: "/", omitted: true, errorMessage: "the client refused" },
  ]);
  assert.deepEqual(omitted.failures, []);
  assert.equal(omitted.requests[1].result, "NOT TESTABLE");
  assert.match(omitted.requests[1].note, /CONNECT was omitted/);
  assert.match(omitted.note, /and was omitted/);

  const omittedTrace = evaluateUnsafeMethods("app.example.test", [{ method: "TRACE", path: "/", omitted: true, errorMessage: "not issued" }]);
  assert.match(omittedTrace.failures[0], /TRACE \/ could not be issued/);
});

test("port-scan evaluation expects exactly the ingress allowlist", () => {
  const allowed = evaluatePortScan("203.0.113.10", [
    { port: 22, state: "open" },
    { port: 80, state: "open" },
    { port: 443, state: "open" },
    { port: 3306, state: "closed" },
  ]);
  assert.deepEqual(allowed.failures, []);
  assert.deepEqual(allowed.ports.map(({ expected, result }) => ({ expected, result })), [
    { expected: "open", result: "PASS" },
    { expected: "open", result: "PASS" },
    { expected: "open", result: "PASS" },
    { expected: "closed", result: "PASS" },
  ]);

  const unexpected = evaluatePortScan("203.0.113.10", [
    { port: 22, state: "open" },
    { port: 80, state: "open" },
    { port: 443, state: "open" },
    { port: 3306, state: "open" },
  ]);
  assert.match(unexpected.failures[0], /port 3306 is open/);

  const missing = evaluatePortScan("203.0.113.10", [
    { port: 22, state: "open" },
    { port: 80, state: "open" },
    { port: 443, state: "filtered" },
  ]);
  assert.match(missing.failures[0], /port 443 is filtered; expected it to be open/);
});

test("timedatectl output is parsed into synchronisation, clock, and timezone fields", () => {
  const output = [
    "NTPSynchronized=yes",
    "TimeUSec=Wed 2026-10-07 12:00:00 UTC",
    "Timezone=Etc/UTC",
    "       Server: 91.189.91.157 (ntp.ubuntu.com)",
    "        Offset: -0.042276s",
    "1791374400.500000000",
  ].join("\n");
  assert.deepEqual(parseTimedatectlShow(output), {
    ntpSynchronized: "yes",
    timeUsec: "Wed 2026-10-07 12:00:00 UTC",
    timezone: "Etc/UTC",
  });
  assert.deepEqual(parseTimeSyncStatus(output), {
    source: "timedatectl",
    server: "91.189.91.157 (ntp.ubuntu.com)",
    offset: "-0.042276s",
  });
  assert.equal(parseServerEpochSeconds(output), 1791374400.5);
});

test("chronyc tracking output is parsed when systemd-timesyncd is unavailable", () => {
  const output = [
    "Reference ID    : C0A80101 (ntp.example.test)",
    "System time     : 0.000123456 seconds fast of NTP time",
    "Last offset     : +0.000045678 seconds",
    "1791374400.500000000",
  ].join("\n");
  assert.deepEqual(parseTimeSyncStatus(output), {
    source: "chronyc",
    server: "C0A80101 (ntp.example.test)",
    offset: "+0.000045678 seconds",
  });
});

test("time-sync evaluation computes the runner difference and enforces NTP with under five seconds", () => {
  const summary = summarizeTimeSync({
    output: ["NTPSynchronized=yes", "TimeUSec=Wed 2026-10-07 12:00:00 UTC", "Timezone=Etc/UTC", "1791374400.500000000"].join("\n"),
    startedAt: "2026-10-07T12:00:00.000Z",
    finishedAt: "2026-10-07T12:00:00.200Z",
  });
  assert.equal(summary.ntpSynchronized, "yes");
  assert.equal(summary.clockDifferenceSeconds, 0.4);
  assert.deepEqual(evaluateTimeSync(summary).failures, []);

  assert.match(evaluateTimeSync({ ntpSynchronized: "no", clockDifferenceSeconds: 0.1 }).failures[0], /expected "yes"/);
  assert.match(evaluateTimeSync({ ntpSynchronized: "yes", clockDifferenceSeconds: 5.5 }).failures[0], /differs from the runner/);
  assert.match(evaluateTimeSync({ ntpSynchronized: "yes", clockDifferenceSeconds: null }).failures[0], /could not be determined/);
  assert.equal(evaluateTimeSync({ ntpSynchronized: null, clockDifferenceSeconds: null }).failures.length, 2);
});

test("the HTTP redirect probe records the status and Location without following it", async (t) => {
  const server = createHttpServer((request, response) => {
    assert.equal(request.method, "GET");
    assert.equal(request.headers.host, "app.example.test");
    response.writeHead(308, { Location: "https://app.example.test/" });
    response.end();
  });
  await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
  t.after(() => new Promise((resolve) => server.close(resolve)));
  const port = server.address().port;
  const result = await probeHttpRedirect({ host: "127.0.0.1", port, hostname: "app.example.test" });
  assert.equal(result.status, 308);
  assert.equal(result.location, "https://app.example.test/");
  assert.deepEqual(evaluateHttpRedirect("app.example.test", result).failures, []);
});

test("TCP port scanning reports open and closed ports with one result per requested port", async (t) => {
  const openServer = createTcpServer();
  await new Promise((resolve) => openServer.listen(0, "127.0.0.1", resolve));
  t.after(() => new Promise((resolve) => openServer.close(resolve)));
  const openPort = openServer.address().port;
  const closedProbeServer = createTcpServer();
  await new Promise((resolve) => closedProbeServer.listen(0, "127.0.0.1", resolve));
  const closedPort = closedProbeServer.address().port;
  await new Promise((resolve) => closedProbeServer.close(resolve));

  const results = await scanTcpPorts({ host: "127.0.0.1", ports: [openPort, closedPort], timeoutMs: 2000, concurrency: 2 });
  assert.deepEqual(results, [
    { port: openPort, state: "open" },
    { port: closedPort, state: "closed", errorCode: "ECONNREFUSED" },
  ]);
});

test("TLS probing reports a refused connection as a server-side rejection, not a client limitation", async () => {
  const server = createTcpServer();
  await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
  const port = server.address().port;
  await new Promise((resolve) => server.close(resolve));
  const result = await probeTlsProtocol({ host: "127.0.0.1", port, servername: "app.example.test", minVersion: "TLSv1.2", maxVersion: "TLSv1.2" });
  assert.equal(result.accepted, false);
  assert.equal(result.clientCannotOffer, false);
  assert.equal(result.errorCode, "ECONNREFUSED");
});
