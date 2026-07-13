# Platform Admin

Status: In Progress  
Owner: Platform Architecture  
Issue: #668

## Purpose

Dedicated internal React application for cross-tenant platform administration. Replaces retired Blazor platform-admin workflows with secure React workflows and module-owned APIs.

## App location

- **Next.js app:** `src/Hosts/Web/apps/platform-admin`
- **Feature package:** `src/Hosts/Web/shared/platform-admin-web` (`@vitarish/platform-admin-web`)
- **Development port:** 3002

## Run

```bash
cd src/Hosts/Web
npm run dev:platform-admin
```

Backend must be running at the configured `NEXT_PUBLIC_API_BASE_URL` (default: `https://localhost:7272`).

## Route map

| Route | Page |
| --- | --- |
| `/login` | Authentication login |
| `/` | Dashboard (requires PlatformAdmin) |
| `/users` | User directory |
| `/users/[userId]` | User detail |
| `/owners` | Owner directory |
| `/owners/[ownerId]` | Owner detail |
| `/tenants` | Tenant directory |
| `/tenants/[tenantId]` | Tenant detail |
| `/operations/health` | Health and memory |
| `/operations/search` | Search index status |
| `/operations/communications` | Communications oversight |
| `/audit` | Platform audit log |

## Authorization

- Only authenticated users with `PlatformAdmin` role can access admin pages.
- Session endpoint exposes `platformAccess.canAccessPlatformAdmin` for frontend routing.
- All admin API endpoints independently enforce `PlatformAdmin` authorization server-side.
- Unauthenticated users are redirected to `/login`.
- Authenticated non-admin users see a forbidden page.

## Localization

- Default locale: Persian (`fa`, RTL)
- Secondary locale: English (`en`, LTR)
- Feature messages in `@vitarish/platform-admin-web/localization`
- App messages in `src/localization/messages/`

## Architecture

```
Authentication Contracts → PlatformAccessDto
Base.Core → AdminModuleEndpointRouteBuilderExtensions (reusable admin group helper)
@vitarish/platform-admin-web → feature package (API clients, hooks, components, localization)
@vitarish/platform-admin → Next.js app (routes, shell, providers)
```

## Slices

1. **Foundation** — App scaffold, session platform access, admin convention, login/forbidden/shell, localization
2. **Users** — User admin contracts/endpoints, directory/detail
3. **Owner/tenant lifecycle** — ArchivedAt, migration, lifecycle
4. **Tenant configuration/indicators** — Registry, metadata, indicators
5. **Operations** — Health, search, communications
6. **Dashboard/hardening/docs** — Dashboard, audit, RTL/accessibility polish
