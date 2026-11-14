import type {NextConfig} from 'next';
import {apiProxyRewrites} from '../../scripts/api-proxy-rewrites.mjs';
import {syncBrandFonts} from '../../scripts/sync-brand-fonts.mjs';
import {twoHostE2eNextDistDirectoryEnvironmentKey} from '../../scripts/two-host-e2e-artifacts.mjs';
// RTL text uses the licensed Ahang font served from this host.
syncBrandFonts(process.cwd());
const nextConfig: NextConfig = {reactStrictMode: true, output: 'standalone', distDir: process.env[twoHostE2eNextDistDirectoryEnvironmentKey] ?? '.next', rewrites: apiProxyRewrites};
export default nextConfig;
