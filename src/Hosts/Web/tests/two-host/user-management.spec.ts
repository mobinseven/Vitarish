import { expect, test, type Page } from "@playwright/test";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { Routes as PlatformRoutes } from "../../apps/platform-admin/src/app/routes";

const platformOrigin = process.env.PLAYWRIGHT_PLATFORM_ADMIN_ORIGIN ?? "http://localhost:3006";
const password = process.env.VITARISH_E2E_PASSWORD;
const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const platformUsername = process.env.VITARISH_E2E_USER_MANAGEMENT_PLATFORM_ADMIN_USERNAME ?? "e2e-user-management-admin";

async function login(page: Page, username: string, origin = ""): Promise<void> {
  expect(password, "VITARISH_E2E_PASSWORD must be supplied by the disposable E2E environment").toBeTruthy();
  await page.goto(`${origin}${ClinicRoutes.Login.Password}`, { waitUntil: "domcontentloaded" });
  await page.locator('input[name="username"]').fill(username);
  await page.locator('input[name="password"]').fill(password!);
  const [response] = await Promise.all([
    page.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/password")),
    page.locator('button[type="submit"]').click(),
  ]);
  expect(response.status()).toBe(200);
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
  await login(page, platformUsername);
  await page.goto(`${platformOrigin}${PlatformRoutes.Users.Index}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Users", exact: true })).toBeVisible({ timeout: 30_000 });
  await page.getByRole("textbox", { name: "Search users" }).fill(ownerUsername);
  await page.getByRole("button", { name: "Search" }).click();
  await expect(page.locator("tbody tr").first()).toBeVisible({ timeout: 30_000 });
  await page.locator("tbody tr").first().click();
  await expect(page.getByRole("heading", { name: "Tenant memberships", exact: true })).toBeVisible();
});
