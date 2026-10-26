import { expect, test, type Page } from "@playwright/test";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { twoHostE2eLocaleEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import { TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { signInToClinic } from "./signInHost";

// In-page requests use the web origin's same-origin /api, as in production.
const browserApiOrigin = "";
const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const password = process.env.VITARISH_E2E_PASSWORD;
const locale = process.env[twoHostE2eLocaleEnvironmentKey] === "fa" ? "fa" : "en";
const expectedMaximumContentBytes = 512 * 1024 * 1024;
// FilesErrorCode.UnsupportedContentType: the contract enum has no string converter, so its ordinal is the wire value.
const unsupportedContentTypeOrdinal = 10;

// A real 1x1 PNG, so the declared type, extension and leading signature all pass.
const pngBytes = [
  0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
  0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
  0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
  0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
  0x89, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x44, 0x41,
  0x54, 0x78, 0xDA, 0x63, 0x64, 0x60, 0xF8, 0x5F,
  0x0F, 0x00, 0x02, 0x87, 0x01, 0x80, 0xEB, 0x47,
  0xBA, 0x92, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45,
  0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82,
];

type UploadPolicy = {
  maximumContentBytes: number;
  formats: Array<{ mediaType: string; extensions: string[] }>;
};
type UploadResult = { status: number; body: Record<string, unknown> | null };

async function login(page: Page): Promise<void> {
  await page.context().addCookies([
    { name: "vitarish.locale", value: locale, url: "http://localhost:3005/" },
  ]);
  expect(password, "VITARISH_E2E_PASSWORD must be supplied by the disposable E2E environment").toBeTruthy();
  await signInToClinic(page, ownerUsername, password!);
}

async function readUploadPolicy(page: Page): Promise<{ status: number; body: UploadPolicy }> {
  return page.evaluate(async ({ origin, tenantHostHeader }) => {
    const response = await fetch(`${origin}/api/modules/files/upload-policy`, {
      credentials: "include",
      headers: { [tenantHostHeader]: window.location.host },
    });
    return { status: response.status, body: await response.json() as UploadPolicy };
  }, { origin: browserApiOrigin, tenantHostHeader: TENANT_HOST_HEADER });
}

async function uploadFile(page: Page, fileName: string, contentType: string, bytes: number[]): Promise<UploadResult> {
  return page.evaluate(async ({ origin, tenantHostHeader, fileName, contentType, bytes }) => {
    const antiforgeryResponse = await fetch(`${origin}/api/modules/authentication/antiforgery`, { credentials: "include" });
    const antiforgery = await antiforgeryResponse.json() as { headerName: string; requestToken: string };
    const content = Uint8Array.from(bytes);
    const digest = await crypto.subtle.digest("SHA-256", content);
    const checksum = Array.from(new Uint8Array(digest), value => value.toString(16).padStart(2, "0")).join("");
    const form = new FormData();
    form.append("content", new Blob([content], { type: contentType }), fileName);
    form.append("fileName", fileName);
    form.append("contentType", contentType);
    form.append("size", String(content.byteLength));
    form.append("checksum", checksum);
    const response = await fetch(`${origin}/api/modules/files/upload`, {
      method: "POST",
      credentials: "include",
      headers: {
        [tenantHostHeader]: window.location.host,
        "X-Request-Id": `e2e-upload-policy-${crypto.randomUUID()}`,
        [antiforgery.headerName]: antiforgery.requestToken,
      },
      body: form,
    });
    return { status: response.status, body: await response.json().catch(() => null) as Record<string, unknown> | null };
  }, { origin: browserApiOrigin, tenantHostHeader: TENANT_HOST_HEADER, fileName, contentType, bytes });
}

test("the Files upload policy accepts an allowed PNG and rejects disallowed content", async ({ page }) => {
  await login(page);

  const policy = await readUploadPolicy(page);
  expect(policy.status).toBe(200);
  expect(policy.body.maximumContentBytes).toBe(expectedMaximumContentBytes);
  expect(policy.body.formats.some((format) => format.mediaType === "image/png")).toBe(true);

  const accepted = await uploadFile(page, "scan.png", "image/png", pngBytes);
  expect(accepted.status).toBe(200);
  expect((accepted.body?.file as { id?: string } | undefined)?.id).toEqual(expect.any(String));

  // A Windows executable renamed as a PNG must fail the leading-signature check.
  const executableBytes = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00];
  const renamedExecutable = await uploadFile(page, "scan.png", "image/png", executableBytes);
  expect(renamedExecutable.status).toBe(415);
  expect(renamedExecutable.body, "FilesErrorCode.UnsupportedContentType").toMatchObject({
    code: unsupportedContentTypeOrdinal,
    message: "This file type is not accepted.",
    statusCode: 415,
  });

  const disallowed = await uploadFile(page, "notes.txt", "text/plain", Array.from(new TextEncoder().encode("evidence notes")));
  expect(disallowed.status).toBe(415);
  expect(disallowed.body, "FilesErrorCode.UnsupportedContentType").toMatchObject({
    code: unsupportedContentTypeOrdinal,
    message: "This file type is not accepted.",
    statusCode: 415,
  });
});
