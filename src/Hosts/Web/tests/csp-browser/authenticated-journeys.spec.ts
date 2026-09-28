import { expect, test } from "@playwright/test";
import { Routes as OwnerRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { Routes as PlatformAdminRoutes } from "../../apps/platform-admin/src/app/routes";
import { cspBrowserEnvironment } from "../../scripts/csp-browser-environment.cjs";
import { cspPolicyProblems, installCspObserver, observeCsp, renderingProblems } from "./csp-evidence";

const appOrigins = JSON.parse(process.env[cspBrowserEnvironment.appOrigins] ?? "{}") as Record<string, string>;
const e2ePassword = process.env.VITARISH_E2E_PASSWORD;
const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const platformAdminUsername = process.env.VITARISH_E2E_PLATFORM_ADMIN_USERNAME ?? "e2e-platform-admin";

test("keeps owner theme overlays and platform-admin pages report free after sign-in", async ({ browser }, testInfo) => {
  expect(e2ePassword, "the real E2E host supplied an isolated test password").toBeTruthy();
  const issues: string[] = [];
  const ownerPage = await browser.newPage({ colorScheme: "light" });
  const ownerObserver = observeCsp(ownerPage);
  await installCspObserver(ownerPage);

  try {
    await signIn(ownerPage, appOrigins["private-practice-owner"], OwnerRoutes.Login.Password, ownerUsername, e2ePassword!);
    const ownerResponse = await ownerPage.goto(`${appOrigins["private-practice-owner"]}${OwnerRoutes.Home}`, { waitUntil: "load" });
    await expect(ownerPage.getByRole("button", { name: "Commands" })).toBeVisible();
    const stylesBeforeOverlay = await ownerPage.locator("style").count();
    await ownerPage.getByRole("button", { name: "Commands" }).click();
    await expect(ownerPage.getByRole("dialog")).toBeVisible();
    await ownerPage.getByText("Toggle Theme", { exact: true }).click();
    await expect(ownerPage.locator("html")).toHaveAttribute("data-theme", "dark");
    const stylesAfterOverlay = await ownerPage.locator("style").count();
    if (stylesAfterOverlay < stylesBeforeOverlay) issues.push("owner command overlay removed existing styles unexpectedly");
    await ownerPage.keyboard.press("Escape");
    await expect(ownerPage.getByRole("dialog")).toBeHidden();

    const ownerEvidence = await ownerObserver.document(ownerResponse);
    for (const problem of [...cspPolicyProblems(ownerEvidence), ...renderingProblems(ownerEvidence)]) issues.push(`owner authenticated home: ${problem}`);
    await testInfo.attach(`owner-${testInfo.project.name}-evidence`, {
      body: Buffer.from(JSON.stringify({ ...ownerEvidence, stylesBeforeOverlay, stylesAfterOverlay }, null, 2)),
      contentType: "application/json",
    });
    const ownerScreenshot = testInfo.outputPath(`owner-authenticated-${testInfo.project.name}.png`);
    await ownerPage.screenshot({ path: ownerScreenshot, fullPage: true });
    await testInfo.attach(`owner-authenticated-${testInfo.project.name}-render`, { path: ownerScreenshot, contentType: "image/png" });
  } finally {
    await ownerPage.close();
  }

  const adminPage = await browser.newPage();
  const adminObserver = observeCsp(adminPage);
  await installCspObserver(adminPage);
  try {
    await signIn(adminPage, appOrigins["platform-admin"], PlatformAdminRoutes.LoginPassword, platformAdminUsername, e2ePassword!, PlatformAdminRoutes.Audit);
    const adminResponse = await adminPage.goto(`${appOrigins["platform-admin"]}${PlatformAdminRoutes.Tenants.Index}`, { waitUntil: "load" });
    await expect(adminPage.locator("main")).toContainText(/tenants/i);
    const adminEvidence = await adminObserver.document(adminResponse);
    for (const problem of [...cspPolicyProblems(adminEvidence), ...renderingProblems(adminEvidence)]) issues.push(`platform-admin tenants: ${problem}`);
    await testInfo.attach(`platform-admin-${testInfo.project.name}-evidence`, {
      body: Buffer.from(JSON.stringify(adminEvidence, null, 2)),
      contentType: "application/json",
    });
    const adminScreenshot = testInfo.outputPath(`platform-admin-authenticated-${testInfo.project.name}.png`);
    await adminPage.screenshot({ path: adminScreenshot, fullPage: true });
    await testInfo.attach(`platform-admin-authenticated-${testInfo.project.name}-render`, { path: adminScreenshot, contentType: "image/png" });
  } finally {
    await adminPage.close();
  }

  expect(issues, "authenticated UI, overlay, theme, and CSP policy failures").toEqual([]);
});

test("renders patient image and video blob previews and completes live dictation", async ({ browser, browserName }, testInfo) => {
  test.skip(browserName !== "chromium", "fake microphone capture is enabled for the Chromium E2E project");
  expect(e2ePassword, "the real E2E host supplied an isolated test password").toBeTruthy();
  const origin = appOrigins["private-practice-owner"];
  const page = await browser.newPage();
  const observer = observeCsp(page);
  await installCspObserver(page);
  await page.context().grantPermissions(["microphone"], { origin });
  let documentResponse: import("@playwright/test").Response | null = null;
  page.on("response", (response) => {
    if (response.request().isNavigationRequest() && response.frame() === page.mainFrame()) documentResponse = response;
  });
  try {
    await signIn(page, origin, OwnerRoutes.Login.Password, ownerUsername, e2ePassword!);
    await page.goto(`${origin}${OwnerRoutes.Clinic.Patients.Index}`, { waitUntil: "load" });
    await page.getByText("E2E Patient", { exact: true }).click();
    await expect(page).toHaveURL(/\/patients\/[0-9a-f-]+(?:\?|$)/i);
    await expect(page.getByRole("button", { name: "New Procedure" })).toBeVisible();
    await page.getByRole("button", { name: "Open procedure", exact: true }).click();
    await page.getByRole("button", { name: "E2E CSP preview event", exact: true }).click();
    await expect(page.getByRole("button", { name: "Attachments", exact: true })).toBeVisible();
    await page.getByRole("button", { name: "Attachments", exact: true }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
    const fileInput = page.locator('input[type="file"]').last();
    const imageBytes = await makePng(page);
    await fileInput.setInputFiles({ name: "csp-preview.png", mimeType: "image/png", buffer: imageBytes });
    const imagePreview = page.locator('img[src^="blob:"]');
    await expect(imagePreview).toBeVisible();
    await expect.poll(() => imagePreview.evaluate((image: HTMLImageElement) => image.naturalWidth)).toBeGreaterThan(0);
    const imageWidth = await imagePreview.evaluate((image: HTMLImageElement) => image.naturalWidth);
    const imageScreenshot = testInfo.outputPath(`patient-image-blob-preview-${testInfo.project.name}.png`);
    await page.screenshot({ path: imageScreenshot, fullPage: true });
    await testInfo.attach(`patient-image-blob-preview-${testInfo.project.name}-render`, { path: imageScreenshot, contentType: "image/png" });

    const videoBytes = await makeWebm(page);
    await fileInput.setInputFiles({ name: "csp-preview.webm", mimeType: "video/webm", buffer: videoBytes });
    const videoPreview = page.locator('video[src^="blob:"]');
    await expect(videoPreview).toBeVisible();
    await expect.poll(() => videoPreview.evaluate((video: HTMLVideoElement) => video.readyState)).toBeGreaterThanOrEqual(2);
    await expect.poll(() => videoPreview.evaluate((video: HTMLVideoElement) => video.duration)).toBeGreaterThan(0);
    const videoDuration = await videoPreview.evaluate((video: HTMLVideoElement) => video.duration);
    const videoScreenshot = testInfo.outputPath(`patient-video-blob-preview-${testInfo.project.name}.png`);
    await page.screenshot({ path: videoScreenshot, fullPage: true });
    await testInfo.attach(`patient-video-blob-preview-${testInfo.project.name}-render`, { path: videoScreenshot, contentType: "image/png" });

    const attachmentsRegion = page.getByRole("region", { name: "Attachments" });
    const startDictation = attachmentsRegion.getByLabel("Start voice dictation", { exact: true });
    await expect(startDictation).toBeEnabled();
    await startDictation.click();
    await page.getByRole("button", { name: "English", exact: true }).click();
    const stopDictation = page.getByLabel("Stop voice dictation", { exact: true });
    await expect(stopDictation).toBeEnabled({ timeout: 20_000 });
    await page.waitForTimeout(700);
    await stopDictation.click();
    await expect(page.getByRole("textbox", { name: "Note" })).toHaveValue(/\S/, { timeout: 20_000 });

    const evidence = await observer.document(documentResponse);
    const problems = [...cspPolicyProblems(evidence), ...renderingProblems(evidence)];
    await testInfo.attach(`procedure-blob-and-dictation-${testInfo.project.name}`, {
      body: Buffer.from(JSON.stringify({ ...evidence, imageWidth, videoDuration }, null, 2)),
      contentType: "application/json",
    });
    const violations = evidence.violations.map((violation) => `${violation.effectiveDirective}:${violation.blockedURI}`).join(", ");
    expect(problems, `the authenticated preview and transcription journey remains free of CSP and rendering failures: ${violations}`).toEqual([]);
    const screenshot = testInfo.outputPath(`procedure-blob-and-dictation-${testInfo.project.name}.png`);
    await page.screenshot({ path: screenshot, fullPage: true });
    await testInfo.attach(`procedure-blob-and-dictation-${testInfo.project.name}-render`, { path: screenshot, contentType: "image/png" });
  } finally {
    await page.close();
  }
});

async function signIn(page: import("@playwright/test").Page, origin: string, loginRoute: string, username: string, password: string, postLoginRoute?: string): Promise<void> {
  await page.goto(`${origin}${loginRoute}`, { waitUntil: "load" });
  const usernameInput = page.locator('input[name="username"]');
  const passwordInput = page.locator('input[name="password"]');
  const appOrigin = new URL(origin).origin;
  await usernameInput.click();
  await usernameInput.pressSequentially(username);
  await expect(usernameInput, "the sign-in form retains the entered account name").toHaveValue(username);
  await passwordInput.fill(password);
  await expect(passwordInput, "the sign-in form retains a nonempty test password").toHaveValue(/\S/);
  const [response] = await Promise.all([
    page.waitForResponse((candidate) => candidate.request().method() === "POST" && new URL(candidate.url()).origin === appOrigin, { timeout: 20_000 }),
    ...(postLoginRoute ? [page.waitForURL((url) => url.origin === new URL(origin).origin && url.pathname === postLoginRoute, { timeout: 20_000 })] : []),
    page.locator('button[type="submit"]').click(),
  ]);
  expect(response.status(), "E2E authentication succeeded through the ingress proxy").toBe(200);
  await expect.poll(async () => {
    const cookies = await page.context().cookies(origin);
    return cookies.some((cookie) => cookie.httpOnly && cookie.secure && cookie.sameSite === "None");
  }, { message: "successful sign-in persisted a secure HTTP-only SameSite=None browser session cookie", timeout: 5_000 }).toBe(true);
  if (postLoginRoute) {
    await expect(page.getByRole("heading", { name: "Global security audit", exact: true }), "the platform-admin session resolves before following an authenticated route").toBeVisible({ timeout: 20_000 });
  }
}

async function makePng(page: import("@playwright/test").Page): Promise<Buffer> {
  const base64 = await page.evaluate(() => {
    const canvas = document.createElement("canvas");
    canvas.width = 4;
    canvas.height = 4;
    const context = canvas.getContext("2d");
    if (!context) throw new Error("canvas context unavailable");
    context.fillStyle = "#2878c8";
    context.fillRect(0, 0, canvas.width, canvas.height);
    return canvas.toDataURL("image/png").split(",")[1];
  });
  return Buffer.from(base64, "base64");
}

async function makeWebm(page: import("@playwright/test").Page): Promise<Buffer> {
  const bytes = await page.evaluate(async () => {
    const canvas = document.createElement("canvas");
    canvas.width = 16;
    canvas.height = 16;
    const context = canvas.getContext("2d");
    if (!context || !canvas.captureStream || !MediaRecorder.isTypeSupported("video/webm"))
      throw new Error("browser cannot generate a WebM preview fixture");
    const stream = canvas.captureStream(8);
    const recorder = new MediaRecorder(stream, { mimeType: "video/webm" });
    const chunks: Blob[] = [];
    recorder.addEventListener("dataavailable", (event) => { if (event.data.size > 0) chunks.push(event.data); });
    const stopped = new Promise<void>((resolve) => recorder.addEventListener("stop", () => resolve(), { once: true }));
    recorder.start();
    const interval = window.setInterval(() => {
      context.fillStyle = `rgb(${Math.floor(Math.random() * 255)}, 80, 120)`;
      context.fillRect(0, 0, canvas.width, canvas.height);
    }, 100);
    await new Promise((resolve) => window.setTimeout(resolve, 900));
    window.clearInterval(interval);
    recorder.stop();
    await stopped;
    for (const track of stream.getTracks()) track.stop();
    return Array.from(new Uint8Array(await new Blob(chunks, { type: "video/webm" }).arrayBuffer()));
  });
  return Buffer.from(bytes);
}
