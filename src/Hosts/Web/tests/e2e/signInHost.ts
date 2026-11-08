import { expect, type Page, type Response } from "@playwright/test";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { LandingRoutes } from "../../apps/vitarish-landing/src/app/routes";
import { twoHostE2eClinicOrigin, twoHostE2eForwardedForHeaderName as forwardedForHeaderName, twoHostE2eLandingOrigin, twoHostE2eSignInOrigin as signInOrigin } from "../../scripts/two-host-e2e-artifacts.mjs";
import { authenticationAntiforgeryGet, authenticationLoginPassword, getAuthenticationSessionGetQueryKey } from "../../shared/auth/src/api/generated/authentication/authentication";
import { REQUEST_ID_HEADER, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { landingIssueWorkspaceHandoff, landingListWorkspaces } from "../../shared/platform/src/api/generated/landing/landing";
import type { LandingWorkspaceHandoffResponse, LandingWorkspacesResponse } from "../../shared/platform/src/api/generated/landing/model";
import { generatedOperationPath } from "./generatedOperationPath";

/** The E2E clinic's own host. */
export const defaultClinicOrigin = twoHostE2eClinicOrigin;
export { forwardedForHeaderName };
const handoffTimeoutMilliseconds = 120_000;
// The real endpoint permits ten requests per user per minute. A newly authenticated browser may
// first send a stale antiforgery token, then make one protected retry; allow at most five such
// E2E sign-ins per minute without changing the policy or retrying a sign-in operation.
const signInHandoffTestPacingMilliseconds = 12_500;
const lastSignInCompletedAtByUsername = new Map<string, number>();
const passwordLoginPath = generatedOperationPath(authenticationLoginPassword);
const antiforgeryPath = generatedOperationPath(authenticationAntiforgeryGet);
const sessionPath = getAuthenticationSessionGetQueryKey()[0];
const landingWorkspacesPath = generatedOperationPath(landingListWorkspaces);
const landingWorkspaceHandoffPathTemplate = generatedOperationPath(landingIssueWorkspaceHandoff);

/** Submits the password form on the sign-in host and returns the sign-in response. */
export async function submitPasswordOnSignInHost(page: Page, username: string, password: string): Promise<Response> {
  await page.goto(`${signInOrigin}${Routes.Login.Password}`, { waitUntil: "domcontentloaded" });
  await expect(page.locator("form")).toHaveAttribute("method", "post", { timeout: 60_000 });
  await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: 60_000 });
  await page.locator('input[name="username"]').fill(username);
  await page.locator('input[name="password"]').fill(password);
  const [response] = await Promise.all([
    page.waitForResponse((candidate) => candidate.url().endsWith(passwordLoginPath) && candidate.request().method() === "POST"),
    page.locator('button[type="submit"]').click(),
  ]);
  return response;
}

/**
 * Follows a signed-in sign-in host through landing home to the clinic at `clinicOrigin`.
 * Resolves once the landing-issued workspace handoff is redeemed.
 */
