import { expect, test } from "@playwright/test";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { twoHostE2eClinicOrigin, twoHostE2eLandingOrigin, twoHostE2eForwardedForHeaderName, twoHostE2eLocaleEnvironmentKey, twoHostE2eOwnerUsernameEnvironmentKey, twoHostE2ePasswordEnvironmentKey, twoHostE2eSignInOrigin } from "../../scripts/two-host-e2e-artifacts.mjs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { navigationTransitionQueryKey, navigationTransitionPolicy, navigationTransitionVersion, navigationTransitionStorageKey, withNavigationTransition, parseNavigationTransition, type NavigationTransitionContext } from "../../shared/ui/src/navigation/transitionContext";
import { navigationTransitionDocument } from "../../shared/ui/src/navigation/transitionDocument";
import { authenticationAntiforgeryGet, authenticationContinuationBegin, authenticationLogoutPost, authenticationTenantHandoffRedeem } from "../../shared/auth/src/api/generated/authentication/authentication";
import { REQUEST_ID_HEADER, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { generatedOperationPath } from "./generatedOperationPath";
import { submitPasswordOnSignInHost } from "./signInHost";
import { LandingRoutes } from "../../apps/vitarish-landing/src/app/routes";
import { homeMessageCatalogs } from "../../apps/vitarish-landing/src/features/home/homeMessages";
import { landingListWorkspaces, landingIssueWorkspaceHandoff } from "../../shared/platform/src/api/generated/landing/landing";
import type { LandingWorkspacesResponse, LandingWorkspaceHandoffResponse } from "../../shared/platform/src/api/generated/landing/model";
import { getRequestedReturnUrl } from "../../shared/auth/src/hooks/requestedReturnUrl";
import { AuthenticationWebRoutes } from "../../shared/auth/src/routes";

const transitionClientIp = "198.51.100.219";
const locale = process.env[twoHostE2eLocaleEnvironmentKey] === "fa" ? "fa" : "en";

test("navigation-transition preserves the presentation attempt through correlated central entry", async ({ page, context }, testInfo) => {
  test.setTimeout(360_000);
  const username = process.env[twoHostE2eOwnerUsernameEnvironmentKey];
  const password = process.env[twoHostE2ePasswordEnvironmentKey];
  expect(username).toBeTruthy();
  expect(password).toBeTruthy();
  await context.setExtraHTTPHeaders({ [twoHostE2eForwardedForHeaderName]: transitionClientIp });
  await context.addCookies([twoHostE2eClinicOrigin, twoHostE2eSignInOrigin, twoHostE2eLandingOrigin].map(url => ({ name: localeCookieName, value: locale, url })));
  let signedIn = false;
  let releaseChunks = () => {};
  let releaseIssuance = () => {};
  try {
    const directoryResponse = page.waitForResponse(response => new URL(response.url()).pathname === generatedOperationPath(landingListWorkspaces) && response.request().method() === "GET");
    expect((await submitPasswordOnSignInHost(page, username!, password!)).status()).toBe(200);
    signedIn = true;
    await expect(page).toHaveURL(twoHostE2eLandingOrigin + LandingRoutes.PlatformHome, { timeout: 120_000 });
    const directory = await (await directoryResponse).json() as LandingWorkspacesResponse;
    const workspace = directory.workspaces.find(item => item.hostname === new URL(twoHostE2eClinicOrigin).hostname);
    expect(workspace).toBeDefined();
    await expect(page.locator(`#${navigationTransitionDocument.surfaceId}`)).toBeHidden();
    const incoming: { origin: string; path: string; carried: boolean; attempt: string | undefined }[] = [];
    const apiReturnPaths: unknown[] = [];
    const hydrationErrors: string[] = [];
    page.on("console", message => { if (/hydration|didn't match|did not match|React error #418/i.test(message.text())) hydrationErrors.push(message.type()); });
    page.on("pageerror", error => hydrationErrors.push(error.name));
    page.on("request", request => {
      const url = new URL(request.url());
      if (request.isNavigationRequest() && request.frame() === page.mainFrame()) {
        incoming.push({ origin: url.origin, path: url.pathname, carried: url.searchParams.has(navigationTransitionQueryKey), attempt: parseNavigationTransition(url.searchParams.get(navigationTransitionQueryKey))?.attempt });
      }
      if (request.method() === "POST" && url.pathname === generatedOperationPath(authenticationContinuationBegin)) apiReturnPaths.push(request.postDataJSON().returnPath);
    });
    const issuancePath = generatedOperationPath(landingIssueWorkspaceHandoff).replace(/\$\{[^}]+\}/, encodeURIComponent(workspace!.tenantId));
    const issuanceBarrier = new Promise<void>(resolve => { releaseIssuance = resolve; });
    const issuedHandoff = Promise.withResolvers<LandingWorkspaceHandoffResponse>();
    await page.route(`**${issuancePath}`, async route => {
      await issuanceBarrier;
      try {
        const response = await route.fetch({ maxRetries: 0, maxRedirects: 0 });
        expect(response.status()).toBe(200);
        issuedHandoff.resolve(await response.json() as LandingWorkspaceHandoffResponse);
        await route.fulfill({ response });
      } catch (error) { issuedHandoff.reject(error); await route.abort(); }
    });
    const chunksBarrier = new Promise<void>(resolve => { releaseChunks = resolve; });
    const clientChunks = "**/_next/static/chunks/**";
    await page.route(clientChunks, async route => { await chunksBarrier; await route.continue(); });
    const redeemed = page.waitForResponse(response => new URL(response.url()).pathname === generatedOperationPath(authenticationTenantHandoffRedeem) && response.request().method() === "POST");
    const openLabel = homeMessageCatalogs[locale].home.workspaces.openNamed.replace("{name}", workspace!.name);
    await page.getByRole("button", { name: openLabel, exact: true }).click();
    const attempt = await page.evaluate(runtimeKey => (window as unknown as Record<string, { context: NavigationTransitionContext }>)[runtimeKey].context, navigationTransitionDocument.runtime);
    expect(attempt.locale).toBe(locale);
    await expect(page.locator(`#${navigationTransitionDocument.surfaceId}`)).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath("00-home-departure.png") });
    releaseIssuance();
    const handoff = await issuedHandoff.promise;
    expect(handoff.handoffUrl).toBeTruthy();
    const expectedReturnPath = getRequestedReturnUrl(new URL(handoff.handoffUrl!).searchParams) ?? null;
    await page.waitForURL(url => url.origin === twoHostE2eClinicOrigin && url.pathname === Routes.Login.Index, { waitUntil: "commit" });
    await expect.poll(() => page.evaluate(runtimeKey => (window as unknown as Record<string, { context: NavigationTransitionContext }>)[runtimeKey]?.context.attempt, navigationTransitionDocument.runtime)).toBe(attempt.attempt);
    await expect(page.locator(`#${navigationTransitionDocument.surfaceId}`)).toHaveAttribute(navigationTransitionDocument.modeAttribute, attempt.mode);
    await page.screenshot({ path: testInfo.outputPath("01-before-react.png") });
    releaseChunks();
    expect((await redeemed).status()).toBe(200);
    await expect(page).toHaveURL(twoHostE2eClinicOrigin + Routes.Home, { timeout: 120_000 });
    await expect(page.locator(`#${navigationTransitionDocument.surfaceId}`)).toBeHidden();
    expect(incoming.filter(entry => entry.carried)).toEqual([
      { origin: twoHostE2eClinicOrigin, path: Routes.Login.Index, carried: true, attempt: attempt.attempt },
      { origin: twoHostE2eSignInOrigin, path: AuthenticationWebRoutes.SignIn, carried: true, attempt: attempt.attempt },
      { origin: twoHostE2eClinicOrigin, path: Routes.Handoff, carried: true, attempt: attempt.attempt },
    ]);
    expect(apiReturnPaths.map(value => value === expectedReturnPath)).toEqual([true]);
    expect(hydrationErrors).toEqual([]);
    await page.screenshot({ path: testInfo.outputPath("02-navigation-destination.png") });
    await testInfo.attach("sanitized-transition-hops", { body: JSON.stringify(incoming), contentType: "application/json" });
  } finally {
    releaseChunks();
    releaseIssuance();
      if (signedIn) {
        const headers = { [TENANT_HOST_HEADER]: new URL(twoHostE2eSignInOrigin).host, [twoHostE2eForwardedForHeaderName]: transitionClientIp };
        const antiforgery = await context.request.get(twoHostE2eSignInOrigin + generatedOperationPath(authenticationAntiforgeryGet), { headers });
        expect(antiforgery.status()).toBe(200);
        const anti = await antiforgery.json();
        const logout = await context.request.post(twoHostE2eSignInOrigin + generatedOperationPath(authenticationLogoutPost), {
          headers: { ...headers, [REQUEST_ID_HEADER]: crypto.randomUUID(), [anti.headerName]: anti.requestToken },
          data: { logoutId: null, returnUrl: null },
        });
        expect(logout.status()).toBe(200);
      }
  }
});


test("navigation-transition bounds a hung bootstrap and offers fresh entry without replay", async ({ page, context }, testInfo) => {
  await context.addCookies([{ name: localeCookieName, value: locale, url: twoHostE2eClinicOrigin }]);
  await page.emulateMedia({ reducedMotion: "reduce" });
  let release = () => {};
  const barrier = new Promise<void>(resolve => { release = resolve; });
  await page.route("**/_next/static/chunks/**", async route => { await barrier; await route.abort(); });
  const attempt: NavigationTransitionContext = {
    version: navigationTransitionVersion, attempt: crypto.randomUUID(),
    started: Date.now() - navigationTransitionPolicy.recoveryMilliseconds + 6_000,
    locale, mode: testInfo.project.use.colorScheme === "dark" ? "dark" : "light",
  };
  try {
    await page.goto(withNavigationTransition(twoHostE2eClinicOrigin + Routes.Login.Index, attempt, twoHostE2eClinicOrigin), { waitUntil: "commit" });
    const surface = page.locator(`#${navigationTransitionDocument.surfaceId}`);
    await expect(surface).toHaveAttribute(navigationTransitionDocument.slowAttribute, "true");
    await expect(surface).toHaveAttribute("aria-busy", "true");
    await page.screenshot({ path: testInfo.outputPath("00-slow-bootstrap.png") });
    await expect(surface).toHaveAttribute("aria-busy", "false", { timeout: 15_000 });
    const recovery = page.locator(`#${navigationTransitionDocument.recoveryId}`);
    await expect(recovery).toBeVisible();
    const entry = recovery.locator(`[${navigationTransitionDocument.localeAttribute}="${locale}"] a`).first();
    await expect(entry).toBeFocused();
    expect(await entry.getAttribute("href")).not.toContain(navigationTransitionQueryKey);
    expect(new URL(page.url()).searchParams.has(navigationTransitionQueryKey)).toBe(false);
    await expect(page.locator(`[${navigationTransitionDocument.contentAttribute}]`)).toHaveAttribute("inert", "");
    await page.screenshot({ path: testInfo.outputPath("01-bounded-recovery.png") });
  } finally { release(); }
});

test.describe("navigation-transition without script execution", () => {
  test.use({ javaScriptEnabled: false });
  test("navigation-transition retains usable server-rendered recovery", async ({ page, context }, testInfo) => {
    await context.addCookies([{ name: localeCookieName, value: locale, url: twoHostE2eClinicOrigin }]);
    await page.goto(twoHostE2eClinicOrigin + Routes.Login.Index);
    const recovery = page.locator(`#${navigationTransitionDocument.recoveryId}`);
    await expect(recovery).toBeVisible();
    const entry = recovery.locator(`[${navigationTransitionDocument.localeAttribute}="${locale}"] a`).first();
    await expect(entry).toBeVisible();
    await expect(entry).toHaveAttribute("href", Routes.Login.Index);
    await expect(page.locator(`[${navigationTransitionDocument.contentAttribute}]`)).toHaveAttribute("inert", "");
    await page.screenshot({ path: testInfo.outputPath("00-no-script-recovery.png") });
  });
});


test("navigation-transition yields to an interactive sign-in and survives refresh", async ({ page, context }, testInfo) => {
  await context.addCookies([{ name: localeCookieName, value: locale, url: twoHostE2eSignInOrigin }]);
  await page.goto(twoHostE2eSignInOrigin + Routes.Login.Password);
  const username = page.locator('input[name="username"]');
  await expect(username).toBeEditable();
  await expect(page.locator(`#${navigationTransitionDocument.surfaceId}`)).toBeHidden();
  await username.fill("navigation-fixture");
  await page.screenshot({ path: testInfo.outputPath("00-interaction-required.png") });
  await page.reload();
  await expect(username).toBeEditable();
  await expect(page.locator(`#${navigationTransitionDocument.surfaceId}`)).toBeHidden();
});


test("navigation-transition remains tab-local and cancels on browser Back when storage is denied", async ({ page, context }) => {
  await context.addInitScript(() => {
    for (const method of ["getItem", "setItem", "removeItem"]) Object.defineProperty(Storage.prototype, method, { value: () => { throw new DOMException("fixture storage denied", "SecurityError"); } });
  });
  await context.addCookies([{ name: localeCookieName, value: locale, url: twoHostE2eSignInOrigin }]);
  await page.goto(twoHostE2eSignInOrigin + Routes.Login.Password);
  const surface = page.locator(`#${navigationTransitionDocument.surfaceId}`);
  await expect(surface).toBeHidden();
  const other = await context.newPage();
  try {
    await other.goto(twoHostE2eSignInOrigin + Routes.Login.Password);
    await expect(other.locator(`#${navigationTransitionDocument.surfaceId}`)).toBeHidden();
    const attempt = await page.evaluate(({ owner, storageKey }) => {
      const runtime = (window as unknown as Record<string, import("../../shared/ui/src/navigation/transitionContract").NavigationTransitionRuntime>)[owner.runtime];
      runtime.start({ ...runtime.context, attempt: crypto.randomUUID(), started: Date.now() });
      history.pushState({ fixture: true }, "", location.href);
      let denied = false;
      try { sessionStorage.getItem(storageKey); } catch { denied = true; }
      return { id: runtime.context.attempt, denied };
    }, { owner: navigationTransitionDocument, storageKey: navigationTransitionStorageKey });
    expect(attempt.denied).toBe(true);
    await expect(surface).toBeVisible();
    await expect(other.locator(`#${navigationTransitionDocument.surfaceId}`)).toBeHidden();
    await page.goBack();
    await expect(surface).toBeHidden();
    await expect(page.locator('input[name="username"]')).toBeEditable();
  } finally { await other.close(); }
});

test("navigation-transition keeps recovery operable when CSP blocks scripts", async ({ page, context }, testInfo) => {
  await context.addCookies([{ name: localeCookieName, value: locale, url: twoHostE2eClinicOrigin }]);
  await page.route(twoHostE2eClinicOrigin + Routes.Login.Index, async route => {
    const response = await route.fetch({ maxRetries: 0 });
    const headers = response.headers();
    const cspHeader = "content-security-policy";
    const existingPolicy = headers[cspHeader] ?? "";
    headers[cspHeader] = /(?:^|;)\s*script-src(?:-elem)?\s/.test(existingPolicy)
      ? existingPolicy.replace(/(^|;)\s*script-src(-elem)?\s[^;]*/g, "$1 script-src$2 'none'")
      : `${existingPolicy}; script-src 'none'`;
    await route.fulfill({ response, headers });
  });
  await page.goto(twoHostE2eClinicOrigin + Routes.Login.Index);
  const entry = page.locator(`#${navigationTransitionDocument.recoveryId} [${navigationTransitionDocument.localeAttribute}="${locale}"] a`).first();
  await expect(entry).toBeVisible();
  await expect(entry).toHaveAttribute("href", Routes.Login.Index);
  await entry.focus();
  await expect(entry).toBeFocused();
  await page.screenshot({ path: testInfo.outputPath("00-csp-recovery.png") });
});
