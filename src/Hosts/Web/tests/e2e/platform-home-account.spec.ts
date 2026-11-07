import { expect, test, type Page } from "@playwright/test";
import crypto from "node:crypto";
import { existsSync, mkdirSync } from "node:fs";
import { dirname } from "node:path";
import { twoHostE2eLandingOrigin, twoHostE2eProofScreenshotEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import { REQUEST_ID_HEADER, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import {
  authenticationAccountPasswordFirstSet,
  authenticationAntiforgeryGet,
  authenticationSessionGet,
} from "../../shared/auth/src/api/generated/authentication/authentication";
import { signInWithEmailCodeOnSharedHost } from "./emailCodeSignIn";
import { generatedOperationPath } from "./generatedOperationPath";
import { forwardedForHeaderName, submitPasswordOnSignInHost } from "./signInHost";

// E2eFixtureSeeder.PasswordlessEmailAccountUserName and ...Email: a confirmed email, no password, no workspace.
const username = "e2e-platform-home-account-user";
const email = "e2e-platform-home-account-user@example.test";
const clientIp = "198.51.100.92";
const antiforgeryPath = generatedOperationPath(authenticationAntiforgeryGet);
const sessionPath = generatedOperationPath(authenticationSessionGet);
const firstPasswordPath = generatedOperationPath(authenticationAccountPasswordFirstSet);

interface JsonResult {
  status: number;
  body: { succeeded?: boolean; hasPassword?: boolean } | null;
}

/** A same-origin JSON call from the signed-in page, with the antiforgery token the account area's own client sends. */
async function postJson(page: Page, path: string, requestBody: unknown): Promise<JsonResult> {
  return page.evaluate(async ({ tenantHostHeader, requestIdHeader, tokenPath, target, requestId, body }) => {
    const headers = { [tenantHostHeader]: window.location.host, [requestIdHeader]: requestId };
    const antiforgeryResponse = await fetch(tokenPath, { credentials: "include", headers });
    if (!antiforgeryResponse.ok) return { status: antiforgeryResponse.status, body: null };
    const antiforgery = (await antiforgeryResponse.json()) as { headerName: string; requestToken: string };
    const response = await fetch(target, {
      method: "POST",
      credentials: "include",
      headers: { ...headers, "Content-Type": "application/json", [antiforgery.headerName]: antiforgery.requestToken },
      body: JSON.stringify(body),
    });
    const text = await response.text();
    return { status: response.status, body: text ? JSON.parse(text) : null };
  }, {
    tenantHostHeader: TENANT_HOST_HEADER,
    requestIdHeader: REQUEST_ID_HEADER,
    tokenPath: antiforgeryPath,
    target: path,
    requestId: `e2e-platform-home-account-${crypto.randomUUID()}`,
    body: requestBody,
  });
}

// The two-host runner serves landing.localhost from the owner app, not the landing app, so this proves the
// server side the landing app's account area calls: the landing tenant accepts a role-free session for the
// account operations. The page itself is covered by the landing app's component tests.
test("a person with no workspace can set a first password on the landing host and sign in with it", async ({ browser, page }) => {
  test.setTimeout(600_000);
  // A strong value that no other run reuses; it only lives in this test.
  const newPassword = `E2e-${crypto.randomBytes(9).toString("base64url")}-9!`;

  await page.route(/^http:\/\/[^/]*localhost:\d+\//, (route) =>
    route.continue({ headers: { ...route.request().headers(), [forwardedForHeaderName]: clientIp } }),
  );

  // 1. Sign in by email code: a confirmed email, no password, no workspace, and a session on the landing host.
  await signInWithEmailCodeOnSharedHost(page, email);
  expect(new URL(page.url()).origin).toBe(twoHostE2eLandingOrigin);
  const session = await page.evaluate(async ({ tenantHostHeader, target }) => {
    const response = await fetch(target, { credentials: "include", headers: { [tenantHostHeader]: window.location.host } });
    return { status: response.status, body: (await response.json()) as { user?: { hasPassword?: boolean } } };
  }, { tenantHostHeader: TENANT_HOST_HEADER, target: sessionPath });
  expect(session.status).toBe(200);
  expect(session.body.user?.hasPassword, "the account starts with no password").toBe(false);

  // 2. The landing host's session sets the first password.
  const firstSet = await postJson(page, firstPasswordPath, { newPassword });
  expect(firstSet.status).toBe(200);
  expect(firstSet.body?.succeeded).toBe(true);

  // 3. Setting it again is refused: a first password is set once, afterwards it is changed with the current one.
  const secondSet = await postJson(page, firstPasswordPath, { newPassword: `${newPassword}x` });
  expect(secondSet.body?.succeeded ?? false).toBe(false);

  // 4. The new password signs this person in on the shared sign-in host, in a fresh browser.
  const freshContext = await browser.newContext({ extraHTTPHeaders: { [forwardedForHeaderName]: clientIp } });
  try {
    const signInResponse = await submitPasswordOnSignInHost(await freshContext.newPage(), username, newPassword);
    expect(signInResponse.status()).toBe(200);
  } finally {
    await freshContext.close();
  }

  const proof = process.env[twoHostE2eProofScreenshotEnvironmentKey]?.replace(/\.png$/i, "-platform-home-account.png");
  if (proof) {
    mkdirSync(dirname(proof), { recursive: true });
    expect(existsSync(proof), `refusing to overwrite retained evidence at ${proof}`).toBe(false);
    await page.mouse.move(0, 0);
    await page.screenshot({ path: proof, fullPage: true, animations: "disabled" });
  }
});
