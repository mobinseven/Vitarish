/**
 * Same-origin API topology for local development and the two-host E2E suite.
 *
 * Production serves `/api/*` on each web app's own HTTPS host through the ingress
 * (`src/Hosts/deployment/Caddyfile`), so SameSite=Lax cookies are first-party. Locally
 * the web apps run on http://localhost and Main on https://localhost:7272, which is
 * cross-site under schemeful same-site. When this server-only setting is present, the
 * Next app proxies `/api/*` (including WebSocket upgrades) to Main so the browser keeps
 * the production same-origin shape. Deployment builds must not set it: rewrites are
 * resolved at build time and the ingress owns `/api` there.
 */
export const apiProxyTargetEnvironmentKey = "VITARISH_API_PROXY_TARGET";
export const apiProxyPathPrefix = "/api";
export const browserApiBaseEnvironmentKey = "NEXT_PUBLIC_API_BASE_URL";

export async function apiProxyRewrites() {
  const target = process.env[apiProxyTargetEnvironmentKey]?.trim();
  if (!target) {
    return [];
  }

  // A leftover browser base URL (for example from an older .env.local) would send the
  // browser straight to Main cross-site, where SameSite=Lax cookies are dropped.
  if (process.env[browserApiBaseEnvironmentKey]?.trim()) {
    throw new Error(
      `${apiProxyTargetEnvironmentKey} is set, so ${browserApiBaseEnvironmentKey} must be empty: ` +
        "the browser calls /api on its own origin. Remove it from .env.local or the environment.",
    );
  }

  const origin = new URL(target).origin;
  return [{ source: `${apiProxyPathPrefix}/:path*`, destination: `${origin}${apiProxyPathPrefix}/:path*` }];
}
