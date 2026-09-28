import type { NextConfig } from "next";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { cspBrowserEnvironment } from "../../scripts/csp-browser-environment.cjs";

const outputFileTracingRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

const nextConfig: NextConfig = {
  distDir: process.env[cspBrowserEnvironment.nextDistDir] ?? ".next",
  outputFileTracingRoot,
  reactStrictMode: true,
  output: "standalone",
};

export default nextConfig;
