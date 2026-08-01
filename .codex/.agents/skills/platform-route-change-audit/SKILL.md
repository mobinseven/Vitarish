---
name: platform-route-change-audit
description: Synchronize platform Web route topology with route constants, navigation registries, intent contracts, localized copy, tests, and derived outputs across hosts and shared packages.
---

# Platform Route Change Audit

## Procedure

1. Treat every host's `src/app/**/page.tsx` tree as its route source of truth. Identify added, removed, renamed, dynamic, and redirected paths before editing derived data.
2. Update only applicable platform surfaces:
   - host route constants;
   - shell navigation, command registries, breadcrumbs, home/landing entry points, and account links;
   - shared-package navigation intent contracts and each host mapper that resolves them;
   - post-login or journey defaults and gates when their destination changes;
   - localized message keys and route/navigation tests.
3. Search for the old path, route key, and feature name across `src/Hosts/Web`, excluding generated output. Remove stale entries; do not register intentionally unavailable functionality.
4. Keep transport generation separate: never manually edit `src/api/generated` or `src/api/specs`; change the owning contract and run its generator when the route change also alters transport.
5. Validate every touched host/package and confirm each registered static path has a matching route or intentional redirect. Report generated-output and external journey-destination effects.