export async function continueToClinic(page: Page, clinicOrigin = defaultClinicOrigin): Promise<void> {
  const requestedClinic = new URL(clinicOrigin);
  const requestedClinicOrigin = requestedClinic.origin;
  const requestedClinicHostname = requestedClinic.hostname.toLowerCase();

  // The two-host runner serves the owner app at landing.localhost; the browser page still
  // carries the landing-host session and can exercise its real tenant-scoped API.
  await page.waitForURL((url) => url.origin === twoHostE2eLandingOrigin && url.pathname === LandingRoutes.PlatformHome, {
    waitUntil: "domcontentloaded",
    timeout: handoffTimeoutMilliseconds,
  });
  await expect.poll(async () => page.evaluate(async ({ tenantHostHeader, path }) => {
    const response = await fetch(path, {
      credentials: "include",
      headers: { [tenantHostHeader]: window.location.host },
    });
    return response.status;
  }, { tenantHostHeader: TENANT_HOST_HEADER, path: sessionPath }), {
    timeout: handoffTimeoutMilliseconds,
  }).toBe(200);

  const directoryResult = await page.evaluate(async ({ tenantHostHeader, requestIdHeader, path }) => {
    const response = await fetch(path, {
      credentials: "include",
      headers: {
        [tenantHostHeader]: window.location.host,
        [requestIdHeader]: `e2e-workspace-list-${crypto.randomUUID()}`,
      },
    });
    return { status: response.status, body: response.ok ? await response.json() : null };
  }, { tenantHostHeader: TENANT_HOST_HEADER, requestIdHeader: REQUEST_ID_HEADER, path: landingWorkspacesPath });
  expect(directoryResult.status, "the signed-in landing workspace directory must be available").toBe(200);
  const directory = directoryResult.body as LandingWorkspacesResponse | null;
  expect(directory, "the landing workspace response must include its directory").not.toBeNull();
  const workspace = directory!.workspaces.find((candidate) => candidate.hostname.toLowerCase() === requestedClinicHostname);
  expect(workspace, `the signed-in person must have a workspace at ${requestedClinicHostname}`).toBeDefined();

  const workspaceHandoffPath = landingWorkspaceHandoffPathTemplate.replace(
    /\$\{[^}]+\}/,
    encodeURIComponent(workspace!.tenantId),
  );
  const handoffResult = await page.evaluate(async ({ tenantHostHeader, requestIdHeader, antiforgeryRequestPath, path }) => {
    const tenantHeaders = { [tenantHostHeader]: window.location.host };
    const antiforgeryResponse = await fetch(antiforgeryRequestPath, { credentials: "include", headers: tenantHeaders });
    if (!antiforgeryResponse.ok) return { status: antiforgeryResponse.status, body: null };

    const antiforgery = (await antiforgeryResponse.json()) as { headerName: string; requestToken: string };
    const response = await fetch(path, {
      method: "POST",
      credentials: "include",
      headers: {
        ...tenantHeaders,
        [requestIdHeader]: `e2e-workspace-handoff-${crypto.randomUUID()}`,
        [antiforgery.headerName]: antiforgery.requestToken,
      },
    });
    return { status: response.status, body: response.ok ? await response.json() : null };
  }, {
    tenantHostHeader: TENANT_HOST_HEADER,
    requestIdHeader: REQUEST_ID_HEADER,
    antiforgeryRequestPath: antiforgeryPath,
    path: workspaceHandoffPath,
  });
  expect(handoffResult.status, "the landing host must issue the requested workspace handoff").toBe(200);
  const handoff = handoffResult.body as LandingWorkspaceHandoffResponse | null;
  expect(handoff?.hostname.toLowerCase()).toBe(requestedClinicHostname);
  expect(handoff?.handoffUrl).toBeTruthy();
  expect(handoff?.token).toBe("");
  await page.goto(handoff!.handoffUrl!, { waitUntil: "domcontentloaded", timeout: handoffTimeoutMilliseconds });
  await page.waitForURL((url) => url.origin === requestedClinicOrigin && url.pathname !== Routes.Handoff && !url.pathname.startsWith(Routes.Login.Index), {
    waitUntil: "domcontentloaded",
    timeout: handoffTimeoutMilliseconds,
  });
  // The URL changes before the destination journey/session gate finishes. Its dashboard heading
  // confirms that initialization is complete before a caller starts another navigation.
  await expect(page.getByRole("main").getByRole("heading", { level: 1 })).toBeVisible({
    timeout: handoffTimeoutMilliseconds,
  });
}

/** Signs in with a password on the sign-in host and lands signed in on the clinic at `clinicOrigin`. */
export async function signInToClinic(page: Page, username: string, password: string, clinicOrigin = defaultClinicOrigin): Promise<void> {
  const pacingKey = username.trim().toLowerCase();
  const previousSignInCompletedAt = lastSignInCompletedAtByUsername.get(pacingKey);
  if (previousSignInCompletedAt !== undefined) {
    const waitMilliseconds = previousSignInCompletedAt + signInHandoffTestPacingMilliseconds - Date.now();
    if (waitMilliseconds > 0) await new Promise((resolve) => setTimeout(resolve, waitMilliseconds));
  }

  try {
    const response = await submitPasswordOnSignInHost(page, username, password);
    expect(response.status()).toBe(200);
    await continueToClinic(page, clinicOrigin);
  } finally {
    lastSignInCompletedAtByUsername.set(pacingKey, Date.now());
  }
}
