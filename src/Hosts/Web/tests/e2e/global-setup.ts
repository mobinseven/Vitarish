import type { FullConfig } from "@playwright/test";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { Routes as PlatformRoutes } from "../../apps/platform-admin/src/app/routes";
import { twoHostE2ePlatformAdminOrigin, twoHostE2eSignInOrigin } from "../../scripts/two-host-e2e-artifacts.mjs";

/**
 * Pages the first tests open. `next dev` compiles each page on its first request,
 * which can take tens of seconds; requesting them here keeps that compile time
 * out of the first test's timeout.
 */
const compileTimeoutMs = 180_000;

export default async function compileFirstPages(config: FullConfig) {
  const baseURL = config.projects[0]?.use.baseURL;
  if (!baseURL) throw new Error("The two-host config has no baseURL to compile pages against.");

  // Clinic login now redirects before its form renders. Compile the actual entry pages
  // on the sign-in and administrator hosts, as well as the clinic dashboard.
  const pagesToCompile = [
    new URL(Routes.Login.Password, twoHostE2eSignInOrigin),
    new URL(PlatformRoutes.LoginPassword, twoHostE2ePlatformAdminOrigin),
    new URL(Routes.Home, baseURL),
  ];
  for (const url of pagesToCompile) {
    // Node does not resolve *.localhost; preserve the requested host while connecting to loopback.
    const connectUrl = new URL(url);
    connectUrl.hostname = "localhost";
    const response = await fetch(connectUrl, {
      headers: { Host: url.host },
      redirect: "manual",
      signal: AbortSignal.timeout(compileTimeoutMs),
    });
    if (response.status >= 500) {
      throw new Error(`Compiling ${url.pathname} failed with HTTP ${response.status}.`);
    }
  }
}
