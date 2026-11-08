import { expect, test, type Page } from "@playwright/test";
import crypto from "node:crypto";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { LandingRoutes } from "../../apps/vitarish-landing/src/app/routes";
import { AuthenticationSearchParams } from "../../shared/auth/src/routes";
import {
  twoHostE2eApiOriginEnvironmentKey,
  twoHostE2eClinicOrigin,
  twoHostE2eLandingOrigin,
  twoHostE2eLocaleEnvironmentKey,
  twoHostE2ePasswordEnvironmentKey,
  twoHostE2eSignInOrigin as signInOrigin,
} from "../../scripts/two-host-e2e-artifacts.mjs";
import { authenticationAntiforgeryGet, authenticationLogoutPost, authenticationLoginPassword, getAuthenticationSessionGetQueryKey } from "../../shared/auth/src/api/generated/authentication/authentication";
import {
  partiesGetSignInClinics,
  partiesIssueSignInClinicHandoff,
  partiesIssueSignInHomeHandoff,
} from "../../shared/journey/src/api/generated/parties/parties";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import type { SupportedLocale } from "../../shared/localization/src/locales";
import { REQUEST_ID_HEADER, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { generatedOperationPath } from "./generatedOperationPath";
import { forwardedForHeaderName } from "./signInHost";

const locale: SupportedLocale = process.env[twoHostE2eLocaleEnvironmentKey] === "fa" ? "fa" : "en";
const password = process.env[twoHostE2ePasswordEnvironmentKey];
// E2eFixtureSeeder.SignInUserName: the account used for shared sign-in host coverage.
const signInUsername = "e2e-sign-in-user";
const signInClientIp = "198.51.100.91";
// These legacy Parties endpoints remain available on the sign-in host for API compatibility.
const signInClinicsPath = generatedOperationPath(partiesGetSignInClinics);
const signInClinicHandoffPathTemplate = generatedOperationPath(partiesIssueSignInClinicHandoff);
const signInClinicHandoffPath = (tenantId: string) => signInClinicHandoffPathTemplate.replace(/\$\{[^}]+\}/, tenantId);
const pageCompileTimeoutMilliseconds = 120_000;
const sessionPath = getAuthenticationSessionGetQueryKey()[0];
const antiforgeryPath = generatedOperationPath(authenticationAntiforgeryGet);
const passwordLoginPath = generatedOperationPath(authenticationLoginPassword);
const signInHomeHandoffPath = generatedOperationPath(partiesIssueSignInHomeHandoff);

async function getStatus(page: Page, path: string): Promise<number> {
  return page.evaluate(
    async ({ tenantHostHeader, target }) =>
      (await fetch(target, { credentials: "include", headers: { [tenantHostHeader]: window.location.host } })).status,
    { tenantHostHeader: TENANT_HOST_HEADER, target: path },
  );
}

async function postStatus(page: Page, path: string, body?: unknown): Promise<number> {
  return page.evaluate(
    async ({ tenantHostHeader, requestIdHeader, antiforgeryRequestPath, target, requestId, body }) => {
      const headers = { [tenantHostHeader]: window.location.host };
      const antiforgery = (await (
        await fetch(antiforgeryRequestPath, { credentials: "include", headers })
      ).json()) as { headerName: string; requestToken: string };
      const response = await fetch(target, {
        method: "POST",
        credentials: "include",
        headers: { ...headers, [requestIdHeader]: requestId, "Content-Type": "application/json", [antiforgery.headerName]: antiforgery.requestToken },
        body: body === undefined ? undefined : JSON.stringify(body),
      });
      return response.status;
    },
    { tenantHostHeader: TENANT_HOST_HEADER, requestIdHeader: REQUEST_ID_HEADER, antiforgeryRequestPath: antiforgeryPath, target: path, requestId: `e2e-sign-in-host-${crypto.randomUUID()}`, body },
  );
}

test("sign-in host forwards clinic-local visitors to platform home", async ({ browser }) => {
  test.setTimeout(600_000);
  expect(password, `${twoHostE2ePasswordEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();

  const context = await browser.newContext({ extraHTTPHeaders: { [forwardedForHeaderName]: signInClientIp } });
  try {
    await context.addCookies(
      [signInOrigin, twoHostE2eClinicOrigin, twoHostE2eLandingOrigin].map((url) => ({ name: localeCookieName, value: locale, url })),
    );
    const page = await context.newPage();

    // Clinic-local entry pages lead to the shared sign-in host before collecting credentials.
    await page.goto(`${twoHostE2eClinicOrigin}${Routes.Login.Password}`, { waitUntil: "domcontentloaded" });
    await expect(page).toHaveURL((url) => url.origin === signInOrigin && url.pathname === Routes.Login.Password && Boolean(url.searchParams.get(AuthenticationSearchParams.ReturnUrl)), { timeout: pageCompileTimeoutMilliseconds });

    await page.goto(`${signInOrigin}${Routes.Login.Password}`, { waitUntil: "domcontentloaded" });
    await expect(page.locator("form")).toHaveAttribute("method", "post", { timeout: pageCompileTimeoutMilliseconds });
    await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: pageCompileTimeoutMilliseconds });
    await page.locator('input[name="username"]').fill(signInUsername);
    await page.locator('input[name="password"]').fill(password!);

    let issuingCookieHeader = "";
    await page.route(
      (url) => {
        try {
          return new URL(url.href).pathname === signInHomeHandoffPath;
        } catch {
          return false;
        }
      },
      async (route) => {
        issuingCookieHeader = (await route.request().allHeaders()).cookie ?? "";
        await route.continue();
      },
    );

    const continueUrl = page.waitForURL((url) => url.origin === signInOrigin && url.pathname === Routes.SignInContinue, {
      waitUntil: "commit",
      timeout: pageCompileTimeoutMilliseconds,
    });
    const [loginResponse] = await Promise.all([
      page.waitForResponse((response) => response.url().endsWith(passwordLoginPath)),
      page.locator('button[type="submit"]').click(),
    ]);
    expect(loginResponse.status()).toBe(200);
    await continueUrl;
    await page.waitForURL((url) => url.origin === twoHostE2eLandingOrigin && url.pathname === LandingRoutes.PlatformHome, {
      waitUntil: "domcontentloaded",
      timeout: pageCompileTimeoutMilliseconds,
    });
    await expect.poll(() => getStatus(page, sessionPath), { timeout: pageCompileTimeoutMilliseconds }).toBe(200);

    // The clinic directory and clinic handoffs stay mapped only on the sign-in host.
    expect(await getStatus(page, signInClinicsPath)).toBe(404);
    expect(await postStatus(page, signInClinicHandoffPath(crypto.randomUUID()))).toBe(404);

    const apiOrigin = process.env[twoHostE2eApiOriginEnvironmentKey];
    expect(apiOrigin, `${twoHostE2eApiOriginEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();
    expect(issuingCookieHeader, "the sign-in host cookie is captured before home handoff").toBeTruthy();
    const sourceSession = await context.request.get(new URL(sessionPath, apiOrigin!).href, {
      headers: {
        Cookie: issuingCookieHeader,
        [TENANT_HOST_HEADER]: new URL(signInOrigin).host,
        [forwardedForHeaderName]: signInClientIp,
      },
      ignoreHTTPSErrors: true,
    });
    expect(sourceSession.status()).toBe(200);

    expect(await postStatus(page, generatedOperationPath(authenticationLogoutPost), { logoutId: null, returnUrl: null })).toBe(200);
    expect(await getStatus(page, sessionPath)).toBe(401);
  } finally {
    await context.close();
  }
});
