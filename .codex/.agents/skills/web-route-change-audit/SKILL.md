---
name: web-route-change-audit
description: Synchronize Vitarish Next.js route topology with route constants, navigation registries, intent contracts, localized copy, tests, and derived outputs. Use when changing Web routes or feature entry points.
---

# Web Route Change Audit

## Procedure

1. Treat each host's `src/app/**/page.tsx` tree as the route source of truth. Identify added, removed, renamed, dynamic, and redirected paths before editing derived data.
2. Update only applicable surfaces:
   - `<app>/src/app/routes.ts` route constants;
   - owner shell navigation, command registry, breadcrumb patterns, home/landing feature entry points, and account links;
   - shared `clinic-web` or `sea-web` navigation intent unions and the host mapper that resolves them;
   - post-login or journey defaults/gates when their destination changes;
   - localized message keys and route/navigation tests.
3. Search for the old path, route key, and feature name across `src/Hosts/Web`, excluding generated output. Remove stale entries; do not add navigation to intentionally unavailable functionality.
4. Never manually edit `src/api/generated` or `src/api/specs`. Change their canonical source and run the owning generator when a transport contract changes.
5. Validate the touched workspace typecheck/tests and confirm every registered static path has a matching route or intentional redirect. Report generated-output and external journey-destination effects explicitly.
