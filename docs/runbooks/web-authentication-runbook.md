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
| `NEXT_PUBLIC_API_BASE_URL` | `http://localhost:5000` | Base URL of the .NET authentication API |

Copy `.env.example` to `.env.local` and adjust as needed.

## Docker

### Build and run the Next.js UI only

```bash
cd src/Hosts/Web
docker build -t vitarish-web --build-arg NEXT_PUBLIC_API_BASE_URL=http://localhost:5000 .
docker run -p 3000:3000 vitarish-web
```

### Run both services together

From the repo root:

```bash
docker compose -f docker-compose.web.yml up --build
```

This starts:
- **web** — Next.js UI on port 3000
- **main** — .NET host on port 5000

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

## Coexistence with Blazor

The Next.js UI (port 3000) and Blazor UI (port 5000) run independently. Sessions are not shared — each has its own authentication cookie. To use the Next.js UI as the primary auth interface, point your reverse proxy or load balancer to port 3000 for `/login`, `/logout`, and related auth routes.

## Troubleshooting

| Issue | Solution |
|---|---|
| `fetch failed` in hooks | Ensure the .NET host is running on `NEXT_PUBLIC_API_BASE_URL` |
| `navigator.credentials` error | Passkey auth requires HTTPS (except localhost) |
| CSRF errors on mutations | The `apiFetch` boundary auto-fetches antiforgery tokens; ensure the backend antiforgery endpoint is reachable |
| Tests fail with "React is not defined" | Run `npm install` to ensure dependencies are installed |
| Build crashes with OOM | Increase Node memory: `NODE_OPTIONS="--max-old-space-size=4096" npm run build` |
