import type { NextConfig } from "next";
import { apiProxyRewrites } from "../../scripts/api-proxy-rewrites.mjs";

const nextConfig: NextConfig = { output: "standalone", reactStrictMode: true, rewrites: apiProxyRewrites };

export default nextConfig;
