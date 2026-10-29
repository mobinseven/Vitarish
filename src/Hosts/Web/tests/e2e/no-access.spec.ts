import { existsSync, readFileSync, statSync } from "node:fs";
import { expect, test } from "@playwright/test";
import { twoHostE2eApiOriginEnvironmentKey, twoHostE2eClinicOrigin } from "../../scripts/two-host-e2e-artifacts.mjs";
import { REQUEST_ID_HEADER, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { authenticationLoginEmailBegin, authenticationLoginEmailComplete, getAuthenticationSessionGetQueryKey } from "../../shared/auth/src/api/generated/authentication/authentication";
import { generatedOperationPath } from "./generatedOperationPath";
import { forwardedForHeaderName } from "./signInHost";

const apiOrigin = process.env[twoHostE2eApiOriginEnvironmentKey];
// The E2E clinic's own host; its sign-in pages redirect to the shared sign-in host, which admits
// accounts without a role and sends them on to create a clinic (ADR-0018), so the clinic host's
// refusal is proven at its API.
const clinicHost = new URL(twoHostE2eClinicOrigin).hostname;
const smtpCapturePath = process.env.VITARISH_E2E_SMTP_CAPTURE_PATH;
const clientIp = "198.51.100.95";

async function readDeliveredCode(previousModifiedAt: number): Promise<string> {
  await expect.poll(() => {
    return Boolean(smtpCapturePath && existsSync(smtpCapturePath) && statSync(smtpCapturePath).mtimeMs > previousModifiedAt);
  }).toBe(true);

  const message = readFileSync(smtpCapturePath!, "utf8");
  const code = /Your verification code is:\s*(?:<strong>)?([^<\s]+)(?:<\/strong>)?/i.exec(message)?.[1];
  expect(code).toEqual(expect.any(String));
  return code!;
}

// Email-code sign-in auto-creates the account, so this scenario owns a brand
// new user with no global role and no tenant membership and needs no other
// spec's state. Its own client IP keeps the begin rate-limit partition shared
// with no earlier scenario.
test("an account with no role or tenant membership is refused on a clinic host after the code verifies", async ({ request }) => {
  expect(smtpCapturePath, "VITARISH_E2E_SMTP_CAPTURE_PATH must be supplied by the two-host runner").toBeTruthy();
  expect(apiOrigin, "the two-host runner supplies the real API origin").toBeTruthy();
  const headers = { [TENANT_HOST_HEADER]: clinicHost, [forwardedForHeaderName]: clientIp };

  const email = `e2e-no-access-${Date.now()}@example.test`;
  const previousModifiedAt = existsSync(smtpCapturePath!) ? statSync(smtpCapturePath!).mtimeMs : 0;
  const begin = await request.post(new URL(generatedOperationPath(authenticationLoginEmailBegin), apiOrigin).href, {
    data: { email, returnUrl: null },
    headers: { ...headers, [REQUEST_ID_HEADER]: `e2e-no-access-begin-${Date.now()}` },
    ignoreHTTPSErrors: true,
  });
  expect(begin.status()).toBe(200);
  const { challengeId } = await begin.json() as { challengeId: string };

  const code = await readDeliveredCode(previousModifiedAt);
  const complete = await request.post(new URL(generatedOperationPath(authenticationLoginEmailComplete), apiOrigin).href, {
    data: { challengeId, code, returnUrl: null },
    headers: { ...headers, [REQUEST_ID_HEADER]: `e2e-no-access-complete-${Date.now()}` },
    ignoreHTTPSErrors: true,
  });
  // The code was accepted; the account itself is refused, and no session starts.
  expect(complete.status()).toBe(403);
  await expect(complete.json()).resolves.toMatchObject({ code: "no_access" });

  const session = await request.get(new URL(getAuthenticationSessionGetQueryKey()[0], apiOrigin).href, {
    headers: { [TENANT_HOST_HEADER]: clinicHost },
    ignoreHTTPSErrors: true,
  });
  expect(session.status()).toBe(401);
});
