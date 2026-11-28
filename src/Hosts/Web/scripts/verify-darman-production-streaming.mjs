import { expect, chromium } from "@playwright/test";
import { writeFile } from "node:fs/promises";
import authentication from "../shared/auth/src/api/specs/Main_authentication.json" with { type: "json" };
import platform from "../src/api/specs/Main_landing.json" with { type: "json" };
import { operations } from "./agent-turn-benchmark.mjs";
import { AuthenticationWebRoutes } from "../shared/auth/src/routes.ts";
import { LandingRoutes } from "../apps/vitarish-landing/src/app/routes.ts";
import { homeMessageCatalogs } from "../apps/vitarish-landing/src/features/home/homeMessages.ts";
import { Routes as ClinicRoutes } from "../apps/private-practice-owner/src/app/routes.ts";
import { TENANT_HOST_HEADER } from "../shared/api-client/src/tenantHostHeader.ts";
import { REQUEST_ID_HEADER } from "../shared/api-client/src/requestIdHeader.ts";
import { readAgentTurnFrames } from "../shared/intelligence/src/api/intelligence/agentTurnFrames.ts";

const authOrigin = "https://auth.vitarish.com";
const landingOrigin = "https://vitarish.com";
const smokeAccount = "smoke-one-clinic";
const workspaceName = "Vitarish Smoke Clinic";
const practiceSubject = "practice";
const helpAgentKey = "clinic.assistant.operations.help";
const expectedPracticeAgents = new Set(["clinic.assistant.operations", helpAgentKey, "clinic.operations"]);
const [commit, outputPath] = process.argv.slice(2);

function operationPath(spec, method, schema) {
  const reference = "#/components/schemas/" + schema;
  const matches = Object.entries(spec.paths).filter(([, path]) =>
    Object.values(path[method]?.responses?.["200"]?.content ?? {}).some((media) => media.schema?.$ref === reference));
  if (matches.length !== 1) throw new Error("The smoke test operation is missing or ambiguous.");
  return matches[0][0];
}

const directoryPath = operationPath(platform, "get", "LandingWorkspacesResponse");
const logoutPath = operationPath(authentication, "post", "LogoutResultDto");

