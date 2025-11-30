# Migration Guide: Legacy Vorn.* to Vitarish

This document provides the canonical mapping of legacy `Vorn.*` projects to the new Vitarish modular structure. It serves as the authoritative reference for all migration sub-issues.

## Migration Mapping Table

| Legacy Project                  | Purpose (High-Level)                               | New Project(s) / Area                                                                |
| ------------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------------------ |
| Vorn.Aaas.ApiClients            | General API client helpers for AAA service         | `Identity/Identity.Core`                                                             |
| Vorn.Aaas.AuthenticationClients | Auth-specific API clients                          | `Identity/Identity.Core`                                                             |
| Vorn.Aaas.Client                | Combined AAA client library                        | `Clients/Clients.Interface`                                                          |
| Vorn.Aaas.Client.Api            | AAA HTTP API client entrypoint                     | `Clients/Clients.Api`                                                                |
| Vorn.Aaas.Host                  | Main AAA host (API + UI)                           | Split into `Web/Web.Api` + `Web.*` apps (Admins/Users; Owners is new)                |
| Vorn.Aaas.Interface             | AAA server UI                                      | Split into `Web/Web.Admins`, `Web/Web.Users`, `Web/Web.Base`; Owners UI is new       |
| Vorn.Aaas.Interface.Tests       | Tests for AAA UI                                   | `Web/Web.Admins` / `Web/Web.Users` tests (new projects)                              |
| Vorn.Aaas.Parties               | Tenants / owners / parties domain                  | `Identity/Identity.Core` (+ maybe shared types in `Base.Domain` where truly generic) |
| Vorn.Aaas.Parties.Tests         | Tests for parties                                  | `Identity/Identity.Core.Tests`                                                       |
| Vorn.Aaas.RequestIdentities     | Request identity abstraction                       | **Deleted**; see [Migration Notes](#migration-notes) below                           |
| Vorn.Aaas.Users                 | User domain / services                             | `Identity/Identity.Core` (Users aggregate + services), tests → `Identity.Core.Tests` |
| Vorn.Ai.Client                  | AI client library                                  | `Ai/Ai.Client` (+ optionally aggregated into `Clients/Clients.Api`)                  |
| Vorn.Ai.Server                  | AI server / API                                    | `Ai/Ai.Api`, `Ai/Ai.Core`, `Ai/Ai.Infrastructure`, tests → `Ai.*.Tests`              |
| Vorn.Caas.Client                | Comms (CaaS) client                                | `Communications/Communications.Client`                                               |
| Vorn.Caas.Host                  | Comms host (API)                                   | `Communications/Communications.Api`                                                  |
| Vorn.Caas.Messages              | Messaging contracts / domain                       | `Communications/Communications.Core`,`Communications/Communications.Infrastructure`  |
| Vorn.Caas.Messages.Tests        | Tests for messages                                 | `Communications/Communications.Core.Tests`                                           |
| Vorn.Files.Client               | Files client                                       | `Files/Files.Client`                                                                 |
| Vorn.Files.Core                 | Files domain                                       | `Files/Files.Core`                                                                   |
| Vorn.Files.Host                 | Files host (API)                                   | `Files/Files.Api` + infra in `Files/Files.Infrastructure`                            |
| Vorn.Files.Host.Tests           | Tests for files host                               | `Files/Files.Api.Tests`                                                              |
| Vorn.Interface                  | Ready-to-use UI components for client integrations | Mostly → `Clients/Clients.Interface` (client-side UI); some into `Web.Base`          |

## Rules of Thumb

When deciding where to place migrated code, follow these guidelines:

| Pattern                                | Destination                                 |
| -------------------------------------- | ------------------------------------------- |
| Server/host functionality              | Module `*.Api` or `Web/Web.Api`             |
| Domain/business logic                  | Module `*.Core`                             |
| Cross-cutting infrastructure           | `Base.*` or `*.Infrastructure`              |
| Client-facing SDK or UI components     | `Clients.*`, `*.Client`, or `Web.*`         |
| Contracts/DTOs shared between services | `*.Contracts`                               |
| Tests                                  | Corresponding `*.Tests` project             |

## New Modular Structure

The new Vitarish structure is organized into the following modules:

```
Vitarish/
├── Ai/                          # AI functionality
│   ├── Ai.Api                   # AI server/API
│   ├── Ai.Api.Tests
│   ├── Ai.Client                # AI client library
│   ├── Ai.Core                  # AI domain logic
│   ├── Ai.Core.Tests
│   └── Ai.Infrastructure        # AI infrastructure
├── Base/                        # Cross-cutting concerns
│   ├── Base.Domain              # Shared domain types
│   ├── Base.Infrastructure      # Shared infrastructure
│   └── Base.Tests
├── Clients/                     # Client libraries
│   ├── Clients.Api              # API client helpers
│   └── Clients.Interface        # Client-side UI components
├── Communications/              # Communications (CaaS)
│   ├── Communications.Api
│   ├── Communications.Api.Tests
│   ├── Communications.Client
│   ├── Communications.Contracts
│   ├── Communications.Core
│   ├── Communications.Core.Tests
│   └── Communications.Infrastructure
├── Files/                       # File management
│   ├── Files.Api
│   ├── Files.Api.Tests
│   ├── Files.Client
│   ├── Files.Client.Tests
│   ├── Files.Contracts
│   ├── Files.Core
│   ├── Files.Core.Tests
│   └── Files.Infrastructure
├── Identity/                    # Identity/AAA functionality
│   ├── Identity.Api
│   ├── Identity.Api.Tests
│   ├── Identity.Client
│   ├── Identity.Contracts
│   ├── Identity.Core
│   ├── Identity.Core.Tests
│   └── Identity.Infrastructure
├── Web/                         # Web applications
│   ├── Web.Admins               # Admin UI
│   ├── Web.Api                  # Main API host
│   ├── Web.Base                 # Shared web components
│   ├── Web.Owners               # Owners UI (new)
│   └── Web.Users                # Users UI
└── _old/                        # Legacy projects (for reference)
```

## Migration Status

This document will be updated as migration work proceeds. Check individual sub-issues for detailed migration status of each legacy project.

## Migration Notes

### Vorn.Aaas.RequestIdentities (Deleted)

The `Vorn.Aaas.RequestIdentities` project has been removed and replaced with standard claim-based access patterns:

- **Previous approach**: Custom `RequestIdentity` abstraction for accessing user/tenant context
- **New approach**: Use `ClaimsPrincipal` and standard ASP.NET Core authentication/authorization
- **Migration path**:
  1. Replace `RequestIdentity` usage with `ClaimsPrincipal` from `HttpContext.User`
  2. Use claim types from `Identity.Contracts` for accessing user claims
  3. For API clients, use the claim-based authentication provided by `Identity.Client`

---

*Last updated: See git history for document changes. This is the canonical reference for Issue #1 and all related sub-issues.*
