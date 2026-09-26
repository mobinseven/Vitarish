import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { resolveProductionImages } from "./resolve-production-images.mjs";

const digest = `sha256:${"a".repeat(64)}`;
const releaseInput = {
  images: ["main"], services: ["main"], owner: "mobinseven", commit: "b".repeat(40),
  runId: "12345", readinessPath: "/health/ready",
};

test("the deployed image is the registry digest published by this workflow run", () => {
  const release = resolveProductionImages(releaseInput, () => digest, () => digest);
  assert.equal(release.environment, `VITARISH_PRODUCTION_MAIN_IMAGE=ghcr.io/mobinseven/vitarish/main@${digest}\n`);
  assert.deepEqual(release.plan.images, [{ name: "main", variable: "VITARISH_PRODUCTION_MAIN_IMAGE", reference: `ghcr.io/mobinseven/vitarish/main@${digest}` }]);
  assert.match(readFileSync("src/Hosts/deployment/docker-compose.production.yml", "utf8"), /\$\{VITARISH_PRODUCTION_MAIN_IMAGE:/);
});

test("a moved SHA tag or missing digest fails before SSH deployment", () => {
  assert.throws(() => resolveProductionImages(releaseInput, () => digest, () => `sha256:${"c".repeat(64)}`), /does not match/);
  assert.throws(() => resolveProductionImages(releaseInput, () => "", () => digest), /does not match/);
});

test("a configuration-only release records its commit without inventing an image tag", () => {
  const release = resolveProductionImages({ ...releaseInput, images: [], services: ["caddy"] },
    () => { throw new Error("No image artifact should be read."); },
    () => { throw new Error("No image tag should be inspected."); });
  assert.equal(release.environment, "");
  assert.deepEqual(release.plan.images, []);
  assert.equal(release.plan.commit, releaseInput.commit);
});
