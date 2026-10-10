import assert from "node:assert/strict";
import { readFileSync, readdirSync } from "node:fs";
import { matchesGlob } from "node:path";
import test from "node:test";
import { selectProductionMatrix } from "./select-production-matrix.mjs";

const workflow = readFileSync(new URL("../../.github/workflows/production.yml", import.meta.url), "utf8");
const caddyfile = readFileSync(new URL("../../src/Hosts/deployment/Caddyfile", import.meta.url), "utf8");
const deployProductionScript = readFileSync(new URL("../../src/Hosts/deployment/deploy-production.sh", import.meta.url), "utf8");
const mainHealthSource = readFileSync(new URL("../../src/Hosts/Main/Infrastructure/Health/MainDependencyHealthCheck.cs", import.meta.url), "utf8");
const authConsumerWorkspaces = readdirSync(new URL("../../src/Hosts/Web/apps/", import.meta.url), { withFileTypes: true })
  .filter((entry) => entry.isDirectory())
  .map((entry) => JSON.parse(readFileSync(new URL(`../../src/Hosts/Web/apps/${entry.name}/package.json`, import.meta.url), "utf8")))
  .filter((manifest) => manifest.dependencies?.["@vitarish/auth"])
  .map((manifest) => manifest.name.replace(/^@vitarish\//, ""))
  .sort();
const unknownSourceRules = /^            unknown_source:\r?\n((?:              - .*\r?\n)+)/m.exec(workflow)?.[1]
  ?.trim().split(/\r?\n/).map((line) => line.trim().slice(3).replace(/^'|'$/g, ""));
const deployScriptRules = /^            deploy_script:\r?\n((?:              - .*\r?\n)+)/m.exec(workflow)?.[1]
  ?.trim().split(/\r?\n/).map((line) => line.trim().slice(3).replace(/^'|'$/g, ""));

function matchesUnknownSource(path) {
  assert.match(workflow, /^          predicate-quantifier: some-with-excludes$/m);
  assert.ok(unknownSourceRules?.length, "unknown_source filter is missing");
  const included = unknownSourceRules.filter((rule) => !rule.startsWith("!"));
  const excluded = unknownSourceRules.filter((rule) => rule.startsWith("!")).map((rule) => rule.slice(1));
  return included.some((rule) => matchesGlob(path, rule)) && !excluded.some((rule) => matchesGlob(path, rule));
}

function matchesDeployScript(path) {
  assert.ok(deployScriptRules?.length, "deploy_script filter is missing");
  return deployScriptRules.some((rule) => matchesGlob(path, rule));
}

test("workflow path rules exclude classified source changes before selecting the fallback matrix", () => {
  const mainPath = "src/Hosts/Main/Infrastructure/Health/MainDependencyHealthCheck.cs";
  const ownerPath = "src/Hosts/Web/apps/private-practice-owner/src/app/page.tsx";
  const unknownPath = "src/Hosts/experimental/NewSource.cs";
  assert.equal(matchesUnknownSource(mainPath), false);
  assert.equal(matchesUnknownSource(ownerPath), false);
  assert.equal(matchesUnknownSource(unknownPath), true);
  assert.deepEqual(selectProductionMatrix("push", { main: true, web_all: true, unknown_source: matchesUnknownSource(mainPath) }).images,
    ["main", "private-practice-owner", "patients", "platform-admin", "pm", "landing", "agent-runtime"]);
  assert.equal(selectProductionMatrix("push", { unknown_source: matchesUnknownSource(unknownPath) }).services.length, 8);
});

test("configuration-only deployments remain eligible when publishing is skipped", () => {
  assert.match(workflow, /deploy_production:\r?\n[\s\S]*?if: \$\{\{ always\(\) && !cancelled\(\)[^\n]*needs\.required\.result == 'success'[^\n]*needs\.publish\.result == 'skipped'/);
  assert.deepEqual(selectProductionMatrix("push", { compose: true }).images, []);
});

test("publishes selected images when unrelated test matrices are skipped", () => {
  assert.match(workflow, /publish:\r?\n[\s\S]*?needs: \[changes\]/);
  assert.match(workflow, /publish:\r?\n[\s\S]*?if: \$\{\{ github\.event_name == 'push'[^\n]*needs\.changes\.outputs\.image_matrix != '\[\]'[^\n]*&& !cancelled\(\) \}\}/);
  assert.match(workflow, /deploy_production:\r?\n[\s\S]*?needs: \[changes, required, publish\][\s\S]*?needs\.required\.result == 'success'/);
});

test("deployment script changes select every production service without publishing application images", () => {
  const scriptPath = "src/Hosts/deployment/deploy-production.sh";
  assert.equal(matchesDeployScript(scriptPath), true);
  assert.equal(matchesDeployScript("src/Hosts/deployment/docker-compose.production.yml"), false);
  const selected = selectProductionMatrix("push", { deploy_script: matchesDeployScript(scriptPath) });
  assert.deepEqual(selected.images, []);
  assert.deepEqual(selected.services, [
    "main", "private-practice-owner", "patients", "platform-admin", "pm",
    "landing", "agent-runtime", "caddy",
  ]);
});

test("the remote Caddy validator does not consume the streamed deployment script", () => {
  assert.match(deployProductionScript, /^ingress run --interactive=false --rm --no-deps caddy-config-validator caddy validate --config \/etc\/caddy\/Caddyfile --adapter caddyfile$/m);
});

test("selected Caddy configuration is loaded and rollback reloads the previous configuration", () => {
  assert.match(deployProductionScript, /if selected caddy; then\r?\n\s+ingress up -d --wait --wait-timeout 600 --no-deps --force-recreate --pull never caddy/);
  assert.match(deployProductionScript, /if \[\[ \$attempted_caddy == true && \$previous_ingress == true \]\]; then ingress_previous up -d --wait --no-deps --force-recreate caddy/);
});

test("the production readiness probe does not require an unsupported curl retry option", () => {
  assert.doesNotMatch(deployProductionScript, /--retry-all-errors/);
});

test("the production readiness probe pins HTTPS to the local ingress origin", () => {
  assert.match(deployProductionScript, /--resolve "\$public_host:443:127\.0\.0\.1" --proto '=https' --tlsv1\.2[\s\S]*?"https:\/\/\$public_host\$readiness_path"/);
  assert.match(deployProductionScript, /Production readiness returned HTTP %s from %s/);
  assert.match(deployProductionScript, /compose exec -T patients node -e/);
  assert.match(deployProductionScript, /Private Main readiness returned %s/);
});

test("production ingress forwards the Main readiness contract to the private Main upstream", () => {
  const readinessPath = /public const string ReadinessPath = "([^\"]+)";/.exec(mainHealthSource)?.[1];
  assert.ok(readinessPath, "Main readiness route contract is missing");
  const healthRoute = /\(production_readiness\)\s*\{(?<body>[\s\S]*?)\}/.exec(caddyfile)?.groups?.body;
  assert.ok(healthRoute, "production readiness route is missing");
  assert.match(healthRoute, new RegExp(`handle ${readinessPath.replace(/[.*+?^${}()|[\\]\\\\]/g, "\\\\$&")}`));
  assert.match(healthRoute, /reverse_proxy production-main:8080/);
  assert.match(caddyfile, /https:\/\/\{\$PRODUCTION_PUBLIC_HOST\}\s*\{[^}]*import production_readiness/s);
});

test("an isolated Web app change selects only that app's tests, image, and service", () => {
  const selected = selectProductionMatrix("pull_request", { web_owner: true });
  assert.deepEqual(selected.web.map(({ workspace }) => workspace), ["private-practice-owner"]);
  assert.deepEqual(selected.images, ["private-practice-owner"]);
  assert.deepEqual(selected.services, ["private-practice-owner"]);
});

test("a shared authentication change selects every consuming app with shared auth checks", () => {
  const selected = selectProductionMatrix("push", { web_auth: true, web_landing: true });
  assert.equal(selected.web.length, authConsumerWorkspaces.length + 1);
  // The knowledge QA demo is checked but never deployed.
  assert.equal(selected.images.length, authConsumerWorkspaces.length);
  assert.equal(selected.services.length, authConsumerWorkspaces.length);
  assert.deepEqual(selected.web.map(({ workspace }) => workspace).sort(), [...authConsumerWorkspaces, "vitarish-landing"].sort());
  assert.match(workflow, /authentication_web:[\s\S]*?env:\r?\n          HEROUI_AUTH_TOKEN: \$\{\{ secrets\.HEROUI_AUTH_TOKEN \}\}/);
  assert.match(workflow, /npm ci/);
  assert.match(workflow, /authentication_web:[\s\S]*?if: \$\{\{ contains\(needs\.changes\.outputs\.filters, 'web_auth'\) \}\}/);
  assert.match(workflow, /needs: \[changes, dotnet, web, web_shared_tests, web_shared_validators, authentication_web, security\]/);
});

test("an authentication-only change selects every consuming app and shared auth", () => {
  const selected = selectProductionMatrix("push", { web_auth: true });
  assert.equal(selected.web.length, 5);
  assert.equal(selected.images.length, 4);
});

test("a shared Web change selects all Web consumers", () => {
  const selected = selectProductionMatrix("push", { web_all: true });
  assert.equal(selected.web.length, 7);
  assert.equal(selected.images.includes("main"), false);
  assert.equal(selected.images.includes("caddy"), false);
  assert.equal(selected.images.length, 6);
});

test("an API contract change selects Main and every Web consumer", () => {
  const selected = selectProductionMatrix("pull_request", { dotnet: true, main: true, web_all: true });
  assert.equal(selected.web.length, 7);
  assert.equal(selected.images[0], "main");
  assert.equal(selected.images.length, 7);
});

test("a Caddy change selects only the Caddy image and ingress service", () => {
  assert.match(workflow, /caddy:\r?\n\s+- 'src\/Hosts\/deployment\/Dockerfile'/);
  assert.doesNotMatch(workflow, /caddy:\r?\n(?:\s+- .*\r?\n)*\s+- 'src\/Hosts\/deployment\/Caddyfile'/);
  const selected = selectProductionMatrix("push", { caddy: true });
  assert.deepEqual(selected.images, ["caddy"]);
  assert.deepEqual(selected.services, ["caddy"]);
});

test("a Caddyfile-only change deploys ingress configuration without publishing an image", () => {
  assert.match(workflow, /caddy_config:\r?\n\s+- 'src\/Hosts\/deployment\/Caddyfile'/);
  const selected = selectProductionMatrix("push", { caddy_config: true });
  assert.deepEqual(selected.images, []);
  assert.deepEqual(selected.services, ["caddy"]);
});

test("an unclassified source change falls back to the broad matrix", () => {
  const selected = selectProductionMatrix("pull_request", { unknown_source: true });
  assert.equal(selected.web.length, 7);
  assert.equal(selected.images.includes("main"), true);
  assert.equal(selected.images.includes("caddy"), true);
  assert.equal(selected.services.length, 8);
});

test("a Compose change updates service configuration without publishing new images", () => {
  const selected = selectProductionMatrix("push", { compose: true });
  assert.equal(selected.images.length, 0);
  assert.deepEqual(selected.services, [
    "main", "private-practice-owner", "patients", "platform-admin", "pm",
    "landing", "agent-runtime", "caddy",
  ]);
});

test("a test Compose change runs checks without selecting production services", () => {
  const selected = selectProductionMatrix("push", { test_compose: true });
  assert.deepEqual(selected.web, []);
  assert.deepEqual(selected.images, []);
  assert.deepEqual(selected.services, []);
});

test("manual dispatch selects the full test suite without requesting a deploy", () => {
  const selected = selectProductionMatrix("workflow_dispatch", {});
  assert.equal(selected.full_suite, true);
  assert.equal(selected.web.length, 7);
  assert.equal(selected.images.length, 8);
  assert.equal(selected.services.length, 8);
});

test("workflow and policy edits run workflow checks without product matrices", () => {
  const selected = selectProductionMatrix("push", { workflow: true });
  assert.deepEqual(selected.web, []);
  assert.deepEqual(selected.images, []);
  assert.deepEqual(selected.services, []);
});

test("the knowledge QA demo is checked in CI but never published or deployed", () => {
  const selected = selectProductionMatrix("push", { web_qa: true });
  assert.deepEqual(selected.web.map(({ workspace }) => workspace), ["knowledge-qa-demo"]);
  assert.deepEqual(selected.images, []);
  assert.deepEqual(selected.services, []);
  const broad = selectProductionMatrix("workflow_dispatch", {});
  assert.equal(broad.images.includes("knowledge-qa"), false);
  assert.equal(broad.services.includes("knowledge-qa"), false);
});
