# Vitarish Web UI — Runbook

## Prerequisites

- Node.js 22+
- npm 10+
- The .NET Main host running on `localhost:5000` (the authentication API backend)

## Quick Start (Local Development)

```bash
# 1. Start the .NET backend (in a separate terminal)
cd src/Hosts/Main
dotnet run --urls=http://localhost:5000

# 2. Start the Next.js dev server
cd src/Hosts/Web
npm install
cp .env.example .env.local
npm run dev
```

Open http://localhost:3000. You should see the auth method selector.

## Available Scripts

| Command | Description |
|---|---|
| `npm run dev` | Start dev server on port 3000 |
| `npm run build` | Production build |
| `npm run start` | Start production server on port 3000 |
| `npm run lint` | Run ESLint |
| `npm run typecheck` | TypeScript type check |
| `npm run test` | Run Vitest tests |
| `npm run test:watch` | Run tests in watch mode |
| `npm run generate:api` | Regenerate API client from OpenAPI spec (requires .NET host running) |

## Environment Variables

| Variable | Default | Description |
|---|---|---|
| `NEXT_PUBLIC_API_BASE_URL` | empty | Leave empty: the browser calls `/api` on its own origin |
| `VITARISH_API_PROXY_TARGET` | `https://localhost:7272` | Local development only: Main origin that Next proxies `/api` to; requires `NODE_OPTIONS=--use-system-ca` |

Copy `.env.example` to `.env.local` and adjust as needed.

## Tenant Host Forwarding

The React web host and the .NET Main host are different HTTP origins in the split-host architecture. Browser requests sent to the Main host therefore arrive with `HttpContext.Request.Host` set to the Main host, not the tenant web host.

The shared `apiFetch` boundary forwards the current web origin host through `X-Vitarish-Tenant-Host`. The Main host tenant resolver prefers that header when it matches the browser request source, then falls back to `Origin`, `Referer`, and finally `Request.Host`. This preserves tenant resolution for requests opened from tenant domains such as `tenantA.mydomain.com` while retaining the old mono-host behavior as a fallback.

## Server deployment

The maintained Docker Compose topology uses release images, separate test and
production environments, and shared HTTPS ingress. Follow
[`docs/hosts/server-deployment-bundles.md`](../../server-deployment-bundles.md)
for image publishing, operator environment files, startup, validation, and
rollback. For local Web development, use the npm workspace quick start above
and run Main separately.

## API Client Regeneration

When the .NET backend's OpenAPI spec changes:

```bash
# Ensure the .NET host is running on localhost:5000
npm run generate:api
```

This uses Orval to regenerate TypeScript types and React Query hooks from the Authentication module's OpenAPI document at `http://localhost:5000/openapi/authentication.json`.

## Testing

```bash
npm run test
```

Tests use MSW to mock the backend API. No running server needed.

## Troubleshooting

| Issue | Solution |
|---|---|
| `fetch failed` in hooks | Ensure the .NET host is running on `VITARISH_API_PROXY_TARGET` and Node trusts its certificate (`--use-system-ca`) |
| Tenant is not resolved from the Web UI | Ensure requests go through the shared `apiFetch` boundary so `X-Vitarish-Tenant-Host` is forwarded |
| `navigator.credentials` error | Passkey auth requires HTTPS (except localhost) |
| CSRF errors on mutations | The `apiFetch` boundary auto-fetches antiforgery tokens; ensure the backend antiforgery endpoint is reachable |
| Tests fail with "React is not defined" | Run `npm install` to ensure dependencies are installed |
| Build crashes with OOM | Increase Node memory: `NODE_OPTIONS="--max-old-space-size=4096" npm run build` |
