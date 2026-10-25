import type { NextConfig } from "next";
import { resolve } from "node:path";
import { apiProxyRewrites } from "../../scripts/api-proxy-rewrites.mjs";
import { syncBrandFonts } from "../../scripts/sync-brand-fonts.mjs";
import { twoHostE2eNextDistDirectoryEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";

const webWorkspaceRoot = resolve(process.cwd(), "../..");
// RTL text uses the licensed Ahang font served from this host.
syncBrandFonts(process.cwd());

const nextConfig: NextConfig = {
  output: "standalone",
  reactStrictMode: true,
  distDir: process.env[twoHostE2eNextDistDirectoryEnvironmentKey] ?? ".next",
  outputFileTracingRoot: webWorkspaceRoot,
  turbopack: { root: webWorkspaceRoot },
  rewrites: apiProxyRewrites,
};

export default nextConfig;
