import { twoHostE2eNextDistDirectoryEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import type { NextConfig } from "next";
import { apiProxyRewrites } from "../../scripts/api-proxy-rewrites.mjs";

const nextConfig: NextConfig = { output: "standalone", distDir: process.env[twoHostE2eNextDistDirectoryEnvironmentKey] ?? ".next", reactStrictMode: true, rewrites: apiProxyRewrites };

export default nextConfig;
