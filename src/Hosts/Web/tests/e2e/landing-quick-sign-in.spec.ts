import { expect, test, type Page } from "@playwright/test";
import crypto from "node:crypto";
import { existsSync, readFileSync, statSync } from "node:fs";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { twoHostE2eSmsCapturePathEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import { TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import {
  authenticationAntiforgeryGet,
  authenticationLoginPhoneBegin,
  authenticationLoginPhoneComplete,
  authenticationSessionGet,
} from "../../shared/auth/src/api/generated/authentication/authentication";
import { intelligenceGetLiveTranscriptionCapabilities } from "../../shared/intelligence/src/api/generated/intelligence/intelligence";
import { landingIssueOnboardingHandoff } from "../../shared/platform/src/api/generated/landing/landing";

// The E2E host binds the landing tenant to landing.localhost and the onboarding tenant to start.localhost.
const landingOrigin = "http://landing.localhost:3005";
const onboardingHostname = "start.localhost";
const landingClientIp = "198.51.100.91";
const smsCapturePath = process.env[twoHostE2eSmsCapturePathEnvironmentKey];

type CapturedSms = { Recipient: string; Text: string };
type ApiResult = { status: number; body: unknown };

function generatedPath(operation: { toString(): string }): string {
  const match = /url:\s*[`"']([^`"']+)[`"']/.exec(operation.toString());
  if (!match) throw new Error("The generated client no longer exposes this operation's URL.");
  return match[1];
}

/** Calls the API the way the landing page does: same origin, the page's tenant host, antiforgery on writes. */
async function callApi(page: Page, method: "GET" | "POST", path: string, body?: unknown): Promise<ApiResult> {
  return page.evaluate(
    async ({ tenantHostHeader, antiforgeryPath, requestMethod, requestPath, requestBody, requestId }) => {
      // Audited endpoints on a resolved tenant require a request id, as the shared API client sends.
      const headers: Record<string, string> = { [tenantHostHeader]: window.location.host, "X-Request-Id": requestId };
      if (requestMethod !== "GET") {
        const antiforgeryResponse = await fetch(antiforgeryPath, { credentials: "include", headers });
        const antiforgery = (await antiforgeryResponse.json()) as { headerName: string; requestToken: string };
        headers[antiforgery.headerName] = antiforgery.requestToken;
        headers["Content-Type"] = "application/json";
      }
      const response = await fetch(requestPath, {
        method: requestMethod,
        credentials: "include",
        headers,
        body: requestBody === undefined ? undefined : JSON.stringify(requestBody),
      });
      const text = await response.text();
      return { status: response.status, body: text ? JSON.parse(text) : null };
    },
    {
      tenantHostHeader: TENANT_HOST_HEADER,
      antiforgeryPath: generatedPath(authenticationAntiforgeryGet),
      requestMethod: method,
      requestPath: path,
      requestBody: body,
      // Made here: inside the page, `crypto` would name this file's Node import.
      requestId: `e2e-landing-${crypto.randomUUID()}`,
    },
  );
}

async function readDeliveredSmsCode(previousModifiedAt: number, nationalDigits: string): Promise<string> {
  await expect.poll(() => Boolean(smsCapturePath && existsSync(smsCapturePath) && statSync(smsCapturePath).mtimeMs > previousModifiedAt)).toBe(true);
  const lines = readFileSync(smsCapturePath!, "utf8").trim().split(/\r?\n/).filter(Boolean).map((line) => JSON.parse(line) as CapturedSms);
  const code = [...lines].reverse().find((entry) => entry.Recipient.endsWith(nationalDigits))?.Text;
  expect(code).toEqual(expect.stringMatching(/^\d+$/));
  return code!;
}

test("a new phone number signs in on the landing host without a clinic and is handed off to clinic sign-up", async ({ page }) => {
  test.setTimeout(300_000);
  expect(smsCapturePath, `${twoHostE2eSmsCapturePathEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();
  const nationalDigits = `912${Array.from({ length: 7 }, () => crypto.randomInt(0, 10)).join("")}`;
  const handoffPath = generatedPath(landingIssueOnboardingHandoff);

  await page.route(/^http:\/\/landing\.localhost:3005\//, (route) =>
    route.continue({ headers: { ...route.request().headers(), "X-Forwarded-For": landingClientIp } }),
  );
  await page.goto(`${landingOrigin}${Routes.Login.Index}`, { waitUntil: "domcontentloaded", timeout: 120_000 });

  // Anonymous visitors cannot ask for a handoff.
  expect((await callApi(page, "POST", handoffPath)).status).toBe(401);

  const previousSmsModifiedAt = smsCapturePath && existsSync(smsCapturePath) ? statSync(smsCapturePath).mtimeMs : 0;
  const begin = await callApi(page, "POST", generatedPath(authenticationLoginPhoneBegin), { phoneNumber: `0${nationalDigits}`, returnUrl: null });
  expect(begin.status).toBe(200);
  const challengeId = (begin.body as { challengeId: string }).challengeId;

  const code = await readDeliveredSmsCode(previousSmsModifiedAt, nationalDigits);
  const complete = await callApi(page, "POST", generatedPath(authenticationLoginPhoneComplete), { challengeId, code, returnUrl: null });
  expect(complete.status, `the landing tenant admits a session without a role: ${JSON.stringify(complete.body)}`).toBe(200);

  const session = await callApi(page, "GET", generatedPath(authenticationSessionGet));
  expect(session.status).toBe(200);
  expect((session.body as { isAuthenticated: boolean }).isAuthenticated).toBe(true);

  // Dictation needs no clinic (ADR-0017): the capability check answers instead of refusing the session.
  const dictation = await callApi(page, "GET", generatedPath(intelligenceGetLiveTranscriptionCapabilities));
  expect(dictation.status).toBe(200);

  const handoff = await callApi(page, "POST", handoffPath);
  expect(handoff.status).toBe(200);
  expect(handoff.body).toEqual({ hostname: onboardingHostname, token: expect.stringMatching(/\S{16,}/) });
});
