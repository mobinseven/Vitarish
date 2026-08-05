# Dedicated platform-admin application and landing-host tenant binding

## Status

Accepted and implemented for the platform-admin foundation and Landing host-binding workflow.

## Context

Platform operations are global and must not be represented as a tenant-owner workspace. Users owns account and tenant-role membership, Parties owns owners, tenants and tenant indicators, Platform owns platform settings, and Landing owns anonymous landing behaviour. `TenantResolver` already resolves a trusted host through active `TenantIndicator` records and caches each result locally for ten minutes.

## Decision

- `@vitarish/platform-admin` is a dedicated Web host on port 3003. Its access boundary is the global `PlatformAdmin` policy; tenant roles do not grant access.
- The host composes frontend-safe contracts from their existing owners. It does not create a platform administration domain or duplicate module rules.
- The landing tenant is the tenant whose active normalized indicator matches `LandingPlatformOptions.LandingUrl.Host`. No `PlatformDb` landing-tenant setting is introduced.
- The Landing-owned, `PlatformAdmin`-protected reassignment command receives only a target tenant ID, derives and normalizes the host server-side, uses Parties to move the indicator, and is idempotent for the current target. Entity audit interception records the persisted indicator mutation; no message, cookie, token, or credential data is included.
- Generic indicator editing must not claim the configured landing hostname. The dedicated landing workflow is the only transfer path.

## Consequences

The dedicated application has an independent deployment/runtime boundary and a client-side platform-access gate used only for usability. Every administration endpoint remains independently protected by `PlatformAdmin`.

Existing landing data is unchanged. On a clean installation startup retains the reserved Landing app tenant bootstrap only when no active indicator exists for the configured host. A restart must never reclaim an operator-selected binding. Conversations remain scoped to the tenant chosen when they were created; reassignment neither moves them nor exposes them under the new tenant.

The current-process resolver entry for the normalized host must be evicted after a successful transfer. Without shared invalidation, other instances can resolve the old tenant until their local ten-minute cache expires. Rollback is a confirmed reassignment to the previous tenant and does not undo conversations created while the alternate tenant was active.

## Non-goals

This decision does not add global administrator membership management, custom roles or claims, credential workflows, impersonation, deletion/suspension/ownership transfer, landing CMS/content management, or immediate multi-instance propagation.
