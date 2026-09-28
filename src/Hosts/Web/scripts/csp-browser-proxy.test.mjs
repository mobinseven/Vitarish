import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { createRequestPolicy, readIngressPolicy } from "./csp-browser-proxy.mjs";

const policyTemplate = readIngressPolicy();
const caddyPath = new URL("../../deployment/Caddyfile", import.meta.url);
const composePath = new URL("../../deployment/docker-compose.ingress.yml", import.meta.url);

test("uses the single ingress request policy for the Report-Only response", () => {
  assert.match(readFileSync(caddyPath, "utf8"), /request_header Content-Security-Policy "\{\$VITARISH_CONTENT_SECURITY_POLICY\}"/u);
  assert.match(readFileSync(caddyPath, "utf8"), /Content-Security-Policy-Report-Only "\{\$VITARISH_CONTENT_SECURITY_POLICY\}"/u);
  assert.match(policyTemplate, /style-src-elem 'self' 'nonce-\{http\.request\.uuid\}'/u);
  assert.match(policyTemplate, /img-src 'self' data: blob:/u);
  assert.match(policyTemplate, /media-src 'self' blob:/u);
  assert.match(policyTemplate, /frame-ancestors 'none'/u);
  assert.doesNotMatch(policyTemplate, /sha(?:256|384|512)-|\*|'unsafe-eval'/u);
});

test("gives each proxied document request one fresh nonce shared by script and style directives", () => {
  const first = createRequestPolicy(policyTemplate);
  const second = createRequestPolicy(policyTemplate);

  assert.notEqual(first.nonce, second.nonce);
  assert.ok(first.policy.includes(`script-src 'self' 'nonce-${first.nonce}'`));
  assert.ok(first.policy.includes(`style-src-elem 'self' 'nonce-${first.nonce}'`));
  assert.doesNotMatch(first.policy, /\{http\.request\.uuid\}/u);
});

test("rejects an enforcing response policy, an incidental hash, and a broad wildcard", () => {
  const caddy = readFileSync(caddyPath, "utf8");
  const compose = readFileSync(composePath, "utf8");

  assert.throws(() => readIngressPolicy(caddy.replace("Content-Security-Policy-Report-Only", "Content-Security-Policy"), compose), /Report-Only/u);
  assert.throws(() => readIngressPolicy(caddy, compose.replace("style-src-elem 'self' 'nonce-{http.request.uuid}'", "style-src-elem 'self' 'sha256-incidental'")), /style-src-elem/u);
  assert.throws(() => readIngressPolicy(caddy, compose.replace("img-src 'self' data: blob:; ", "")), /img-src/u);
  assert.throws(() => readIngressPolicy(caddy, compose.replace("frame-ancestors 'none'; ", "")), /frame-ancestors/u);
  assert.throws(() => readIngressPolicy(caddy, compose.replace("default-src 'self'", "default-src *")), /default-src/u);
});
