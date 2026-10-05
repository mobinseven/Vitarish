import type {NextConfig} from 'next';
import {apiProxyRewrites} from '../../scripts/api-proxy-rewrites.mjs';
const nextConfig: NextConfig = {reactStrictMode: true, output: 'standalone', rewrites: apiProxyRewrites};
export default nextConfig;