async function main() {
  const password = process.env.VITARISH_SMOKE_PASSWORD_ONE_CLINIC;
  if (!password || !/^[0-9a-f]{40}$/.test(commit ?? "") || !outputPath) {
    throw new Error("Smoke credentials and release identity are required.");
  }
  let stage = "browser startup";
  const browser = await chromium.launch();
  try {
    const context = await browser.newContext({ locale: "en-US" });
    const page = await context.newPage();
    page.setDefaultTimeout(30_000);

    stage = "central password sign-in";
    const directoryResponse = page.waitForResponse((response) =>
      response.url() === landingOrigin + directoryPath && response.request().method() === "GET" && response.ok(), { timeout: 60_000 });
    // Attach a rejection handler while the UI navigates before awaiting the response.
    void directoryResponse.catch(() => {});
    await page.goto(authOrigin + AuthenticationWebRoutes.SignInPassword, { waitUntil: "domcontentloaded" });
    await expect(page.locator("form")).toHaveAttribute("method", "post");
    await page.locator('input[name="username"]').fill(smokeAccount);
    await page.locator('input[name="password"]').fill(password);
    await page.locator('button[type="submit"]').click();
    await page.waitForURL(landingOrigin + LandingRoutes.PlatformHome, { timeout: 60_000, waitUntil: "domcontentloaded" });

    stage = "isolate the smoke workspace";
    const directory = await (await directoryResponse).json();
    if (!Array.isArray(directory.workspaces) || directory.workspaces.length !== 1 ||
        directory.workspaces[0].name !== workspaceName) {
      throw new Error("Unexpected smoke-account workspace directory.");
    }
    const clinicOrigin = new URL("https://" + directory.workspaces[0].hostname).origin;
    if (clinicOrigin === authOrigin || clinicOrigin === landingOrigin) throw new Error("Unexpected smoke clinic origin.");
    const clinicHost = new URL(clinicOrigin).host;
    const openName = homeMessageCatalogs.en.home.workspaces.openNamed.replace("{name}", workspaceName);
    await page.getByRole("button", { name: openName, exact: true }).click();
    await page.waitForURL((url) => url.origin === clinicOrigin && url.pathname === ClinicRoutes.Home, {
      waitUntil: "domcontentloaded",
      timeout: 60_000,
    });
    await page.goto(clinicOrigin + ClinicRoutes.Clinic.Assistant, { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("main").getByRole("heading", { level: 1 })).toBeVisible();

    async function post(path, body, headers = {}) {
      const tokenResponse = await context.request.get(clinicOrigin + operations.antiforgery.path, {
        headers: { [TENANT_HOST_HEADER]: clinicHost }, maxRedirects: 0,
      });
      if (!tokenResponse.ok()) throw new Error("Antiforgery response failed.");
      const token = await tokenResponse.json();
      const cookie = (await context.cookies(clinicOrigin + path)).map(({ name, value }) => name + "=" + value).join("; ");
      return fetch(clinicOrigin + path, {
        method: "POST",
        redirect: "error",
        signal: AbortSignal.timeout(45_000),
        headers: { origin: clinicOrigin, cookie, [TENANT_HOST_HEADER]: clinicHost, [REQUEST_ID_HEADER]: crypto.randomUUID(),
          [token.headerName]: token.requestToken, "content-type": "application/json", ...headers },
        body: JSON.stringify(body),
      });
    }

    let evidence;
    try {
      stage = "authenticated streaming capabilities";
      const response = await context.request.get(clinicOrigin + operations.capabilities.path, {
        headers: { [TENANT_HOST_HEADER]: clinicHost }, maxRedirects: 0,
      });
      if (!response.ok()) throw new Error("Capabilities response failed.");
      const { agents } = await response.json();
      const enabled = agents.filter((agent) => agent.streamingEnabled);
      const practice = enabled.filter((agent) => agent.subjectKind === practiceSubject && expectedPracticeAgents.has(agent.agentKey));
      if (practice.length !== expectedPracticeAgents.size || new Set(practice.map((agent) => agent.agentKey)).size !== practice.length) {
        throw new Error("Expected Darman streaming capabilities were missing.");
      }

      stage = "live help stream";
      const help = practice.find((agent) => agent.agentKey === helpAgentKey);
      const start = performance.now();
      const streamed = await post(operations.streamed.path, {
        agentKey: help.agentKey, subjectKind: help.subjectKind, subjectId: practiceSubject,
        message: "How do I book a patient into an existing shift?",
      }, { accept: operations.streamed.mediaType, "accept-language": "en" });
      if (!streamed.ok || !streamed.body || streamed.headers.get("content-type")?.split(";")[0].trim() !== operations.streamed.mediaType) {
        throw new Error("Expected an authenticated NDJSON response.");
      }
      let firstTextMs;
      let terminalMs;
      let terminal;
      for await (const frame of readAgentTurnFrames(streamed.body)) {
        if (frame.kind === "delta" && frame.text.trim()) firstTextMs ??= performance.now() - start;
        if (frame.kind === "completed") {
          terminalMs = performance.now() - start;
          terminal = frame.response;
        }
      }
      if (!Number.isFinite(firstTextMs) || !Number.isFinite(terminalMs) || firstTextMs >= terminalMs ||
          !terminal?.agentRunId || terminal.result?.actions?.length !== 0 ||
          !terminal.result.messages?.some((message) => /book|appointment/i.test(message.content))) {
        throw new Error("Expected useful help text before successful completion without actions.");
      }
      evidence = { commit, authenticated: true, capabilitiesVerified: practice.length,
        firstHelpTextMs: Math.round(firstTextMs), terminalMs: Math.round(terminalMs) };
    } finally {
      stage = evidence ? "smoke-account sign-out" : stage;
      const signedOut = await post(logoutPath, { logoutId: null, returnUrl: null });
      if (!signedOut.ok || (await signedOut.json()).signedOut !== true) throw new Error("Smoke-account sign-out failed.");
    }

    stage = "write sanitized release evidence";
    await writeFile(outputPath, JSON.stringify(evidence, null, 2) + "\n", { flag: "wx" });
    console.log(JSON.stringify(evidence));
  } catch {
    throw new Error("Authenticated Darman production verification failed during " + stage + ".");
  } finally {
    await browser.close();
  }
}

if (commit !== "--check") await main();
