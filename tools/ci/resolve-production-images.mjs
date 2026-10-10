import { execFileSync } from "node:child_process";
import { readFileSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const imageVariables = Object.freeze({
  caddy: "VITARISH_CADDY_IMAGE",
  main: "VITARISH_PRODUCTION_MAIN_IMAGE",
  "private-practice-owner": "VITARISH_PRODUCTION_OWNER_WEB_IMAGE",
  patients: "VITARISH_PRODUCTION_PATIENTS_WEB_IMAGE",
  "platform-admin": "VITARISH_PRODUCTION_PLATFORM_ADMIN_WEB_IMAGE",
  pm: "VITARISH_PRODUCTION_PM_WEB_IMAGE",
  landing: "VITARISH_PRODUCTION_LANDING_WEB_IMAGE",
  "agent-runtime": "VITARISH_PRODUCTION_AGENT_RUNTIME_IMAGE",
});

const digestPattern = /^sha256:[0-9a-f]{64}$/;
const shaPattern = /^[0-9a-f]{40}$/;

export function resolveProductionImages({ images, services, owner, commit, runId, readinessPath }, readPublishedDigest, inspectPublishedTag) {
  if (!shaPattern.test(commit) || !/^[1-9][0-9]*$/.test(String(runId)) ||
      !/^[A-Za-z0-9-]+$/.test(owner) || !/^\/[A-Za-z0-9/_-]+$/.test(readinessPath)) {
    throw new Error("Invalid production release identity or readiness path.");
  }
  if (!Array.isArray(images) || !Array.isArray(services) || services.length === 0 ||
      new Set(images).size !== images.length || new Set(services).size !== services.length ||
      images.some((name) => !imageVariables[name]) || services.some((name) => !imageVariables[name])) {
    throw new Error("Invalid production image or service selection.");
  }

  const references = images.map((name) => {
    const tag = `ghcr.io/${owner.toLowerCase()}/vitarish/${name}:${commit}`;
    const publishedDigest = readPublishedDigest(name);
    const registryDigest = inspectPublishedTag(tag);
    if (!digestPattern.test(publishedDigest) || publishedDigest === `sha256:${"0".repeat(64)}` ||
        publishedDigest !== registryDigest) {
      throw new Error(`Published registry digest does not match this run's ${name} image.`);
    }
    return { name, variable: imageVariables[name], reference: `${tag.slice(0, tag.lastIndexOf(":"))}@${publishedDigest}` };
  });

  return {
    environment: references.map(({ variable, reference }) => `${variable}=${reference}`).join("\n") + (references.length ? "\n" : ""),
    plan: { commit, runId: String(runId), services, readinessPath, images: references.map(({ name, variable, reference }) => ({ name, variable, reference })) },
  };
}

if (resolve(process.argv[1] ?? "") === fileURLToPath(import.meta.url)) {
  const artifactRoot = process.env.IMAGE_DIGEST_DIRECTORY;
  const outputRoot = process.env.RELEASE_OUTPUT_DIRECTORY;
  if (!artifactRoot || !outputRoot) throw new Error("Production image artifact and output directories are required.");
  const release = resolveProductionImages({
    images: JSON.parse(process.env.IMAGE_MATRIX),
    services: JSON.parse(process.env.SERVICE_MATRIX),
    owner: process.env.GITHUB_REPOSITORY_OWNER,
    commit: process.env.COMMIT_SHA,
    runId: process.env.GITHUB_RUN_ID,
    readinessPath: process.env.READINESS_PATH,
  },
  (name) => readFileSync(resolve(artifactRoot, `${name}.digest`), "utf8").trim(),
  (tag) => JSON.parse(execFileSync("docker", ["buildx", "imagetools", "inspect", tag, "--format", "{{json .Manifest.Digest}}"], { encoding: "utf8" }).trim()));
  writeFileSync(resolve(outputRoot, "release.env"), release.environment, { mode: 0o600 });
  writeFileSync(resolve(outputRoot, "plan.json"), `${JSON.stringify(release.plan)}\n`, { mode: 0o600 });
}
