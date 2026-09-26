import { appendFileSync, readFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const webConsumers = [
  { service: "private-practice-owner", image: "private-practice-owner", workspace: "private-practice-owner", filter: "web_owner" },
  { service: "patients", image: "patients", workspace: "patients", filter: "web_patients" },
  { service: "platform-admin", image: "platform-admin", workspace: "platform-admin", filter: "web_admin" },
  { service: "pm", image: "pm", workspace: "vitarish-pm", filter: "web_pm" },
  { service: "knowledge-qa", image: "knowledge-qa", workspace: "knowledge-qa-demo", filter: "web_qa" },
  { service: "landing", image: "landing", workspace: "vitarish-landing", filter: "web_landing" },
  { service: "agent-runtime", image: "agent-runtime", workspace: "agent-runtime", filter: "web_agent" },
];

const sharedComposeConsumers = [
  "main", "private-practice-owner", "patients", "platform-admin", "pm",
  "knowledge-qa", "landing", "agent-runtime", "caddy",
];

export function selectProductionMatrix(eventName, filters) {
  const fullSuite = eventName === "workflow_dispatch";
  const broad = fullSuite || filters.unknown_source === true;
  const allWebImages = broad || filters.web_all === true;
  const webTests = webConsumers.filter((consumer) => allWebImages || filters[consumer.filter] === true);
  const webImages = webConsumers.filter((consumer) => allWebImages || filters[consumer.filter] === true);
  const main = broad || filters.main === true || filters.dotnet === true;
  const caddy = broad || filters.caddy === true;
  const images = [
    ...(main ? ["main"] : []),
    ...webImages.map((consumer) => consumer.image),
    ...(caddy ? ["caddy"] : []),
  ];
  let services = [
    ...(main ? ["main"] : []),
    ...webImages.map((consumer) => consumer.service),
    ...(caddy ? ["caddy"] : []),
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
  const filters = Object.fromEntries([
    "dotnet", "main", "web_all", "web_owner", "web_patients", "web_admin", "web_pm",
    "web_qa", "web_landing", "web_agent", "caddy", "compose", "deploy_script", "test_compose", "workflow", "unknown_source",
  ].map((name) => [name, process.env[name.toUpperCase()] === "true"]));
  const selected = selectProductionMatrix(process.env.EVENT_NAME, filters);
  const outputs = {
    web: JSON.stringify(selected.web),
    images: JSON.stringify(selected.images),
    services: JSON.stringify(selected.services),
    full_suite: String(selected.full_suite),
    readiness_path: readinessPathFromSource(),
  };
  appendFileSync(process.env.GITHUB_OUTPUT, `${Object.entries(outputs).map(([name, value]) => `${name}=${value}`).join("\n")}\n`);
}
