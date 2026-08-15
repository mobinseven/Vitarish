import { expect, test, type Page } from "@playwright/test";

const apiOrigin = process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7272";
const e2eUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const e2ePassword = process.env.VITARISH_E2E_PASSWORD;

async function fetchJson(page: Page, path: string) {
  return page.evaluate(async ({ apiOrigin: origin, path: requestPath }) => {
    const response = await fetch(`${origin}${requestPath}`, {
      credentials: "include",
      headers: { "X-Vitarish-Tenant-Host": window.location.host },
    });
    let body: unknown = null;
    try {
      body = await response.json();
    } catch {
      // The status assertion below is the useful diagnostic for an empty response.
    }
    return { status: response.status, body };
  }, { apiOrigin, path });
}

test("uses the real Web and Main hosts for an authenticated tenant-scoped request", async ({ page }) => {
  const apiRequests: Array<{ method: string; url: string; tenantHost: string | undefined }> = [];
  const apiResponses: Array<{ method: string; url: string; status: number; body: unknown }> = [];
  const mockServiceWorkerRequests: string[] = [];
  page.on("request", (request) => {
    if (request.url().startsWith(apiOrigin)) {
      apiRequests.push({ method: request.method(), url: request.url(), tenantHost: request.headers()["x-vitarish-tenant-host"] });
    }
    if (request.url().includes("mockServiceWorker.js")) mockServiceWorkerRequests.push(request.url());
  });
  page.on("response", async (response) => {
    if (!response.url().startsWith(apiOrigin)) return;
    let body: unknown = null;
    try {
      body = await response.json();
    } catch {
      // Non-JSON responses are still represented by their status below.
    }
    apiResponses.push({ method: response.request().method(), url: response.url(), status: response.status(), body });
  });

  expect(e2ePassword).toBeTruthy();
  await page.goto("/login/password", { waitUntil: "domcontentloaded" });
  await page.locator('input[name="username"]').fill(e2eUsername);
  await page.locator('input[name="password"]').fill(e2ePassword!);
  await Promise.all([
    page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/password") && response.status() === 200),
    page.locator('button[type="submit"]').click(),
  ]);
  await page.goto("/home", { waitUntil: "domcontentloaded" });

  await expect(page).toHaveURL(/\/home(?:\?|$)/);
  await expect(page.locator("body")).not.toContainText("MSW");

  const session = await fetchJson(page, "/api/modules/authentication/session");
  expect(session.status).toBe(200);
  expect(session.body).toMatchObject({
    isAuthenticated: true,
    tenantAccess: { canAccessTenantOwner: true },
  });

  const antiforgery = await fetchJson(page, "/api/modules/authentication/antiforgery");
  expect(antiforgery.status).toBe(200);
  expect(antiforgery.body).toMatchObject({ headerName: "X-XSRF-TOKEN" });
  expect((antiforgery.body as { requestToken?: unknown }).requestToken).toEqual(expect.any(String));

  const health = await fetchJson(page, "/api/modules/appointments/health");
  expect(health.status).toBe(200);
  expect(health.body).toMatchObject({ succeeded: true });

  const removedAnonymousRoute = await fetchJson(page, "/__e2e/authenticate");
  expect(removedAnonymousRoute.status).toBe(404);

  await expect.poll(() => page.locator("body").innerText()).toMatch(/patients|patients/i);

  expect(mockServiceWorkerRequests).toEqual([]);
  const dashboardEndpoints = [
    "/api/modules/clinic/patients/count",
    "/api/modules/appointments/reservations",
    "/api/modules/appointments/shifts",
    "/communications/reminders/query",
  ];
  await expect.poll(() => dashboardEndpoints.every((path) => apiResponses.some((response) => response.url.includes(path) && response.method === "POST" && response.status === 200))).toBe(true);
  const patientCount = apiResponses.find((response) => response.url.includes("/api/modules/clinic/patients/count") && response.method === "POST" && response.status === 200)?.body;
  const reservations = apiResponses.find((response) => response.url.includes("/api/modules/appointments/reservations") && response.method === "POST" && response.status === 200)?.body;
  const shifts = apiResponses.find((response) => response.url.includes("/api/modules/appointments/shifts") && response.method === "POST" && response.status === 200)?.body;
  const reminders = apiResponses.find((response) => response.url.includes("/communications/reminders/query") && response.method === "POST" && response.status === 200)?.body;
  expect(patientCount).toBe(1);
  expect(reservations).toEqual(expect.arrayContaining([expect.objectContaining({ status: 1 })]));
  expect(shifts).toEqual(expect.arrayContaining([expect.objectContaining({ totalSeats: 2 })]));
  expect(reminders).toEqual(expect.arrayContaining([expect.objectContaining({ isSent: false, subject: "E2E reminder" })]));
  for (const metric of ["patients", "reservations", "reminders", "schedule"]) {
    await expect(page.getByTestId(`home-metric-${metric}`)).toContainText("1");
  }
  expect(apiRequests.some((request) => request.url.endsWith("/api/modules/authentication/session"))).toBe(true);
  expect(apiRequests.some((request) => request.url.endsWith("/api/modules/appointments/health"))).toBe(true);
  expect(apiRequests.filter((request) => request.method !== "OPTIONS").every((request) => request.tenantHost === new URL(page.url()).host)).toBe(true);

  const proofScreenshot = process.env.VITARISH_E2E_PROOF_SCREENSHOT;
  if (proofScreenshot) {
    await page.screenshot({ path: proofScreenshot, fullPage: true });
  }
});
