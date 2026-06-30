# OpenAPI → React Client Generation Runbook

## Overview

The Web host's API client is generated from the .NET backend's OpenAPI specifications using [Orval](https://orval.dev/). The flow:

```
.NET Module Endpoints (C#)
  → AddModuleOpenApiDocument() (registers filtered OpenAPI spec)
  → MapOpenApi() (serves spec as JSON in Development)
  → Orval (consumes spec, generates TypeScript)
  → Generated React Query hooks + DTO types + MSW mocks
  → Feature hooks wrap generated hooks (optional)
  → UI components consume feature hooks
```

## Architecture

### Backend: OpenAPI Document Registration

Each module registers a named OpenAPI document filtered by group name:

```csharp
// src/Hosts/Main/Program.cs
builder.Services.AddModuleOpenApiDocument("authentication", "Authentication");
builder.Services.AddModuleOpenApiDocument("users", "Users");
```

The filter uses `ApiDescription.GroupName` or endpoint metadata tags to include only that module's endpoints:

```csharp
// src/Hosts/Main/DependencyInjection/HostOpenApiDocumentExtensions.cs
options.ShouldInclude = description => IsModuleOpenApiDescription(description, moduleGroupName);
```

Specs are served at `/openapi/{documentName}.json` (Development only):

```csharp
app.MapOpenApi("/openapi/{documentName}.json");
```

### Frontend: Orval Configuration

Orval config at `src/Hosts/Web/src/api/client/orval.config.ts`:

```typescript
const specsDir = "../specs";

export default defineConfig({
  authentication: {
    input: {
      target: `${specsDir}/authentication.json`,
    },
    output: {
      target: "./src/api/generated/authentication/authentication.ts",
      schemas: "./src/api/generated/authentication/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  // ... other modules
});
```

- **input.target**: Path to local OpenAPI JSON spec (relative to config)
- **output.target**: Generated hooks file
- **output.schemas**: Generated TypeScript types directory
- **client**: `react-query` generates React Query hooks
- **override.mutator**: Custom fetch function (`apiFetch`) instead of default `fetch`

### Generated Output

```
src/Hosts/Web/src/api/generated/
  authentication/
    authentication.ts          ← React Query hooks (useMutation, useQuery)
    authentication.msw.ts      ← MSW mock handlers for testing
    model/
      index.ts                 ← Re-exports all DTO types
      passwordLoginRequest.ts  ← Request/response types
      authenticationChallengeDto.ts
      ...
  users/
    users.ts
    model/
      ...
```

### The apiFetch Boundary

All generated hooks use `apiFetch` instead of raw `fetch`:

- **Antiforgery**: Auto-fetches and attaches CSRF tokens for non-GET requests
- **Error handling**: Throws `ApiError` with `ProblemDetails` on non-OK responses
- **Base URL**: Uses `NEXT_PUBLIC_API_BASE_URL` env var
- **Credentials**: Includes cookies (`credentials: "include"`)

## Regenerating the Client

### Prerequisites

- OpenAPI spec JSON files in `src/Hosts/Web/src/api/specs/`
- Node.js 22+, npm 10+

### Steps

```bash
cd src/Hosts/Web
npm run generate:api
```

Orval reads spec files from `src/api/specs/` and overwrites generated output in `src/api/generated/`.

### Verifying the Generation

```bash
# Type check passes
npm run typecheck

# Tests pass with MSW mocks
npm run test
```

## Adding a New Module

### 1. Register the OpenAPI Document

In `src/Hosts/Main/Program.cs`:

```csharp
builder.Services.AddModuleOpenApiDocument("modulename", "ModuleName");
```

The second argument must match the `GroupName` or tag on your module's endpoints.

### 2. Add Orval Config Entry

In `src/Hosts/Web/src/api/client/orval.config.ts`:

```typescript
modulename: {
  input: {
    target: `${specsDir}/modulename.json`,
  },
  output: {
    target: "./src/api/generated/modulename/modulename.ts",
    schemas: "./src/api/generated/modulename/model",
    client: "react-query",
    override: {
      mutator: {
        path: "./src/api/client/apiFetch.ts",
        name: "apiFetch",
      },
    },
  },
},
```

### 3. Regenerate

```bash
npm run generate:api
```

### 4. Create MSW Mocks

Orval generates `modulename.msw.ts`. Add handlers to `src/__tests__/server.ts` if tests need the new module.

## Adding an Endpoint to an Existing Module

1. Add the endpoint in the .NET module (ensure it has the correct `GroupName`/tag)
2. Update the spec file in `src/Hosts/Web/src/api/specs/`
3. Run `npm run generate:api`
4. New hook and types appear in the generated output
5. Use the new hook in your component

## Endpoint Naming Convention

Orval derives operation IDs from the OpenAPI spec. The `ModuleEndpointMappingExtensions` helpers (`MapModulePost`, `MapModuleGet`, etc.) apply `.WithName(operationId)` automatically:

```csharp
endpoints.MapModulePost("login/password", handler, "LoginWithPassword");
// Generates: usePasswordLoginMutation()
```

The generated hook name follows the pattern: `use{OperationId}Mutation` or `use{OperationId}Query`.

## Troubleshooting

| Issue | Cause | Fix |
|---|---|---|
| Empty generated file | Spec file missing or has no paths | Ensure `src/api/specs/{module}.json` exists with paths |
| Missing types after generation | Schema name changed in backend | Update spec file, regenerate |
| `ApiError: 401` on mutations | Antiforgery token expired or missing | `apiFetch` auto-fetches tokens; ensure `/api/modules/authentication/antiforgery` is reachable |
| `ApiError: 403` on mutations | CSRF validation failed | Check cookie is sent (`credentials: "include"`) and antiforgery header matches |
| Generated hooks don't match expected API | Orval config path is wrong | Verify `input.target` in `orval.config.ts` matches spec file location |
| MSW mocks return wrong shapes | Generated DTO types changed | Regenerate mocks: `npm run generate:api` |

## File Reference

| File | Purpose |
|---|---|
| `src/Hosts/Main/Program.cs:93-109` | OpenAPI document registration |
| `src/Hosts/Main/DependencyInjection/HostOpenApiDocumentExtensions.cs` | Module document filter |
| `src/Hosts/Web/src/api/client/orval.config.ts` | Orval generation config |
| `src/Hosts/Web/src/api/client/apiFetch.ts` | Custom fetch boundary |
| `src/Hosts/Web/src/api/generated/` | Generated hooks, types, mocks |
| `src/Hosts/Web/.env.example` | `NEXT_PUBLIC_API_BASE_URL` |

## Web host API boundary and antiforgery troubleshooting

The React Web host must call module APIs through the central `apiFetch` mutator. Generated Orval clients are configured to use `src/api/client/apiFetch.ts`, and feature/page code must not call `/api/modules/**` with direct `fetch`. Run `npm run validate:api-boundary` from `src/Hosts/Web` before changing module API adapters.

For unsafe browser-cookie requests, `apiFetch` first bootstraps an antiforgery token from the same API origin used for the module request. Troubleshoot failures in this order:

1. Verify `GET /api/modules/authentication/antiforgery` returns `200` from the browser.
2. Verify the response sets the antiforgery cookie and returns a non-empty `requestToken`.
3. Verify the following unsafe request sends both the antiforgery cookie and the `X-XSRF-TOKEN` header unless the endpoint response specifies a different `headerName`.
4. Verify `NEXT_PUBLIC_API_BASE_URL` matches the intended API origin. Same-origin deployments may leave it empty; cross-origin development must point at the HTTPS API origin that issues the cookie.
5. Verify the API `WebHostCors` allowed origins match the Web host origin exactly and credentials are enabled. Wildcard origins are intentionally rejected when credentials are enabled.
6. For local HTTPS and Docker, verify the browser accepts the antiforgery cookie settings: secure cookies require HTTPS, `SameSite` must allow the selected same-origin or cross-origin development topology, and cookie domain/path must scope the cookie to the API requests.
