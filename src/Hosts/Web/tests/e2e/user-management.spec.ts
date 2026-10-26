import { expect, test, type Page } from "@playwright/test";
import { execFile } from "node:child_process";
import { existsSync } from "node:fs";
import { rm } from "node:fs/promises";
import { promisify } from "node:util";
import { resolve } from "node:path";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { Routes as PlatformRoutes } from "../../apps/platform-admin/src/app/routes";
import { signInToClinic, submitPasswordOnSignInHost } from "./signInHost";
import { authenticationLogoutPost } from "../../shared/auth/src/api/generated/authentication/authentication";
import type { SecurityAuthenticationPolicyDto } from "../../shared/auth/src/api/generated/authentication/model";
import { resetAntiforgeryToken, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { UsersCommandErrorKind } from "../../shared/auth/src/users";
import { localizePlatformAuditCategory, localizePlatformAuditEvent, localizeSecurityAuditOutcome } from "../../shared/security/src/localization";
import {
  twoHostE2eApiOriginEnvironmentKey,
  twoHostE2eProofScreenshotEnvironmentKey,
  twoHostE2eSecurityPolicyScreenshotEnvironmentKey,
  twoHostE2eProofStorageStateEnvironmentKey,
} from "../../scripts/two-host-e2e-artifacts.mjs";

const platformOrigin = process.env.PLAYWRIGHT_PLATFORM_ADMIN_ORIGIN ?? "http://localhost:3006";
const webOrigin = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3005";
const password = process.env.VITARISH_E2E_PASSWORD;
const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const platformUsername = process.env.VITARISH_E2E_USER_MANAGEMENT_PLATFORM_ADMIN_USERNAME ?? "e2e-user-management-admin";
// Main itself, for API-boundary checks made without the browser.
const apiOrigin = process.env[twoHostE2eApiOriginEnvironmentKey] ?? "https://localhost:7272";
// In-page requests use the web origin's same-origin /api, as in production.
const browserApiOrigin = "";
const execFileAsync = promisify(execFile);

async function captureAuthenticatedScreenshot(page: Page, outputPath: string): Promise<void> {
  const storageStatePath = process.env[twoHostE2eProofStorageStateEnvironmentKey];
  expect(storageStatePath, `${twoHostE2eProofStorageStateEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();
  expect(existsSync(outputPath), `refusing to overwrite retained evidence at ${outputPath}`).toBe(false);
  try {
    await page.context().storageState({ path: storageStatePath! });
    await execFileAsync("powershell.exe", [
      "-NoProfile",
      "-File",
      resolve(process.cwd(), "scripts", "screenshot.ps1"),
      "-Url",
      page.url(),
      "-OutputPath",
      outputPath,
      "-StorageStatePath",
      storageStatePath!,
      // The restored session must actually render the authenticated detail
      // page (this test's fixture name); never retain a login-page capture.
      "-WaitForSelector",
      "text=Security Evidence",
      "-IgnoreHttpsErrors",
    ], { cwd: process.cwd(), timeout: 45_000, windowsHide: true });
    expect(existsSync(outputPath), `the screenshot script did not create ${outputPath}`).toBe(true);
  } finally {
    await rm(storageStatePath!, { force: true });
  }
}

async function signOutThroughPublicApi(page: Page): Promise<void> {
  const tenantHost = new URL(page.url()).host;
  const originalFetch = globalThis.fetch;
  let responseStatus = 0;
  resetAntiforgeryToken();
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const headers = new Headers(init?.headers);
    headers.set(TENANT_HOST_HEADER, tenantHost);
    const response = await page.request.fetch(input.toString(), {
      method: init?.method,
      headers: Object.fromEntries(headers.entries()),
      data: typeof init?.body === "string" ? init.body : undefined,
      ignoreHTTPSErrors: true,
    });
    responseStatus = response.status();
    return new Response(await response.body(), {
      status: response.status(),
      headers: { "Content-Type": response.headers()["content-type"] ?? "application/json" },
    });
  }) as typeof fetch;

  try {
    const result = await authenticationLogoutPost(
      { logoutId: null, returnUrl: null },
      { headers: { [TENANT_HOST_HEADER]: tenantHost } },
    );
    expect(responseStatus).toBe(200);
    expect(result).toMatchObject({ signedOut: true });
  } finally {
    globalThis.fetch = originalFetch;
  }
}

async function putAuthenticationPolicy(page: Page, request: Record<string, unknown>): Promise<number> {
  return page.evaluate(async ({ origin, body, tenantHost }) => {
    const antiforgeryResponse = await fetch(`${origin}/api/modules/authentication/antiforgery`, {
      credentials: "include",
      headers: { ["X-Vitarish-Tenant-Host"]: tenantHost },
    });
    const antiforgery = await antiforgeryResponse.json() as { headerName: string; requestToken: string };
    const response = await fetch(`${origin}/api/modules/authentication/security/authentication-policy`, {
      method: "PUT",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        ["X-Vitarish-Tenant-Host"]: tenantHost,
        [antiforgery.headerName]: antiforgery.requestToken,
      },
      body: JSON.stringify(body),
    });
    return response.status;
  }, { origin: browserApiOrigin, body: request, tenantHost: new URL(page.url()).host });
}

async function login(page: Page, username: string, origin = ""): Promise<void> {
  expect(password, "VITARISH_E2E_PASSWORD must be supplied by the disposable E2E environment").toBeTruthy();
  // Clinic hosts sign in only on the shared sign-in host; platform administration has its own sign-in.
  if (origin !== platformOrigin) {
    await signInToClinic(page, username, password!, origin || webOrigin);
    return;
  }
  await page.goto(`${origin}${ClinicRoutes.Login.Password}`, { waitUntil: "domcontentloaded" });
  await expect(page.locator("form")).toHaveAttribute("method", "post");
  await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: 30_000 });
  await page.locator('input[name="username"]').fill(username);
  await page.locator('input[name="password"]').fill(password!);
  const [response] = await Promise.all([
    page.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/password")),
    page.locator('button[type="submit"]').click(),
  ]);
  expect(response.status()).toBe(200);
}

async function reviewUserAuditEvent(page: Page, category: string, eventName: string, operation: string): Promise<void> {
  await page.getByRole("button", { name: "Clear filters" }).click();
  await page.getByRole("button", { name: "All categories" }).click();
  await page.getByRole("option", { name: localizePlatformAuditCategory("en", category, category), exact: true }).click();
  await page.getByRole("button", { name: "All event types" }).click();
  await page.getByRole("option", { name: localizePlatformAuditEvent("en", eventName, eventName), exact: true }).click();
  await page.getByRole("button", { name: "All operations" }).click();
  await page.getByRole("option", { name: operation, exact: true }).click();
  await page.getByRole("textbox", { name: "Outcome" }).fill("succeeded");
  await page.getByRole("button", { name: "Apply filters" }).click();
  const row = page.locator("tbody tr").first();
  await expect(row).toBeVisible({ timeout: 30_000 });
  await expect(row.getByText(localizePlatformAuditEvent("en", eventName, eventName), { exact: true })).toBeVisible();
  await expect(row.getByText(operation, { exact: true })).toBeVisible();
  await expect(row.getByText(localizeSecurityAuditOutcome("en", "succeeded", "succeeded"), { exact: true })).toBeVisible();
}

test("authenticated clinic owner manages tenant staff through the shared surface", async ({ page }) => {
  await login(page, ownerUsername);
  await page.goto(ClinicRoutes.Clinic.Settings.Users, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Clinic Users", exact: true })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByRole("heading", { name: "Add staff member", exact: true })).toBeVisible();
  await expect(page.getByText(/^Owner ·/)).toBeVisible();
  await expect(page.getByText("Role to grant", { exact: true })).toHaveCount(0);

  const contact = `e2e-staff-${Date.now()}@example.test`;
  await page.getByRole("textbox", { name: "Email address" }).fill(contact);
  const refreshedMembersResponse = page.waitForResponse((response) =>
    response.request().method() === "GET" && response.url().includes("/api/modules/users/tenant-members"),
  );
  await page.getByRole("button", { name: "Add staff member" }).click();
  await expect(page.getByText("The account is now a tenant member.", { exact: true })).toBeVisible();
  const refreshedMembers = await refreshedMembersResponse.then((response) => response.json()) as Array<{ user?: { email?: string | null } }>;
  expect(refreshedMembers.some((member) => member.user?.email === contact)).toBe(true);
  const contactLocator = page.getByText(contact, { exact: false });
  await expect(contactLocator).toBeVisible();

});

test("authenticated platform administrator retains global search and membership inspection", async ({ page }) => {
  await login(page, platformUsername, platformOrigin);
  await page.goto(`${platformOrigin}${PlatformRoutes.Users.Index}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Users", exact: true })).toBeVisible({ timeout: 30_000 });
  await page.getByRole("textbox", { name: "Search users" }).fill(ownerUsername);
  await page.getByRole("button", { name: "Search" }).click();
  await expect(page.locator("tbody tr").first()).toBeVisible({ timeout: 30_000 });
  await page.locator("tbody tr").first().click();
  await expect(page.getByRole("heading", { name: "Tenant memberships", exact: true })).toBeVisible();

  await page.getByRole("textbox", { name: "Search users" }).fill(platformUsername);
  await page.getByRole("button", { name: "Search" }).click();
  const platformAdminRow = page.locator("tbody tr").filter({ hasText: platformUsername }).first();
  await expect(platformAdminRow).toBeVisible({ timeout: 30_000 });
  await platformAdminRow.getByRole("button", { name: "View user security details" }).click();
  await expect(page.getByText("User ID", { exact: true })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByRole("heading", { name: "Global roles", exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Authentication history", exact: true })).toBeVisible();
  const authenticationHistory = page.locator("section").filter({
    has: page.getByRole("heading", { name: "Authentication history", exact: true }),
  });
  await expect(authenticationHistory.getByText("Password login", { exact: true })).toBeVisible();
  const passwordLoginEvent = authenticationHistory.getByRole("row").filter({ hasText: "Password login" }).first();
  await expect(passwordLoginEvent.getByText(new URL(platformOrigin).hostname, { exact: true })).toBeVisible();
  await signOutThroughPublicApi(page);
});

async function endAllUserSessionsThroughUi(page: Page, username: string): Promise<number> {
  await page.goto(`${platformOrigin}${PlatformRoutes.Users.Index}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Users", exact: true })).toBeVisible({ timeout: 30_000 });
  await page.getByRole("textbox", { name: "Search users" }).fill(username);
  await page.getByRole("button", { name: "Search" }).click();
  const userRow = page.locator("tbody tr").filter({ hasText: username }).first();
  await expect(userRow).toBeVisible({ timeout: 30_000 });
  await userRow.getByRole("button", { name: "View user security details" }).click();
  await expect(page.getByText("User ID", { exact: true })).toBeVisible({ timeout: 30_000 });

  await page.getByRole("button", { name: "End all sessions" }).click();
  const confirmation = page.getByRole("dialog");
  await expect(confirmation).toHaveAccessibleName("End all sessions");
  const [revocationResponse] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "POST" && response.url().includes("/sessions/revoke")),
    confirmation.getByRole("button", { name: "End sessions" }).click(),
  ]);
  expect(revocationResponse.status()).toBe(200);
  await expect(page.getByRole("status").filter({ hasText: "Active sessions ended" })).toBeVisible();
  const revocation = await revocationResponse.json() as { endedSessions?: number | string };
  return Number(revocation.endedSessions);
}

async function setAccountStatusThroughUi(page: Page, username: string, enabled: boolean): Promise<void> {
  await page.goto(`${platformOrigin}${PlatformRoutes.Users.Index}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Users", exact: true })).toBeVisible({ timeout: 30_000 });
  await page.getByRole("textbox", { name: "Search users" }).fill(username);
  await page.getByRole("button", { name: "Search" }).click();
  const userRow = page.locator("tbody tr").filter({ hasText: username }).first();
  await expect(userRow).toBeVisible({ timeout: 30_000 });
  await userRow.getByRole("button", { name: "View user security details" }).click();
  await expect(page.getByText("User ID", { exact: true })).toBeVisible({ timeout: 30_000 });

  const actionName = enabled ? "Enable account" : "Disable account";
  const accountStatus = page.locator("section").filter({
    has: page.getByRole("heading", { name: "Account status", exact: true }),
  });
  await expect(accountStatus.getByText(enabled ? "Disabled" : "Enabled", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: actionName }).click();
  const confirmation = page.getByRole("dialog");
  await expect(confirmation).toHaveAccessibleName(actionName);
  const [statusResponse] = await Promise.all([
    page.waitForResponse((response) =>
      response.request().method() === "POST" && response.url().endsWith(`/${enabled ? "enable" : "disable"}`)),
    confirmation.getByRole("button", { name: actionName }).click(),
  ]);
  expect(statusResponse.status()).toBe(200);
  await expect(page.getByRole("status").filter({ hasText: enabled ? "The account is enabled." : "The account is disabled." })).toBeVisible();
  await expect(accountStatus.getByText(enabled ? "Enabled" : "Disabled", { exact: true })).toBeVisible();
}

test("platform administrator disables and re-enables a signed-out user's account", async ({ page }) => {
  const browser = page.context().browser();
  expect(browser, "the two-host browser is required for an isolated user session").toBeTruthy();
  expect(password, "VITARISH_E2E_PASSWORD must be supplied by the disposable E2E environment").toBeTruthy();
  // Own the PasswordLogin rate-limit partition; the shared client IP is
  // exhausted by the earlier login scenarios.
  await page.setExtraHTTPHeaders({ "X-Forwarded-For": "198.51.100.232" });
  await login(page, platformUsername, platformOrigin);
  // Earlier scenarios leave the owner signed in elsewhere while the evaluated
  // concurrent-session limit is active; a disable needs a zero-session owner.
  await endAllUserSessionsThroughUi(page, ownerUsername);
  await setAccountStatusThroughUi(page, ownerUsername, false);

  const userContext = await browser!.newContext({ extraHTTPHeaders: { "X-Forwarded-For": "198.51.100.233" } });
  const userPage = await userContext.newPage();
  let failure: unknown;
  let accountReenabled = false;
  try {
    const disabledLogin = await submitPasswordOnSignInHost(userPage, ownerUsername, password!);
    expect(disabledLogin.status()).toBe(403);
    await expect(userPage.getByText("This account is disabled. Contact a platform administrator.")).toBeVisible();

    await setAccountStatusThroughUi(page, ownerUsername, true);
    accountReenabled = true;

    await login(userPage, ownerUsername, webOrigin);
    await signOutThroughPublicApi(userPage);
    // Release the administrator session too; the concurrent-session limit is
    // active and the following scenarios sign the same administrator in again.
    await signOutThroughPublicApi(page);
  } catch (error) {
    failure = error;
  } finally {
    // A failed sign-in assertion must not leave the shared owner disabled for later specs.
    if (!accountReenabled) {
      try {
        await setAccountStatusThroughUi(page, ownerUsername, true);
      } catch (cleanupError) {
        failure = failure
          ? new AggregateError([failure, cleanupError], "The disabled-account scenario and account restoration both failed.")
          : cleanupError;
      }
    }
  }

  try {
    await userContext.close();
  } catch (cleanupError) {
    if (failure) {
      throw new AggregateError([failure, cleanupError], "The disabled-account scenario and its browser-context cleanup both failed.");
    }
    throw cleanupError;
  }
  if (failure) {
    throw failure;
  }
});

test("platform administrator ends a signed-in user's sessions and the user's next API call is rejected", async ({ page }) => {
  const browser = page.context().browser();
  expect(browser, "the two-host browser is required for an isolated user session").toBeTruthy();
  await login(page, platformUsername, platformOrigin);
  // Earlier scenarios leave the owner signed in elsewhere while the evaluated
  // concurrent-session limit is active; start from a known zero-session state.
  await endAllUserSessionsThroughUi(page, ownerUsername);

  const userContext = await browser!.newContext();
  const userPage = await userContext.newPage();
  let failure: unknown;
  try {
    await login(userPage, ownerUsername, webOrigin);
    const userTenantHost = new URL(userPage.url()).host;
    const signedInSession = await userPage.request.get(`${apiOrigin}/api/modules/authentication/session`, {
      headers: { [TENANT_HOST_HEADER]: userTenantHost },
      ignoreHTTPSErrors: true,
    });
    expect(signedInSession.status()).toBe(200);

    expect(await endAllUserSessionsThroughUi(page, ownerUsername)).toBeGreaterThanOrEqual(1);

    const revokedSession = await userPage.request.get(`${apiOrigin}/api/modules/authentication/session`, {
      headers: { [TENANT_HOST_HEADER]: userTenantHost },
      ignoreHTTPSErrors: true,
    });
    expect(revokedSession.status()).toBe(401);
  } catch (error) {
    failure = error;
  }

  try {
    await userContext.close();
  } catch (cleanupError) {
    if (failure) {
      throw new AggregateError([failure, cleanupError], "The revoked-session scenario and its browser-context cleanup both failed.");
    }
    throw cleanupError;
  }
  if (failure) {
    throw failure;
  }
});

test("platform administrator manages profile-constrained authentication policy through Settings", async ({ page }) => {
  await login(page, platformUsername, platformOrigin);
  await page.goto(`${platformOrigin}/settings/security`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Authentication policy", exact: true })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByRole("spinbutton", { name: "Failed authentication threshold", exact: true })).toHaveValue("5");
  await expect(page.getByText("Required by deployment profile").first()).toBeVisible();

  const loadedPolicyResponse = await page.request.get(`${apiOrigin}/api/modules/authentication/security/authentication-policy`, {
    headers: { [TENANT_HOST_HEADER]: new URL(platformOrigin).host },
    ignoreHTTPSErrors: true,
  });
  expect(loadedPolicyResponse.status()).toBe(200);
  const originalPolicy = await loadedPolicyResponse.json() as SecurityAuthenticationPolicyDto;
  const threshold = page.getByRole("spinbutton", { name: "Failed authentication threshold", exact: true });
  await threshold.fill("6");
  const [policyResponse] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "PUT" && response.url().endsWith("/api/modules/authentication/security/authentication-policy")),
    page.getByRole("button", { name: "Save authentication policy" }).click(),
  ]);
  expect(policyResponse.status()).toBe(200);
  const updatedPolicy = await policyResponse.json() as SecurityAuthenticationPolicyDto;
  expect(updatedPolicy.failedAttemptThreshold).toBe(6);
  await expect(page.getByRole("status").filter({ hasText: "Security policy saved." })).toBeVisible();

  const securityPolicyScreenshot = process.env[twoHostE2eSecurityPolicyScreenshotEnvironmentKey];
  if (securityPolicyScreenshot) {
    // The page above is already authenticated and saved; capture it in place.
    expect(existsSync(securityPolicyScreenshot), `refusing to overwrite retained evidence at ${securityPolicyScreenshot}`).toBe(false);
    await page.screenshot({ path: securityPolicyScreenshot, fullPage: true });
  }

  await page.reload({ waitUntil: "domcontentloaded" });
  const reloadedThreshold = page.getByRole("spinbutton", { name: "Failed authentication threshold", exact: true });
  await expect(reloadedThreshold).toHaveValue("6");
  const invalidUpdateStatus = await putAuthenticationPolicy(page, {
    minimumPasswordLength: originalPolicy.minimumPasswordLength,
    requirePasswordLowercase: originalPolicy.requirePasswordLowercase,
    requirePasswordUppercase: originalPolicy.requirePasswordUppercase,
    requirePasswordNumber: originalPolicy.requirePasswordNumber,
    requirePasswordSpecialCharacter: originalPolicy.requirePasswordSpecialCharacter,
    failedAttemptThreshold: 0,
    failedAttemptDenialDurationSeconds: originalPolicy.failedAttemptDenialDurationSeconds,
    failedAttemptResponse: originalPolicy.failedAttemptResponse,
    enabledAuthenticationMechanisms: originalPolicy.enabledAuthenticationMechanisms,
    permittedPreAuthenticationActions: originalPolicy.permittedPreAuthenticationActions,
    expectedVersion: updatedPolicy.version,
  });
  expect(invalidUpdateStatus).toBe(400);
  await page.reload({ waitUntil: "domcontentloaded" });
  await expect(page.getByRole("spinbutton", { name: "Failed authentication threshold", exact: true })).toHaveValue("6");

  await page.getByRole("spinbutton", { name: "Failed authentication threshold", exact: true }).fill(String(originalPolicy.failedAttemptThreshold));
  const [restoreResponse] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "PUT" && response.url().endsWith("/api/modules/authentication/security/authentication-policy")),
    page.getByRole("button", { name: "Save authentication policy" }).click(),
  ]);
  expect(restoreResponse.status()).toBe(200);
  await expect(page.getByRole("spinbutton", { name: "Failed authentication threshold", exact: true })).toHaveValue(String(originalPolicy.failedAttemptThreshold));

  await page.goto(`${platformOrigin}${PlatformRoutes.Audit}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Security event stream", exact: true })).toBeVisible({ timeout: 30_000 });
  await reviewUserAuditEvent(page, "authentication", "authentication.security-access-policy", "Authentication_SecurityAccessPolicy_Change");
  await signOutThroughPublicApi(page);
});

test("platform administrator creates, edits, assigns access, and reviews a user's security history", async ({ page }) => {
  await login(page, platformUsername, platformOrigin);

  await page.goto(`${platformOrigin}${PlatformRoutes.Users.Index}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Users", exact: true })).toBeVisible({ timeout: 30_000 });
  await page.getByRole("textbox", { name: "Search users" }).fill(ownerUsername);
  await page.getByRole("button", { name: "Search" }).click();
  const ownerRow = page.locator("tbody tr").filter({ hasText: ownerUsername }).first();
  await expect(ownerRow).toBeVisible({ timeout: 30_000 });
  await ownerRow.click();
  const memberships = page.locator("section").filter({ has: page.getByRole("heading", { name: "Tenant memberships", exact: true }) });
  const firstMembership = memberships.locator("li").first();
  await expect(firstMembership).toBeVisible();
  const tenantId = (await firstMembership.innerText()).match(/[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}/i)?.[0];
  expect(tenantId, "the E2E owner must have a tenant membership for role-assignment proof").toBeTruthy();

  const originalContact = `e2e-security-${Date.now()}@example.test`;
  await page.goto(`${platformOrigin}${PlatformRoutes.Users.New}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Create user", exact: true })).toBeVisible({ timeout: 30_000 });
  await page.getByRole("textbox", { name: "Email address" }).fill(originalContact);
  await page.getByRole("textbox", { name: "First name" }).fill("Security");
  await page.getByRole("textbox", { name: "Last name" }).fill("Evidence");
  const [createResponse] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "POST" && response.request().postData()?.includes(originalContact) === true),
    page.getByRole("button", { name: "Create user" }).click(),
  ]);
  expect(createResponse.ok()).toBe(true);
  const createdUser = await createResponse.json() as { id?: string };
  expect(createdUser.id).toBeTruthy();

  const duplicateEmail = page.getByRole("textbox", { name: "Email address" });
  await duplicateEmail.fill(originalContact);
  const [duplicateResponse] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "POST" && response.request().postData()?.includes(originalContact) === true),
    page.getByRole("button", { name: "Create user" }).click(),
  ]);
  expect(duplicateResponse.status()).toBe(409);
  const duplicateBody = await duplicateResponse.json() as { errors?: Array<{ failureCode?: string | null }> };
  expect(duplicateBody.errors?.some((error) => error.failureCode === UsersCommandErrorKind.ContactIdentifierInUse)).toBe(true);
  await expect(page.getByRole("status").filter({ hasText: "Another account already uses this email address or phone number." })).toBeVisible();
  await expect(duplicateEmail).toHaveValue(originalContact);

  const changedContact = `e2e-security-edited-${Date.now()}@example.test`;
  await page.goto(`${platformOrigin}${PlatformRoutes.Users.Detail(createdUser.id!)}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByText(createdUser.id!, { exact: true })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByText("No roles are assigned.", { exact: true })).toBeVisible();
  await expect(page.getByText("No authentication attempts have been recorded for this user.", { exact: true })).toBeVisible();

  await page.getByLabel("Global role to grant").selectOption("PlatformAdmin");
  const [globalRoleResponse] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "POST" && response.url().includes(`/api/modules/users/${createdUser.id}/roles`)),
    page.getByRole("button", { name: "Grant global role" }).click(),
  ]);
  expect(globalRoleResponse.ok()).toBe(true);
  await expect(page.getByText("PlatformAdmin", { exact: true })).toBeVisible();

  const inactivityInput = page.getByRole("spinbutton", { name: "Inactivity timeout (minutes)" });
  await inactivityInput.fill("15");
  const [inactivityResponse] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "PUT" && response.url().includes(`/api/modules/authentication/security/users/${createdUser.id}/inactivity-timeout`)),
    page.getByRole("button", { name: "Save inactivity timeout" }).click(),
  ]);
  expect(inactivityResponse.ok()).toBe(true);
  await expect(page.getByRole("status").filter({ hasText: "User inactivity timeout updated." })).toBeVisible();
  await page.reload({ waitUntil: "domcontentloaded" });
  await expect(page.getByRole("spinbutton", { name: "Inactivity timeout (minutes)" })).toHaveValue("15");

  await page.getByRole("spinbutton", { name: "Inactivity timeout (minutes)" }).fill("");
  const [inheritResponse] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "PUT" && response.url().includes(`/api/modules/authentication/security/users/${createdUser.id}/inactivity-timeout`)),
    page.getByRole("button", { name: "Save inactivity timeout" }).click(),
  ]);
  expect(inheritResponse.ok()).toBe(true);

  // Each saved timeout records security events, so these push the security history past
  // its first page of 25 and give the pager something real to page through.
  for (const minutes of [16, 17, 18, 19, 20, 21, 22, 23]) {
    await page.getByRole("spinbutton", { name: "Inactivity timeout (minutes)" }).fill(String(minutes));
    // The save refetches the stored timeout; wait for it so it cannot overwrite the next value typed.
    const [timeoutResponse] = await Promise.all([
      page.waitForResponse((response) => response.request().method() === "PUT" && response.url().includes(`/api/modules/authentication/security/users/${createdUser.id}/inactivity-timeout`)),
      page.waitForResponse((response) => response.request().method() === "GET" && response.url().includes(`/api/modules/authentication/security/users/${createdUser.id}/inactivity-timeout`)),
      page.getByRole("button", { name: "Save inactivity timeout" }).click(),
    ]);
    expect(timeoutResponse.ok()).toBe(true);
    await expect(page.getByRole("spinbutton", { name: "Inactivity timeout (minutes)" })).toHaveValue(String(minutes));
  }

  const stalePage = await page.context().newPage();
  await stalePage.goto(`${platformOrigin}${PlatformRoutes.Users.Detail(createdUser.id!)}`, { waitUntil: "domcontentloaded" });
  await expect(stalePage.getByText(createdUser.id!, { exact: true })).toBeVisible({ timeout: 30_000 });
  await page.getByRole("textbox", { name: "Email address" }).fill(changedContact);
  const [editResponse] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "PUT" && response.request().postData()?.includes(changedContact) === true),
    page.getByRole("button", { name: "Save user changes" }).click(),
  ]);
  expect(editResponse.ok()).toBe(true);
  await expect(page.getByRole("status").filter({ hasText: "User details were updated." })).toBeVisible();

  const staleContact = `e2e-security-stale-${Date.now()}@example.test`;
  await stalePage.getByRole("textbox", { name: "Email address" }).fill(staleContact);
  const [conflictResponse] = await Promise.all([
    stalePage.waitForResponse((response) => response.request().method() === "PUT" && response.request().postData()?.includes(staleContact) === true),
    stalePage.getByRole("button", { name: "Save user changes" }).click(),
  ]);
  expect(conflictResponse.status()).toBe(409);
  await expect(stalePage.getByRole("status").filter({ hasText: "This account changed elsewhere." })).toBeVisible();
  await expect(stalePage.getByRole("textbox", { name: "Email address" })).toHaveValue(changedContact);
  await stalePage.close();

  await page.getByRole("textbox", { name: "Tenant ID" }).fill(tenantId!);
  const [grantResponse] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "PUT" && response.request().url().includes(createdUser.id!)),
    page.getByRole("button", { name: "Grant role" }).click(),
  ]);
  expect(grantResponse.ok()).toBe(true);
  await expect(page.getByText(new RegExp(`Tenant: ${tenantId} · Role: TenantUser`))).toBeVisible();

  const browser = page.context().browser();
  expect(browser, "the two-host browser is required for an unauthenticated login attempt").toBeTruthy();
  const anonymousContext = await browser!.newContext();
  let loginFailure: unknown;
  try {
    const anonymousPage = await anonymousContext.newPage();
    await anonymousPage.goto(`${platformOrigin}${ClinicRoutes.Login.Password}`, { waitUntil: "domcontentloaded" });
    await expect(anonymousPage.locator("form")).toHaveAttribute("method", "post");
    await expect(anonymousPage.locator('button[type="submit"]')).toBeEnabled({ timeout: 30_000 });
    const usernameInput = anonymousPage.getByRole("textbox", { name: "Username or email" });
    const passwordInput = anonymousPage.locator('input[name="password"]');
    await usernameInput.fill(changedContact);
    await passwordInput.fill("invalid-e2e-password");
    await expect(usernameInput).toHaveValue(changedContact);
    await expect(passwordInput).toHaveValue("invalid-e2e-password");
    const [failedLogin] = await Promise.all([
      anonymousPage.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/password")),
      anonymousPage.locator('button[type="submit"]').click(),
    ]);
    expect(failedLogin.status()).toBe(401);
  } catch (error) {
    loginFailure = error;
  }

  try {
    await anonymousContext.close();
  } catch (cleanupError) {
    if (loginFailure) {
      throw new AggregateError([loginFailure, cleanupError], "The anonymous login scenario and its browser-context cleanup both failed.");
    }
    throw cleanupError;
  }
  if (loginFailure) {
    throw loginFailure;
  }

  await page.reload({ waitUntil: "domcontentloaded" });
  const authenticationHistory = page.locator("section").filter({ has: page.getByRole("heading", { name: "Authentication history", exact: true }) });
  await expect(authenticationHistory.getByText("Password login", { exact: true })).toBeVisible({ timeout: 30_000 });
  const failedAttempt = authenticationHistory.getByRole("row").filter({ hasText: "Password login" }).first();
  await expect(failedAttempt.getByText("Denied", { exact: true })).toBeVisible();
  await expect(failedAttempt.getByText(new URL(platformOrigin).hostname, { exact: true })).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Email address" })).toHaveValue(changedContact);

  const securityHistory = page.locator("section").filter({ has: page.getByRole("heading", { name: "Security history", exact: true }) });
  await expect(securityHistory.getByText(/^Page 1 of [2-9]$/)).toBeVisible();
  await expect(authenticationHistory.getByRole("button", { name: "Older events" })).toHaveCount(0);

  // The server reports which authentication data the account holds: the changed
  // email is no longer verified and no passkey has been registered.
  const authenticationMethods = page.locator("section").filter({ has: page.getByRole("heading", { name: "Authentication methods", exact: true }) });
  await expect(authenticationMethods.getByRole("definition").nth(1)).toHaveText("Not verified");
  await expect(authenticationMethods.getByText("No passkey is registered.", { exact: true })).toBeVisible();

  const proofScreenshot = process.env[twoHostE2eProofScreenshotEnvironmentKey];
  if (proofScreenshot) await captureAuthenticatedScreenshot(page, proofScreenshot);

  // The security history pages on its own: the request moves only its page, and the
  // sign-in history stays on its first page.
  const firstSecurityEvent = await securityHistory.getByRole("row").nth(1).textContent();
  const [olderSecurityResponse] = await Promise.all([
    page.waitForResponse((response) => response.request().method() === "GET" && response.url().includes(`/api/modules/users/administration/${createdUser.id}/security`) && response.url().includes("page=2")),
    securityHistory.getByRole("button", { name: "Older events" }).click(),
  ]);
  expect(olderSecurityResponse.ok()).toBe(true);
  const olderSecurityUrl = new URL(olderSecurityResponse.url());
  expect(olderSecurityUrl.searchParams.get("page")).toBe("2");
  expect(olderSecurityUrl.searchParams.get("authenticationPage")).toBe("1");
  await expect(securityHistory.getByText(/^Page 2 of [2-9]$/)).toBeVisible();
  await expect(securityHistory.getByRole("row").nth(1)).not.toHaveText(firstSecurityEvent ?? "");
  await expect(failedAttempt.getByText("Denied", { exact: true })).toBeVisible();

  await page.goto(`${platformOrigin}${PlatformRoutes.Audit}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Security event stream", exact: true })).toBeVisible({ timeout: 30_000 });
  await reviewUserAuditEvent(page, "management", "users.account.created", "users.account.create");
  await reviewUserAuditEvent(page, "management", "users.account.edited", "users.account.edit");
  await reviewUserAuditEvent(page, "security-attributes", "users.security-attributes.changed", "users.security-attributes.change");
  await signOutThroughPublicApi(page);
});
