import type {NextConfig} from 'next';
import {apiProxyRewrites} from '../../scripts/api-proxy-rewrites.mjs';
import {syncBrandFonts} from '../../scripts/sync-brand-fonts.mjs';
// RTL text uses the licensed Ahang font served from this host.
syncBrandFonts(process.cwd());
const nextConfig: NextConfig = {reactStrictMode: true, output: 'standalone', rewrites: apiProxyRewrites};
export default nextConfig;
