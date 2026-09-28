import { expect, test } from "@playwright/test";
import { Routes as OwnerRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { Routes as PatientRoutes } from "../../apps/patients/src/app/routes";
import { Routes as PlatformAdminRoutes } from "../../apps/platform-admin/src/app/routes";
import { Routes as ProjectManagerRoutes } from "../../apps/vitarish-pm/src/app/routes";
import { Routes as KnowledgeDemoRoutes } from "../../apps/knowledge-qa-demo/src/app/routes";
import { Routes as LandingRoutes } from "../../apps/vitarish-landing/src/app/routes";
import { cspBrowserEnvironment } from "../../scripts/csp-browser-environment.cjs";
import { cspPolicyProblems, installCspObserver, observeCsp, renderingProblems, waitForMeaningfulContent } from "./csp-evidence";

const appOrigins = JSON.parse(process.env[cspBrowserEnvironment.appOrigins] ?? "{}") as Record<string, string>;
const publicRoutes: Record<string, string[]> = {
  "private-practice-owner": [OwnerRoutes.Login.Password, OwnerRoutes.Login.Phone, OwnerRoutes.Login.Email, OwnerRoutes.Login.Passkey],
  patients: [PatientRoutes.Home, PatientRoutes.Login.Password, PatientRoutes.Login.Phone, PatientRoutes.Login.Email, PatientRoutes.Login.Passkey],
  "platform-admin": [PlatformAdminRoutes.LoginPassword],
  "vitarish-pm": [ProjectManagerRoutes.Root],
  "knowledge-qa-demo": [KnowledgeDemoRoutes.Home, KnowledgeDemoRoutes.Login, KnowledgeDemoRoutes.KnowledgeQa],
  "vitarish-landing": [LandingRoutes.Home, LandingRoutes.About, LandingRoutes.Portfolio, LandingRoutes.Contact, LandingRoutes.Explore],
};

test("renders public routes through the ingress nonce policy across six apps", async ({ browser }, testInfo) => {
  const issues: string[] = [];
  const apps = Object.entries(publicRoutes);
  expect(Object.keys(appOrigins).sort()).toEqual(apps.map(([name]) => name).sort());
  for (const [appName, paths] of apps) {
    expect(paths.every((path) => typeof path === "string" && path.startsWith("/")), `${appName} routes resolve from its route owner`).toBe(true);
  }

  for (const [appName, paths] of apps) {
    const origin = appOrigins[appName];
    expect(origin, `${appName} proxy origin is configured`).toBeTruthy();
    for (const [routeIndex, routePath] of paths.entries()) {
      const page = await browser.newPage();
      const observer = observeCsp(page);
      await installCspObserver(page);
      try {
        const response = await page.goto(`${origin}${routePath}`, { waitUntil: "load", timeout: 60_000 });
        await waitForMeaningfulContent(page);
        if (appName === "patients") await expect(page.locator("html")).toHaveAttribute("data-theme", "glass-light");
        await page.waitForTimeout(350);
        const evidence = await observer.document(response);
        const routeLabel = `${appName}${routePath}`;
        if (!response || response.status() < 200 || response.status() >= 400) issues.push(`${routeLabel}: returned HTTP ${response?.status() ?? "no response"}`);
        for (const problem of cspPolicyProblems(evidence)) issues.push(`${routeLabel}: ${problem}`);
        for (const problem of renderingProblems(evidence)) issues.push(`${routeLabel}: ${problem}`);

        let nonceRotates = true;
        if (routeIndex === 0) {
          const reloadedResponse = await page.reload({ waitUntil: "load", timeout: 60_000 });
          await waitForMeaningfulContent(page);
          await page.waitForTimeout(350);
          const reloadedEvidence = await observer.document(reloadedResponse);
          nonceRotates = reloadedEvidence.nonceRotatedFromPreviousDocument;
          if (!nonceRotates) issues.push(`${routeLabel}: the nonce did not rotate on a fresh document request`);
          for (const problem of cspPolicyProblems(reloadedEvidence)) issues.push(`${routeLabel} after reload: ${problem}`);
          for (const problem of renderingProblems(reloadedEvidence)) issues.push(`${routeLabel} after reload: ${problem}`);
        }

        const proof = {
          app: appName,
          page: routePath,
          status: response?.status() ?? null,
          reportOnlyPresent: evidence.reportOnlyPresent,
          enforcingHeaderPresent: evidence.enforcingHeaderPresent,
          requestResponseNonceMatched: evidence.requestResponseNonceMatches && evidence.metaNonceMatches,
          nonceRotates,
          inlineStyleCount: evidence.inlineStyleNonceCount,
          inlineScriptCount: evidence.inlineScriptNonceCount,
          visibleTextLength: evidence.visibleTextLength,
          visibleContentPresent: evidence.visibleContentPresent,
          stylesheetCount: evidence.stylesheetCount,
          loadedImages: evidence.loadedImages,
          brokenImages: evidence.brokenImages,
          violations: evidence.violations,
          consoleSignals: evidence.consoleSignals,
          resources: evidence.resources,
        };
        await testInfo.attach(`${appName}-${routePath.replaceAll("/", "_") || "root"}-evidence`, {
          body: Buffer.from(JSON.stringify(proof, null, 2)),
          contentType: "application/json",
        });
        if (routeIndex === 0) {
          const screenshotPath = testInfo.outputPath(`${appName}-${testInfo.project.name}.png`);
          await page.screenshot({ path: screenshotPath, fullPage: true });
          await testInfo.attach(`${appName}-${testInfo.project.name}-render`, { path: screenshotPath, contentType: "image/png" });
        }
      } finally {
        await page.close();
      }
    }
  }

  expect(issues, "public route CSP policy, nonce, and rendering failures").toEqual([]);
});
