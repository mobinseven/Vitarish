import { expect, test, type Page } from "@playwright/test";
import crypto from "node:crypto";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { LANDING_CONTENT_SITE_KEY, LANDING_EDITORIAL_PAGE_FIELDS } from "../../apps/vitarish-landing/src/content/constants";
import { LandingRoutes } from "../../apps/vitarish-landing/src/app/routes";
import { contentsPublicDeliveryRoutes, publicEntryPathQueryKey } from "../../shared/contents/src/contentsApiRoutes";
import { contentFieldKind, contentsStudioApiRoutes } from "../../shared/contents/src/studio/contentsStudioApi";
import { TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { authenticationAntiforgeryGet } from "../../shared/auth/src/api/generated/authentication/authentication";
import { twoHostE2eProofScreenshotEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import { generatedOperationPath } from "./generatedOperationPath";
import { defaultClinicOrigin, signInToClinic } from "./signInHost";

// The E2E host binds the landing tenant to landing.localhost; its startup seeds the purpose page.
const landingOrigin = "http://landing.localhost:3005";
const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const password = process.env.VITARISH_E2E_PASSWORD;
const proofScreenshot = process.env[twoHostE2eProofScreenshotEnvironmentKey];

type ApiResult = { status: number; body: Record<string, unknown> | null };

const managementPath = (route: string, parameters: Record<string, string> = {}) =>
  `${contentsStudioApiRoutes.base}${route.replace(/\{([A-Za-z]+):guid\}/g, (_, name: string) => parameters[name])}`;

const publicEntryPath = (siteKey: string, path: string) =>
  `${contentsPublicDeliveryRoutes.base}${contentsPublicDeliveryRoutes.entry.replace("{siteKey}", encodeURIComponent(siteKey))}?${new URLSearchParams({ [publicEntryPathQueryKey]: path })}`;

/** Calls the API from the signed-in clinic page: same origin, its tenant host, antiforgery on writes. */
async function callApi(page: Page, method: "GET" | "POST" | "PUT", path: string, body?: unknown): Promise<ApiResult> {
  return page.evaluate(
    async ({ tenantHostHeader, antiforgeryPath, requestMethod, requestPath, requestBody, requestId }) => {
      const headers: Record<string, string> = { [tenantHostHeader]: window.location.host, "X-Request-Id": requestId };
      if (requestMethod !== "GET") {
        const antiforgery = (await (await fetch(antiforgeryPath, { credentials: "include", headers })).json()) as { headerName: string; requestToken: string };
        headers[antiforgery.headerName] = antiforgery.requestToken;
        headers["Content-Type"] = "application/json";
      }
      const response = await fetch(requestPath, { method: requestMethod, credentials: "include", headers, body: requestBody === undefined ? undefined : JSON.stringify(requestBody) });
      const text = await response.text();
      return { status: response.status, body: text ? JSON.parse(text) : null };
    },
    { tenantHostHeader: TENANT_HOST_HEADER, antiforgeryPath: generatedOperationPath(authenticationAntiforgeryGet), requestMethod: method, requestPath: path, requestBody: body, requestId: `e2e-contents-${crypto.randomUUID()}` },
  );
}

// The two-host runner serves landing.localhost from the owner app, so this proves the landing tenant's
// seeded, published entry and its anonymous delivery; the landing app's page render is not part of this host.
test("the landing purpose page is delivered from published CMS content", async ({ page }) => {
  await page.goto(landingOrigin, { waitUntil: "domcontentloaded" });
  // landing.localhost resolves in the browser only, so delivery is read from the page as an anonymous visitor.
  const delivered = await page.evaluate(async ({ path, tenantHostHeader }) => {
    const response = await fetch(path, { credentials: "omit", headers: { [tenantHostHeader]: window.location.host } });
    return { status: response.status, body: response.ok ? await response.json() as { canonicalPath: string; fields: Record<string, Record<string, unknown>> } : null };
  }, { path: publicEntryPath(LANDING_CONTENT_SITE_KEY, LandingRoutes.Purpose), tenantHostHeader: TENANT_HOST_HEADER });
  expect(delivered.status).toBe(200);
  expect(delivered.body!.canonicalPath).toBe(LandingRoutes.Purpose);
  // Every landing locale has the seeded title and rich-text body.
  for (const field of [LANDING_EDITORIAL_PAGE_FIELDS.title, LANDING_EDITORIAL_PAGE_FIELDS.body]) {
    expect(Object.keys(delivered.body!.fields[field]).sort()).toEqual(["en", "fa"]);
  }
});

test("a clinic owner authors, publishes and serves an entry, and drafts stay private", async ({ page, request }) => {
  expect(password, "VITARISH_E2E_PASSWORD must be supplied by the disposable E2E environment").toBeTruthy();
  await signInToClinic(page, ownerUsername, password!);

  const suffix = crypto.randomUUID().slice(0, 8);
  const siteKey = `e2e-${suffix}`;
  const site = await callApi(page, "POST", managementPath(contentsStudioApiRoutes.sites), { key: siteKey, name: "E2E site", defaultLocale: "en", locales: ["en"] });
  expect(site.status, JSON.stringify(site.body)).toBe(200);
  const type = await callApi(page, "POST", managementPath(contentsStudioApiRoutes.types), {
    key: `e2e-note-${suffix}`,
    name: "E2E note",
    description: null,
    fields: [{ key: "summary", label: "Summary", kind: contentFieldKind.text, required: true, localized: false }],
  });
  expect(type.status, JSON.stringify(type.body)).toBe(200);

  const path = `/notes/${suffix}`;
  const title = `CMS proof ${suffix}`;
  const created = await callApi(page, "POST", managementPath(contentsStudioApiRoutes.entries), {
    siteId: site.body!.id, contentTypeId: type.body!.id, slug: suffix, canonicalPath: path, title, excerpt: null, fields: { summary: "Published through the CMS." },
  });
  expect(created.status, JSON.stringify(created.body)).toBe(200);
  const entryId = created.body!.id as string;

  const anonymousEntry = () => request.get(`${defaultClinicOrigin}${publicEntryPath(siteKey, path)}`, { headers: { [TENANT_HOST_HEADER]: new URL(defaultClinicOrigin).host } });
  expect((await anonymousEntry()).status()).toBe(404);

  const entry = await callApi(page, "GET", managementPath(contentsStudioApiRoutes.entry, { id: entryId }));
  const published = await callApi(page, "POST", managementPath(contentsStudioApiRoutes.entryPublish, { id: entryId }), {
    revisionId: entry.body!.workingRevisionId, expectedVersion: entry.body!.version,
  });
  expect(published.status, JSON.stringify(published.body)).toBe(200);

  const stale = await callApi(page, "PUT", managementPath(contentsStudioApiRoutes.entryDraft, { id: entryId }), {
    expectedVersion: entry.body!.version, slug: suffix, canonicalPath: path, title: "stale", excerpt: null, fields: { summary: "stale" },
  });
  expect(stale.status).toBe(409);

  const delivered = await anonymousEntry();
  expect(delivered.status()).toBe(200);
  expect(((await delivered.json()) as { title: string }).title).toBe(title);

  await page.goto(`${defaultClinicOrigin}${ClinicRoutes.Contents.Index}`, { waitUntil: "domcontentloaded" });
  // The studio lists the site this owner just created, read back through the protected API.
  await expect(page.getByRole("main").getByText(siteKey, { exact: true })).toBeVisible({ timeout: 60_000 });
  if (proofScreenshot) await page.screenshot({ path: proofScreenshot, fullPage: true });
});
