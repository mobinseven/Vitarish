import type { FullConfig } from "@playwright/test";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { Routes as PlatformRoutes } from "../../apps/platform-admin/src/app/routes";
import { twoHostE2ePlatformAdminOrigin, twoHostE2eSignInOrigin } from "../../scripts/two-host-e2e-artifacts.mjs";

/** Check the pages the first tests open before the suite starts. */
const pageProbeTimeoutMs = 180_000;

export default async function checkFirstPages(config: FullConfig) {
  const baseURL = config.projects[0]?.use.baseURL;
  if (!baseURL) throw new Error("The two-host config has no baseURL to probe pages against.");

  // Clinic login now redirects before its form renders. Check the actual entry pages
  // on the sign-in and administrator hosts, as well as the clinic dashboard.
  const pagesToProbe = [
    new URL(Routes.Login.Password, twoHostE2eSignInOrigin),
    new URL(PlatformRoutes.LoginPassword, twoHostE2ePlatformAdminOrigin),
    new URL(Routes.Home, baseURL),
  ];
  for (const url of pagesToProbe) {
    // Node does not resolve *.localhost; preserve the requested host while connecting to loopback.
    const connectUrl = new URL(url);
    connectUrl.hostname = "localhost";
    const response = await fetch(connectUrl, {
      headers: { Host: url.host },
      redirect: "manual",
      signal: AbortSignal.timeout(pageProbeTimeoutMs),
    });
    if (response.status >= 500) {
      throw new Error(`Probing ${url.pathname} failed with HTTP ${response.status}.`);
    }
  }
}
