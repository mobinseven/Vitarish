import { expect, test, type Page } from "@playwright/test";
import { Routes as AdminRoutes } from "../../apps/platform-admin/src/app/routes";
import { Routes as PatientRoutes } from "../../apps/patients/src/app/routes";
import { Routes as ProjectRoutes } from "../../apps/vitarish-pm/src/app/routes";
import { Routes as KnowledgeRoutes } from "../../apps/knowledge-qa-demo/src/app/routes";
import { getMessages } from "../../apps/platform-admin/src/localization/messages";
import { AuthenticationWebRoutes } from "../../shared/auth/src/routes";
import { authenticationAntiforgeryGet, authenticationBrowserSessionsGet, authenticationLoginPassword, authenticationLogoutPost, authenticationSessionGet } from "../../shared/auth/src/api/generated/authentication/authentication";
import { TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { twoHostE2eForwardedForHeaderName, twoHostE2eKnowledgeOrigin, twoHostE2ePasswordEnvironmentKey, twoHostE2ePatientsOrigin, twoHostE2ePlatformAdminOrigin, twoHostE2eProjectManagementOrigin, twoHostE2eProofScreenshotEnvironmentKey, twoHostE2eSignInOrigin } from "../../scripts/two-host-e2e-artifacts.mjs";
import { generatedOperationPath } from "./generatedOperationPath";

async function post(page: Page, path: string, body: unknown) {
  return page.evaluate(async ({ path, body, tenantHeader, antiforgeryPath }) => {
    const headers = { [tenantHeader]: window.location.host };
    const anti = await (await fetch(antiforgeryPath, { headers, credentials: "include" })).json();
    return (await fetch(path, { method: "POST", credentials: "include", headers: { ...headers, "Content-Type": "application/json", [anti.headerName]: anti.requestToken }, body: JSON.stringify(body) })).status;
  }, { path, body, tenantHeader: TENANT_HOST_HEADER, antiforgeryPath: generatedOperationPath(authenticationAntiforgeryGet) });
}

test("one central browser signs in across apps and app logout revokes the whole family", async ({ browser }) => {
  test.setTimeout(360_000);
  const password = process.env[twoHostE2ePasswordEnvironmentKey];
  expect(password).toBeTruthy();
  const origins = [twoHostE2ePlatformAdminOrigin, twoHostE2ePatientsOrigin, twoHostE2eProjectManagementOrigin, twoHostE2eKnowledgeOrigin, twoHostE2eSignInOrigin];
  const context = await browser.newContext({ extraHTTPHeaders: { [twoHostE2eForwardedForHeaderName]: "198.51.100.202" } });
  await context.addCookies(origins.map(url => ({ name: localeCookieName, value: "en", url })));
  const page = await context.newPage();
  let passwordSubmissions = 0;
  page.on("request", request => { if (request.method() === "POST" && request.url().endsWith(generatedOperationPath(authenticationLoginPassword))) passwordSubmissions++; });
  try {
    await page.goto(twoHostE2ePlatformAdminOrigin + AdminRoutes.Audit);
    await expect(page).toHaveURL(url => url.origin === twoHostE2eSignInOrigin && url.pathname === AuthenticationWebRoutes.SignIn, { timeout: 120_000 });
    await page.getByRole("link", { name: new RegExp(getMessages("en").auth.login.passwordMethod) }).click();
    await expect(page.locator('button[type="submit"]')).toBeEnabled();
    await page.locator('input[name="username"]').fill("e2e-platform-admin");
    await page.locator('input[name="password"]').fill(password!);
    await page.locator('button[type="submit"]').click();
    await expect(page).toHaveURL(twoHostE2ePlatformAdminOrigin + AdminRoutes.Audit, { timeout: 120_000 });
    // No Origin, Referer or tenant header: renewal must resolve the actual Auth host.
    const authSession = await page.request.get(twoHostE2eSignInOrigin + generatedOperationPath(authenticationSessionGet));
    expect(authSession.status()).toBe(200);
    for (const [origin, route] of [[twoHostE2ePatientsOrigin, PatientRoutes.Profile], [twoHostE2eProjectManagementOrigin, ProjectRoutes.Projects], [twoHostE2eKnowledgeOrigin, KnowledgeRoutes.KnowledgeQa]]) {
      await page.goto(origin + route);
      await expect(page).toHaveURL(origin + route, { timeout: 120_000 });
      await expect.poll(async () => (await page.request.get(origin + generatedOperationPath(authenticationSessionGet))).status()).toBe(200);
    }
    expect(passwordSubmissions).toBe(1);
    // App-local credential submission remains unavailable even with a valid linked session.
    const credentialPath = generatedOperationPath(authenticationLoginPassword);
    for (const path of [credentialPath, credentialPath.toUpperCase(), credentialPath + "/"])
      expect(await post(page, path, { userNameOrEmail: "e2e-platform-admin", password, returnUrl: null })).toBe(404);
    await page.goto(twoHostE2eSignInOrigin + AuthenticationWebRoutes.AccountSecurity);
    const roots = await page.request.get(twoHostE2eSignInOrigin + generatedOperationPath(authenticationBrowserSessionsGet));
    expect(roots.status()).toBe(200);
    expect((await roots.json()).sessions).toEqual([expect.objectContaining({ isCurrent: true })]);
    const proof = process.env[twoHostE2eProofScreenshotEnvironmentKey];
    if (proof) {
      await expect(page.getByRole("heading", { name: getMessages("en").auth.browserSessions.title, exact: true })).toBeVisible();
      await page.screenshot({ path: proof.replace(/\.png$/i, "-central-browser-security.png"), fullPage: true });
    }
    await page.goto(twoHostE2eProjectManagementOrigin + ProjectRoutes.Projects);
    expect(await post(page, generatedOperationPath(authenticationLogoutPost), { logoutId: null, returnUrl: null })).toBe(200);
    for (const origin of origins) {
      expect((await page.request.get(origin + generatedOperationPath(authenticationSessionGet))).status()).toBe(401);
    }
  } finally { await context.close(); }
});
