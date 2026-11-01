import { expect, test, type Page } from "@playwright/test";
import { randomUUID } from "node:crypto";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { Routes } from "../../apps/platform-admin/src/app/routes";
import { TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { authenticationLoginPassword, authenticationLogoutPost, getAuthenticationAntiforgeryGetQueryKey, getAuthenticationSessionGetQueryKey } from "../../shared/auth/src/api/generated/authentication/authentication";
import { usersAdministrationCreateWithEmail, usersDeleteUser, getUsersSearchUsersQueryKey } from "../../shared/auth/src/api/generated/users/users";
import type { AuthenticationSessionDto } from "../../shared/auth/src/api/generated/authentication/model";
import type { UserSummaryResponse } from "../../shared/auth/src/api/generated/users/model";
import { getAuditPlatformSecurityEventsListQueryKey } from "../../shared/security/src/api/generated/audit/audit";
import type { SecurityAuditEventReadDto, AuditPlatformSecurityEventsListParams } from "../../shared/security/src/api/generated/audit/model";
import { auditSortFields } from "../../shared/security/src/api/securityAuditSortFields";
import { localizePlatformAuditEvent, localizePlatformAuditChangeValue, securityMessageCatalogs } from "../../shared/security/src/localization";
import { usersMessageCatalogs } from "../../shared/users/src/localization/catalogs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { twoHostE2eAccountDeletionClientIp, twoHostE2eForwardedForHeaderName as forwardedForHeaderName, twoHostE2eLocaleEnvironmentKey, twoHostE2ePasswordEnvironmentKey, twoHostE2ePlatformAdminOrigin } from "../../scripts/two-host-e2e-artifacts.mjs";
import { generatedOperationPath } from "./generatedOperationPath";

// Read fixture and event identifiers from their actual source owners, never copy their values here.
function sourceString(path: string, name: string): string {
  const source = readFileSync(resolve(process.cwd(), path), "utf8");
  const value = new RegExp(`public const string ${name} = "([^"]+)";`).exec(source)?.[1];
  if (!value) throw new Error(`Source constant ${name} was not found in ${path}.`);
  return value;
}
const fixtureSource = "../E2e/Infrastructure/Fixtures/E2eFixtureSeeder.cs";
const auditSource = "../../Libraries/Audit/Audit.Domain/SecurityAuditEventCatalog.cs";
const administrator = sourceString(fixtureSource, "UserManagementPlatformAdminUserName");
const deletionEvent = sourceString(auditSource, "UserDeletionEvent");
const deletionConflict = sourceString(auditSource, "UserDeletionConflictEvent");
const deletionOperation = sourceString(auditSource, "UserDeletionOperation");
const origin = twoHostE2ePlatformAdminOrigin;
const locale = process.env[twoHostE2eLocaleEnvironmentKey] === "fa" ? "fa" : "en";
const text = usersMessageCatalogs[locale].users.management;
const auditText = securityMessageCatalogs[locale].security.platformAudit;
const deletePath = (id: string) => generatedOperationPath(usersDeleteUser).replace("${userId}", encodeURIComponent(id));

async function mutation(page: Page, path: string, body: unknown): Promise<number> {
  return page.evaluate(async ({ path, body, tokenPath, tenantHeader }) => {
    const headers = { [tenantHeader]: location.host };
    const token = await fetch(tokenPath, { credentials: "include", headers }).then(response => response.json()) as { headerName: string; requestToken: string };
    const response = await fetch(path, { method: "POST", credentials: "include", headers: {
      ...headers, "Content-Type": "application/json", [token.headerName]: token.requestToken,
    }, body: JSON.stringify(body) });
    return response.status;
  }, { path, body, tokenPath: getAuthenticationAntiforgeryGetQueryKey()[0], tenantHeader: TENANT_HOST_HEADER });
}

async function records(page: Page, subjectId: string, eventName: string): Promise<SecurityAuditEventReadDto[]> {
  const params: AuditPlatformSecurityEventsListParams = {
    page: 1, pageSize: 25, sortBy: auditSortFields.occurredAt, descending: true, uncataloguedOnly: false,
    subjectId, eventName, operation: deletionOperation,
  };
  const query = new URLSearchParams(Object.entries(params).map(([key, value]) => [key, String(value)]));
  const response = await page.request.get(`${origin}${getAuditPlatformSecurityEventsListQueryKey(params)[0]}?${query}`, {
    headers: { [TENANT_HOST_HEADER]: new URL(origin).host },
  });
  expect(response.status()).toBe(200);
  return (await response.json() as { items: SecurityAuditEventReadDto[] }).items;
}

test(`platform account deletion, refusal and attributable audit detail ${locale}`, async ({ page }, testInfo) => {
  const password = process.env[twoHostE2ePasswordEnvironmentKey];
  expect(password, "The disposable host supplies the fixture password.").toBeTruthy();
  await page.context().addCookies([{ name: localeCookieName, value: locale, url: origin }]);
  await page.setExtraHTTPHeaders({ [forwardedForHeaderName]: twoHostE2eAccountDeletionClientIp });
  await page.goto(`${origin}${Routes.LoginPassword}`);
  await page.locator('input[name="username"]').fill(administrator);
  await page.locator('input[name="password"]').fill(password!);
  const [signIn] = await Promise.all([
    page.waitForResponse(response => response.url().endsWith(generatedOperationPath(authenticationLoginPassword)) && response.request().method() === "POST"),
    page.locator('button[type="submit"]').click(),
  ]);
  expect(signIn.status()).toBe(200);
  const sessionResponse = await page.request.get(`${origin}${getAuthenticationSessionGetQueryKey()[0]}`, { headers: { [TENANT_HOST_HEADER]: new URL(origin).host } });
  const session = await sessionResponse.json() as AuthenticationSessionDto;
  expect(session.isAuthenticated).toBe(true);
  const actorId = session.user!.userId;
  try {
    // Self-deletion proves a denial through the real application and its confirmation UI.
    await page.goto(`${origin}${Routes.Users.Detail(actorId)}`);
    await page.getByRole("button", { name: text.deleteAccount, exact: true }).click();
    const [denial] = await Promise.all([
      page.waitForResponse(response => response.url().endsWith(deletePath(actorId)) && response.request().method() === "POST"),
      page.getByRole("dialog").getByRole("button", { name: text.deleteAccountConfirmAction, exact: true }).click(),
    ]);
    expect(denial.status()).toBe(409);
    await expect(page.getByText(text.deleteSelf, { exact: true })).toBeVisible();
    const denied = await records(page, actorId, deletionConflict);
    expect(denied).toEqual(expect.arrayContaining([expect.objectContaining({ actorId, subjectId: actorId, outcome: "denied" })]));

    // This scenario owns the newly created account; no shared patient/staff identity is erased.
    const contact = `account-deletion-${randomUUID()}@example.test`;
    await page.goto(`${origin}${Routes.Users.New}`);
    await page.getByRole("textbox", { name: text.emailContact, exact: true }).fill(contact);
    const [created] = await Promise.all([
      page.waitForResponse(response => response.url().endsWith(generatedOperationPath(usersAdministrationCreateWithEmail)) && response.request().method() === "POST"),
      page.getByRole("button", { name: text.createUser, exact: true }).click(),
    ]);
    expect(created.status()).toBe(200);
    const subject = await created.json() as UserSummaryResponse;
    await page.goto(`${origin}${Routes.Users.Detail(subject.id)}`);
    await page.getByRole("button", { name: text.deleteAccount, exact: true }).click();
    await page.getByRole("dialog").getByRole("button", { name: text.cancel, exact: true }).click();
    await expect(page.getByRole("button", { name: text.deleteAccount, exact: true })).toBeEnabled();
    await page.getByRole("button", { name: text.deleteAccount, exact: true }).click();
    const [deleted] = await Promise.all([
      page.waitForResponse(response => response.url().endsWith(deletePath(subject.id)) && response.request().method() === "POST"),
      page.getByRole("dialog").getByRole("button", { name: text.deleteAccountConfirmAction, exact: true }).click(),
    ]);
    expect(deleted.status()).toBe(200);
    await expect(page).toHaveURL(`${origin}${Routes.Users.Index}`);
    const search = await page.request.get(`${origin}${getUsersSearchUsersQueryKey()[0]}`, {
      params: { Query: contact }, headers: { [TENANT_HOST_HEADER]: new URL(origin).host },
    });
    expect(search.status()).toBe(200);
    expect(await search.json()).toEqual([]);
    const events = await records(page, subject.id, deletionEvent);
    expect(events).toHaveLength(1);
    const event = events[0];
    expect(event).toMatchObject({ actorId, subjectId: subject.id, outcome: "succeeded", sourceIpAddress: twoHostE2eAccountDeletionClientIp,
      changes: [{ field: "accountStatus", previousValue: "enabled", newValue: "deleted" }] });
    expect(Number.isFinite(Date.parse(event.occurredAt!))).toBe(true);
    expect(JSON.stringify(event)).not.toContain(contact);

    await page.goto(`${origin}${Routes.Audit}`);
    await page.getByRole("textbox", { name: auditText.eventId, exact: true }).fill(event.id!);
    await page.getByRole("button", { name: auditText.applyFilters, exact: true }).click();
    await page.getByRole("row").filter({ hasText: localizePlatformAuditEvent(locale, deletionEvent, deletionEvent) }).first().click();
    const detail = page.getByRole("dialog");
    await expect(detail).toHaveAccessibleName(auditText.detailTitle);
    await expect(detail.getByText(subject.id, { exact: true })).toBeVisible();
    await expect(detail.getByText(actorId, { exact: true })).toBeVisible();
    await expect(detail.getByText(twoHostE2eAccountDeletionClientIp, { exact: true })).toBeVisible();
    await expect(detail.getByText(localizePlatformAuditChangeValue(locale, "deleted", "deleted"), { exact: true })).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath("account-deletion-audit-detail.png"), fullPage: true });
  } finally {
    expect(await mutation(page, generatedOperationPath(authenticationLogoutPost), { logoutId: null, returnUrl: null })).toBe(200);
  }
});
