import type { FullConfig } from "@playwright/test";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";

/**
 * Pages the first tests open. `next dev` compiles each page on its first request,
 * which can take tens of seconds; requesting them here keeps that compile time
 * out of the first test's timeout.
 */
const pagesToCompile = [Routes.Login.Password, Routes.Home];
const compileTimeoutMs = 180_000;

export default async function compileFirstPages(config: FullConfig) {
  const baseURL = config.projects[0]?.use.baseURL;
  if (!baseURL) throw new Error("The two-host config has no baseURL to compile pages against.");

  for (const path of pagesToCompile) {
    const response = await fetch(new URL(path, baseURL), {
      redirect: "manual",
      signal: AbortSignal.timeout(compileTimeoutMs),
    });
    if (response.status >= 500) {
      throw new Error(`Compiling ${path} failed with HTTP ${response.status}.`);
    }
  }
}
