import { appendFileSync, readFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const platformAdminFilter = "web_admin";
const webAuthFilter = "web_auth";

const webConsumers = [
  { service: "private-practice-owner", image: "private-practice-owner", workspace: "private-practice-owner", filter: "web_owner", authConsumer: true },
  { service: "patients", image: "patients", workspace: "patients", filter: "web_patients", authConsumer: true },
  { service: "platform-admin", image: "platform-admin", workspace: "platform-admin", filter: platformAdminFilter, authConsumer: true },
  { service: "pm", image: "pm", workspace: "vitarish-pm", filter: "web_pm", authConsumer: true },
  // Checked in CI but not deployed: the knowledge QA demo is opted out of production.
  { service: "knowledge-qa", image: "knowledge-qa", workspace: "knowledge-qa-demo", filter: "web_qa", authConsumer: true, deploy: false },
  { service: "landing", image: "landing", workspace: "vitarish-landing", filter: "web_landing" },
  { service: "agent-runtime", image: "agent-runtime", workspace: "agent-runtime", filter: "web_agent" },
];

const sharedComposeConsumers = [
  "main", "private-practice-owner", "patients", "platform-admin", "pm",
  "landing", "agent-runtime", "caddy",
];

// A manual release selects the changes since the deployed commit; without a
// usable deployed commit it falls back to the full suite and every image.
export function selectProductionMatrix(eventName, filters, deployedBase = "") {
  const fullSuite = eventName === "workflow_dispatch" && !deployedBase;
  const broad = fullSuite || filters.unknown_source === true;
  const authChanged = filters[webAuthFilter] === true;
  const selectedConsumers = webConsumers.filter((consumer) =>
    filters[consumer.filter] === true || (authChanged && consumer.authConsumer));
  const allWebConsumers = broad || filters.web_all === true;
  const webTests = webConsumers.filter((consumer) => allWebConsumers || selectedConsumers.includes(consumer));
  const webImages = webConsumers.filter((consumer) =>
    consumer.deploy !== false && (allWebConsumers || selectedConsumers.includes(consumer)));
  const main = broad || filters.main === true || filters.dotnet === true;
  const caddyImage = broad || filters.caddy === true;
  const caddyService = caddyImage || filters.caddy_config === true;
  const images = [
    ...(main ? ["main"] : []),
    ...webImages.map((consumer) => consumer.image),
    ...(caddyImage ? ["caddy"] : []),
  ];
  let services = [
    ...(main ? ["main"] : []),
    ...webImages.map((consumer) => consumer.service),
    ...(caddyService ? ["caddy"] : []),
  ];
  if (filters.compose === true || filters.deploy_script === true) services = [...sharedComposeConsumers];

  // The shared Web job typechecks and tests every workspace, so per-app jobs
  // then only build. Typechecks and tests miss server/client module boundary
  // errors that only `next build` reports, so every selected deployed app is
  // built; a release builds them in its image publish instead.
  const buildDeployedApps = eventName !== "workflow_dispatch";
  const web = webTests
    .map(({ image, workspace, service, deploy }) => ({
      image,
      workspace,
      service,
      checks: !allWebConsumers,
      build: buildDeployedApps && deploy !== false,
    }))
    .filter(({ checks, build }) => checks || build);

  return {
    web,
    images,
    services,
    full_suite: fullSuite,
  };
}

function readinessPathFromSource() {
  const source = readFileSync("src/Hosts/Main/Infrastructure/Health/MainDependencyHealthCheck.cs", "utf8");
  const path = /public const string ReadinessPath = "([^"]+)";/.exec(source)?.[1];
  if (!path?.startsWith("/")) throw new Error("Main readiness route constant could not be resolved.");
  return path;
}

if (process.env.GITHUB_OUTPUT && resolve(process.argv[1] ?? "") === fileURLToPath(import.meta.url)) {
  const changedFilters = JSON.parse(process.env.CHANGED_FILTERS || "[]");
  if (!Array.isArray(changedFilters) || changedFilters.some((name) => typeof name !== "string")) {
    throw new Error("Changed path filters must be a JSON array of names.");
  }
  const filters = Object.fromEntries(changedFilters.map((name) => [name, true]));
  const selected = selectProductionMatrix(process.env.EVENT_NAME, filters, process.env.DEPLOYED_BASE ?? "");
  const outputs = {
    web: JSON.stringify(selected.web),
    images: JSON.stringify(selected.images),
    services: JSON.stringify(selected.services),
    full_suite: String(selected.full_suite),
    readiness_path: readinessPathFromSource(),
  };
  appendFileSync(process.env.GITHUB_OUTPUT, `${Object.entries(outputs).map(([name, value]) => `${name}=${value}`).join("\n")}\n`);
}
