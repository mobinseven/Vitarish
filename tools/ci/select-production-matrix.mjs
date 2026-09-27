import { appendFileSync, readFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const platformAdminCommitScope = "platform-admin";
const platformAdminFilter = "web_admin";
const webAuthFilter = "web_auth";

const webConsumers = [
  { service: "private-practice-owner", image: "private-practice-owner", workspace: "private-practice-owner", filter: "web_owner" },
  { service: "patients", image: "patients", workspace: "patients", filter: "web_patients" },
  { service: "platform-admin", image: "platform-admin", workspace: "platform-admin", filter: platformAdminFilter },
  { service: "pm", image: "pm", workspace: "vitarish-pm", filter: "web_pm" },
  { service: "knowledge-qa", image: "knowledge-qa", workspace: "knowledge-qa-demo", filter: "web_qa" },
  { service: "landing", image: "landing", workspace: "vitarish-landing", filter: "web_landing" },
  { service: "agent-runtime", image: "agent-runtime", workspace: "agent-runtime", filter: "web_agent" },
];

const sharedComposeConsumers = [
  "main", "private-practice-owner", "patients", "platform-admin", "pm",
  "knowledge-qa", "landing", "agent-runtime", "caddy",
];

export function selectProductionMatrix(eventName, filters, changeTitle = "") {
  const fullSuite = eventName === "workflow_dispatch";
  const broad = fullSuite || filters.unknown_source === true;
  const appConsumers = webConsumers.filter((consumer) => filters[consumer.filter] === true);
  const authChanged = filters[webAuthFilter] === true;
  const commitScope = /^\w+\(([^)]+)\):/.exec(changeTitle.trim())?.[1];
  const selectedAppConsumers = authChanged && appConsumers.length === 0 && commitScope === platformAdminCommitScope
    ? webConsumers.filter((consumer) => consumer.filter === platformAdminFilter)
    : appConsumers;
  const allWebConsumers = broad || filters.web_all === true || (authChanged && selectedAppConsumers.length === 0);
  const webTests = webConsumers.filter((consumer) => allWebConsumers || selectedAppConsumers.includes(consumer));
  const allWebImages = broad || filters.web_all === true || (authChanged && selectedAppConsumers.length === 0);
  const webImages = webConsumers.filter((consumer) => allWebImages || selectedAppConsumers.includes(consumer));
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

  return {
    web: webTests.map(({ image, workspace, service }) => ({ image, workspace, service })),
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
  const selected = selectProductionMatrix(process.env.EVENT_NAME, filters, process.env.CHANGE_TITLE);
  const outputs = {
    web: JSON.stringify(selected.web),
    images: JSON.stringify(selected.images),
    services: JSON.stringify(selected.services),
    full_suite: String(selected.full_suite),
    readiness_path: readinessPathFromSource(),
  };
  appendFileSync(process.env.GITHUB_OUTPUT, `${Object.entries(outputs).map(([name, value]) => `${name}=${value}`).join("\n")}\n`);
}
