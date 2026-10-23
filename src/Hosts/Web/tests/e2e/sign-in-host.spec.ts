import { expect, test, type Page } from "@playwright/test";
import crypto from "node:crypto";
import { existsSync, mkdirSync } from "node:fs";
import { dirname } from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { enMessages } from "../../apps/private-practice-owner/src/localization/messages/en";
import { faMessages } from "../../apps/private-practice-owner/src/localization/messages/fa";
import {
  twoHostE2eLocaleEnvironmentKey,
  twoHostE2eProofScreenshotEnvironmentKey,
} from "../../scripts/two-host-e2e-artifacts.mjs";
import { TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import type { SupportedLocale } from "../../shared/localization/src/locales";

const locale: SupportedLocale = process.env[twoHostE2eLocaleEnvironmentKey] === "fa" ? "fa" : "en";
const workspaceMessages = { en: enMessages, fa: faMessages }[locale].journey.gates.workspace;

const e2ePassword = process.env.VITARISH_E2E_PASSWORD;
// The E2E host binds auth.localhost to the sign-in tenant (src/Hosts/E2e/Program.cs).
const signInOrigin = "http://auth.localhost:3005";
const tenantAOrigin = "http://localhost:3005";
const tenantBHostname = "tenant-b.localhost";
const tenantBOrigin = `http://${tenantBHostname}:3005`;
// E2eFixtureSeeder.SignInUserName: staff in tenant A ("E2E Clinic"), owner of tenant B ("E2E Clinic B", with a logo).
const signInUsername = "e2e-sign-in-user";
const tenantAName = "E2E Clinic";
const tenantBName = "E2E Clinic B";
// The seeded logo is an 8x8 PNG.
const tenantBLogoWidth = 8;
const signInClientIp = "198.51.100.91";
// The sign-in host's own endpoints, which answer nowhere else.
const signInClinicsPath = "/api/modules/parties/sign-in/clinics";
const signInClinicHandoffPath = (tenantId: string) => `/api/modules/parties/sign-in/clinics/${tenantId}/handoff`;
const pageCompileTimeoutMilliseconds = 120_000;

async function capture(page: Page, name: string) {
  const screenshot = process.env[twoHostE2eProofScreenshotEnvironmentKey]?.replace(/\.png$/i, `-${name}.png`);
  if (!screenshot) return;
  mkdirSync(dirname(screenshot), { recursive: true });
  expect(existsSync(screenshot), `refusing to overwrite retained evidence at ${screenshot}`).toBe(false);
  await page.mouse.move(0, 0);
  await page.waitForFunction(() => document.getAnimations().every((animation) => animation.playState !== "running"));
  await page.screenshot({ path: screenshot, fullPage: true });
}

async function getStatus(page: Page, path: string): Promise<number> {
  return page.evaluate(
    async ({ tenantHostHeader, target }) =>
      (await fetch(target, { credentials: "include", headers: { [tenantHostHeader]: window.location.host } })).status,
    { tenantHostHeader: TENANT_HOST_HEADER, target: path },
  );
}

async function postStatus(page: Page, path: string): Promise<number> {
  return page.evaluate(
    async ({ tenantHostHeader, target, requestId }) => {
      const headers = { [tenantHostHeader]: window.location.host };
      const antiforgery = (await (
        await fetch("/api/modules/authentication/antiforgery", { credentials: "include", headers })
      ).json()) as { headerName: string; requestToken: string };
      const response = await fetch(target, {
        method: "POST",
        credentials: "include",
        headers: { ...headers, "X-Request-Id": requestId, [antiforgery.headerName]: antiforgery.requestToken },
      });
      return response.status;
    },
    // The id is made here: inside the page, `crypto` would name this file's Node import.
    { tenantHostHeader: TENANT_HOST_HEADER, target: path, requestId: `e2e-sign-in-host-${crypto.randomUUID()}` },
  );
}

test("signing in on the shared sign-in host lists the person's clinics and lands signed in on the one chosen", async ({ browser }) => {
  test.setTimeout(600_000);
  expect(e2ePassword, "VITARISH_E2E_PASSWORD must be supplied by the two-host runner").toBeTruthy();

  // Its own account and client IP keep this scenario out of other specs' sessions and sign-in limits.
  const context = await browser.newContext({ extraHTTPHeaders: { "X-Forwarded-For": signInClientIp } });
  try {
    await context.addCookies(
      [signInOrigin, tenantAOrigin, tenantBOrigin].map((url) => ({ name: localeCookieName, value: locale, url })),
    );
    const page = await context.newPage();

    // 1. Sign in with a password on the sign-in host, which belongs to no clinic.
    await page.goto(`${signInOrigin}${Routes.Login.Password}`, { waitUntil: "domcontentloaded" });
    await expect(page.locator("form")).toHaveAttribute("method", "post", { timeout: pageCompileTimeoutMilliseconds });
    await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: pageCompileTimeoutMilliseconds });
    await page.locator('input[name="username"]').fill(signInUsername);
    await page.locator('input[name="password"]').fill(e2ePassword!);
    const [loginResponse] = await Promise.all([
      page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/password")),
      page.locator('button[type="submit"]').click(),
    ]);
    expect(loginResponse.status()).toBe(200);

    // 2. With two clinics the journey stops at the picker; each row shows the clinic's own address.
    await expect(page).toHaveURL(new RegExp(`^${signInOrigin}${Routes.Workspaces.Select}(?:[/?#]|$)`), {
      timeout: pageCompileTimeoutMilliseconds,
    });
    const clinicList = page.getByRole("list", { name: workspaceMessages.title });
    await expect(clinicList).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
    const clinicRow = (name: string) =>
      clinicList.getByRole("listitem").filter({ has: page.getByText(name, { exact: true }) });
    await expect(clinicList.getByRole("listitem")).toHaveCount(2);
    await expect(clinicRow(tenantAName).getByText("localhost", { exact: true })).toBeVisible();
    await expect(clinicRow(tenantBName).getByText(tenantBHostname, { exact: true })).toBeVisible();
    await expect(clinicRow(tenantAName).getByText(workspaceMessages.roles.staff, { exact: true })).toBeVisible();
    await expect(clinicRow(tenantBName).getByText(workspaceMessages.roles.owner, { exact: true })).toBeVisible();

    // 3. Tenant B's logo loads from tenant B's own host; tenant A has none and keeps its initials.
    const tenantBLogo = clinicRow(tenantBName).locator("img");
    await expect(tenantBLogo).toHaveAttribute("src", new RegExp(`^${tenantBOrigin}/`));
    await expect
      .poll(() => tenantBLogo.evaluate((image: HTMLImageElement) => (image.complete ? image.naturalWidth : 0)), {
        timeout: pageCompileTimeoutMilliseconds,
      })
      .toBe(tenantBLogoWidth);
    await expect(clinicRow(tenantAName).locator("img")).toHaveCount(0);
    await capture(page, "sign-in-host-clinic-list");

    // 4. No handoff link is issued for a clinic that is not the person's.
    expect(await postStatus(page, signInClinicHandoffPath(crypto.randomUUID()))).toBe(404);

    // 4b. The sign-in host has no clinic dashboard: a signed-in visitor who opens it is sent back to the list.
    await page.goto(`${signInOrigin}${Routes.Home}`, { waitUntil: "domcontentloaded" });
    await expect(page).toHaveURL(new RegExp(`^${signInOrigin}${Routes.Workspaces.Select}(?:[/?#]|$)`), {
      timeout: pageCompileTimeoutMilliseconds,
    });
    await expect(clinicList.getByRole("listitem")).toHaveCount(2, { timeout: pageCompileTimeoutMilliseconds });

    // 5. Choosing tenant B continues through a one-time handoff link and lands signed in on its host.
    const handoffUrlPromise = page
      .waitForURL(new RegExp(`${Routes.Handoff}#.+`), { waitUntil: "commit", timeout: pageCompileTimeoutMilliseconds })
      .then(() => page.url());
    await clinicRow(tenantBName)
      .getByRole("button", { name: workspaceMessages.openAction.replace("{name}", tenantBName) })
      .click();
    expect(await handoffUrlPromise).toMatch(new RegExp(`^${tenantBOrigin}${Routes.Handoff}#.+$`));
    await expect(page).toHaveURL(new RegExp(`^${tenantBOrigin}${Routes.Home}(?:[/?#]|$)`), {
      timeout: pageCompileTimeoutMilliseconds,
    });
    expect(await getStatus(page, "/api/modules/authentication/session")).toBe(200);

    // 6. The sign-in host's clinic list answers only on the sign-in host, not on a clinic host.
    expect(await getStatus(page, signInClinicsPath)).toBe(404);
    await page.waitForLoadState("networkidle");
    await capture(page, "sign-in-host-tenant-b-home");
  } finally {
    await context.close();
  }
});
