import { expect, test, type Page } from "@playwright/test";
import { existsSync, mkdirSync } from "node:fs";
import { dirname } from "node:path";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { twoHostE2eApiOriginEnvironmentKey, twoHostE2eProofScreenshotEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import { defaultClinicOrigin, forwardedForHeaderName, signInToClinic } from "./signInHost";

const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const password = process.env.VITARISH_E2E_PASSWORD;
const apiOrigin = process.env[twoHostE2eApiOriginEnvironmentKey] ?? "https://localhost:7272";
// Seeded by the E2E host (E2eFixtureSeeder) for tenant A.
const seededPatientId = "2d7a3eb9-1d47-4c63-9a45-19d7e6a0fb18";
const medicalApi = "/api/modules/medical";
// Seeded Tamin catalog drug and the values the E2E Tamin stub accepts for its dosage form.
const drugName = "E2E Ibuprofen 400 mg";
const taminAuthorizeUrl = "https://account.tamin.ir/**";
const doctorMobile = "09121111111";
const messages = clinicMessageCatalogs.en.clinic;

async function login(page: Page) {
  expect(password).toBeTruthy();
  await page.setExtraHTTPHeaders({ [forwardedForHeaderName]: "198.51.100.52" });
  await signInToClinic(page, ownerUsername, password!);
}

async function captureProof(page: Page, name: string) {
  const proofScreenshot = process.env[twoHostE2eProofScreenshotEnvironmentKey]?.replace(/\.png$/i, `-${name}.png`);
  if (!proofScreenshot) return;
  mkdirSync(dirname(proofScreenshot), { recursive: true });
  expect(existsSync(proofScreenshot), `refusing to overwrite retained evidence at ${proofScreenshot}`).toBe(false);
  await page.screenshot({ path: proofScreenshot, fullPage: true });
}

async function selectOption(page: Page, label: string, option: string | RegExp) {
  await page.getByRole("button", { name: new RegExp(label) }).first().click();
  await page.getByRole("option", { name: option }).first().click();
}

test("a doctor connects Tamin and prescribes a drug with its consumption values", async ({ page, request }) => {
  // One test walks the whole feature, and next dev compiles each page on its first visit.
  test.setTimeout(600_000);

  // Bypass denial: the dosage-form lookup runs under the caller's own Tamin login, so it is never anonymous.
  const anonymous = await request.post(`${apiOrigin}${medicalApi}/providers/drug-consumption-options`, {
    data: { providerKey: "TAMIN", formCode: "17" },
    ignoreHTTPSErrors: true,
  });
  expect(anonymous.status()).toBe(401);

  await login(page);

  // Connect Tamin: the real credential setup, then the real OAuth callback with the stubbed authorize redirect.
  await page.route(taminAuthorizeUrl, async (route) => {
    const state = new URL(route.request().url()).searchParams.get("state");
    expect(state, "the authorize redirect must carry the OAuth state").toBeTruthy();
    await route.fulfill({
      status: 302,
      headers: { location: `${defaultClinicOrigin}${ClinicRoutes.Clinic.Settings.InsuranceCallback}?code=e2e-code&state=${state}` },
    });
  });
  await page.goto(`${defaultClinicOrigin}${ClinicRoutes.Clinic.Settings.Insurance}`, { waitUntil: "domcontentloaded" });
  await page.getByLabel(messages.settings.insurance.doctorIdentifier).fill("E2E-DOC-1");
  await page.getByLabel(messages.settings.insurance.nationalCode).fill("0012345678");
  await page.getByLabel(messages.settings.insurance.doctorMobile).fill(doctorMobile);
  await page.getByLabel(messages.settings.insurance.clinicIdentifier).fill("E2E-CLINIC-1");
  await captureProof(page, "tamin-settings");
  await page.getByRole("button", { name: messages.settings.insurance.authorize }).click();
  await expect(page.getByText(messages.settings.insurance.completed)).toBeVisible({ timeout: 120_000 });
  await captureProof(page, "tamin-connected");

  // Prescribe: pick the drug, choose its consumption values from the provider's lists, submit.
  await page.goto(`${defaultClinicOrigin}${ClinicRoutes.Clinic.Patients.Detail(seededPatientId, "prescription")}`, { waitUntil: "domcontentloaded" });
  await page.getByRole("button", { name: "TAMIN" }).click();
  await page.getByPlaceholder(messages.prescriptionSubmission.search).fill("Ibuprofen");
  await page.getByRole("option", { name: new RegExp(drugName) }).click();

  await expect(page.getByRole("heading", { name: `Drug usage for ${drugName}` })).toBeVisible({ timeout: 60_000 });
  const add = page.getByRole("button", { name: messages.prescriptionSubmission.consumption.add });
  await expect(add, "a drug cannot be added before its consumption values are chosen").toBeDisabled();
  await selectOption(page, messages.prescriptionSubmission.consumption.amount, "One quarter");
  await selectOption(page, messages.prescriptionSubmission.consumption.instruction, "Night");
  await selectOption(page, messages.prescriptionSubmission.consumption.usage, "Intra-articular");
  await expect(add).toBeEnabled();
  await captureProof(page, "drug-consumption-picker");
  await add.click();
  await expect(page.getByText("One quarter · Night")).toBeVisible();

  await page.getByRole("button", { name: messages.prescriptionSubmission.submit }).click();
  // On success the sheet closes and the patient feed shows the provider-accepted prescription with its tracking code.
  await expect(page.getByText(/Tracking: 987654/)).toBeVisible({ timeout: 120_000 });
  await expect(page.getByText("Accepted").first()).toBeVisible();
  await captureProof(page, "drug-prescription-submitted");
});
