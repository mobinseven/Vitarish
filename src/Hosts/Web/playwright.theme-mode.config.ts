import { defineConfig } from "@playwright/test";
import layoutConfig from "./playwright.layout.config";

/** Layout suite settings, limited to the theme-mode proof and the servers it needs. */
const themeModeServers = new Set(["layout-fixture-service", "private-practice-owner"]);
const layoutServers = [layoutConfig.webServer ?? []].flat();

export default defineConfig({
  ...layoutConfig,
  testMatch: "theme-mode.spec.ts",
  webServer: layoutServers.filter((server) => themeModeServers.has(server.name ?? "")),
});
