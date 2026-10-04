import type { NextConfig } from "next";
import { resolve } from "node:path";
import { twoHostE2eNextDistDirectoryEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";

const webWorkspaceRoot = resolve(process.cwd(), "../..");

const nextConfig: NextConfig = {
  reactStrictMode: true,
  output: "standalone",
  distDir: process.env[twoHostE2eNextDistDirectoryEnvironmentKey] ?? ".next",
  outputFileTracingRoot: webWorkspaceRoot,
  turbopack: { root: webWorkspaceRoot },
};

export default nextConfig;
