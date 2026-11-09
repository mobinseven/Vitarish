import { existsSync, mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import test from "node:test";
import assert from "node:assert/strict";

import {
  discardPrebuilt,
  fingerprintApp,
  fingerprintSharedInputs,
  isPrebuilt,
  markPrebuilt,
} from "./two-host-e2e-web-build-cache.mjs";

function createWebRoot() {
  const webRoot = mkdtempSync(join(tmpdir(), "e2e-build-cache-"));
  for (const directory of ["shared/ui/src", "src/api", "apps/owner/src"]) mkdirSync(join(webRoot, directory), { recursive: true });
  writeFileSync(join(webRoot, "package.json"), "{}");
  writeFileSync(join(webRoot, "package-lock.json"), "{}");
  writeFileSync(join(webRoot, "shared/ui/src/Button.tsx"), "export const Button = 1;");
  writeFileSync(join(webRoot, "apps/owner/src/page.tsx"), "export default 1;");
  return webRoot;
}

function fingerprints(webRoot, buildEnvironment = { NODE_ENV: "production" }) {
  const sharedFingerprint = fingerprintSharedInputs(webRoot);
  const appFingerprint = fingerprintApp({ webRoot, appRoot: join(webRoot, "apps/owner"), sharedFingerprint, buildEnvironment });
  return { sharedFingerprint, appFingerprint };
}

test("unchanged inputs give the same fingerprint, so the build is reused", () => {
  const webRoot = createWebRoot();
  try {
    assert.deepEqual(fingerprints(webRoot), fingerprints(webRoot));
  } finally {
    rmSync(webRoot, { recursive: true, force: true });
  }
});

test("a change to the app, a shared package, the lockfile or the build environment forces a rebuild", () => {
  const webRoot = createWebRoot();
  try {
    const before = fingerprints(webRoot);

    writeFileSync(join(webRoot, "apps/owner/src/page.tsx"), "export default 2;");
    assert.notEqual(fingerprints(webRoot).appFingerprint, before.appFingerprint);
    writeFileSync(join(webRoot, "apps/owner/src/page.tsx"), "export default 1;");
    assert.equal(fingerprints(webRoot).appFingerprint, before.appFingerprint);

    writeFileSync(join(webRoot, "shared/ui/src/Button.tsx"), "export const Button = 2;");
    assert.notEqual(fingerprints(webRoot).appFingerprint, before.appFingerprint);
    writeFileSync(join(webRoot, "shared/ui/src/Button.tsx"), "export const Button = 1;");

    writeFileSync(join(webRoot, "package-lock.json"), '{"changed":true}');
    assert.notEqual(fingerprints(webRoot).appFingerprint, before.appFingerprint);
    writeFileSync(join(webRoot, "package-lock.json"), "{}");

    assert.notEqual(fingerprints(webRoot, { NODE_ENV: "production", NEXT_PUBLIC_API_BASE_URL: "x" }).appFingerprint, before.appFingerprint);
  } finally {
    rmSync(webRoot, { recursive: true, force: true });
  }
});

test("generated API clients are part of the build, but tests, dependencies and Next output are not", () => {
  const webRoot = createWebRoot();
  try {
    const before = fingerprints(webRoot);

    mkdirSync(join(webRoot, "shared/ui/src/api/generated"), { recursive: true });
    writeFileSync(join(webRoot, "shared/ui/src/api/generated/client.ts"), "export const client = 1;");
    const withGeneratedClient = fingerprints(webRoot);
    assert.notEqual(withGeneratedClient.appFingerprint, before.appFingerprint);

    writeFileSync(join(webRoot, "shared/ui/src/Button.test.tsx"), "test");
    mkdirSync(join(webRoot, "shared/ui/src/__tests__"), { recursive: true });
    writeFileSync(join(webRoot, "shared/ui/src/__tests__/Button.tsx"), "test");
    mkdirSync(join(webRoot, "apps/owner/node_modules/dependency"), { recursive: true });
    writeFileSync(join(webRoot, "apps/owner/node_modules/dependency/index.js"), "x");
    mkdirSync(join(webRoot, "apps/owner/.next-two-host-e2e-prebuilt"), { recursive: true });
    writeFileSync(join(webRoot, "apps/owner/.next-two-host-e2e-prebuilt/BUILD_ID"), "id");
    writeFileSync(join(webRoot, "apps/owner/next-env.d.ts"), "// generated");
    assert.deepEqual(fingerprints(webRoot), withGeneratedClient);
  } finally {
    rmSync(webRoot, { recursive: true, force: true });
  }
});

test("a build is reused only when it finished and was made from the same inputs", () => {
  const webRoot = createWebRoot();
  const appRoot = join(webRoot, "apps/owner");
  const distPath = join(appRoot, ".next-two-host-e2e-prebuilt");
  try {
    assert.equal(isPrebuilt(distPath, "a"), false);

    mkdirSync(distPath, { recursive: true });
    markPrebuilt(distPath, "a");
    assert.equal(isPrebuilt(distPath, "a"), false, "output without a finished build must not be reused");

    writeFileSync(join(distPath, "BUILD_ID"), "id");
    assert.equal(isPrebuilt(distPath, "a"), true);
    assert.equal(isPrebuilt(distPath, "b"), false, "output made from other inputs must not be reused");

    discardPrebuilt(distPath, appRoot);
    assert.equal(existsSync(distPath), false);
  } finally {
    rmSync(webRoot, { recursive: true, force: true });
  }
});

test("discarding refuses a folder outside the owning app", () => {
  const webRoot = createWebRoot();
  try {
    assert.throws(() => discardPrebuilt(join(webRoot, "shared"), join(webRoot, "apps/owner")), /inside its owning app/);
    assert.equal(existsSync(join(webRoot, "shared")), true);
  } finally {
    rmSync(webRoot, { recursive: true, force: true });
  }
});
