# Migration Guide: Legacy Vorn.* to Vitarish

This document provides the canonical mapping of legacy `Vorn.*` projects to the new Vitarish modular structure. It serves as the authoritative reference for all migration sub-issues.

## Migration Mapping Table

| Legacy Project                  | Purpose (High-Level)                               | New Project(s) / Area                                                                |
| ------------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------------------ |
| Vorn.Aaas.ApiClients            | A module to manage ApiClient definitions           | **Deleted** because all api clients are getting merged into one host                 |
| Vorn.Aaas.AuthenticationClients | A module to manage Authentication clients defs.    | `Identity/Identity.*`                                                                |
| Vorn.Aaas.Client                | A client library used by Authentication clients    | `Web/Web.Clients`                                                                    |
| Vorn.Aaas.Client.Api            | A client library used by Api Clients               | **Deleted** because all api clients are getting merged into one host                 |
| Vorn.Aaas.Host                  | Main AAA host (API + UI)                           | Split into `Web/Web.Api` + `Web.*` apps (Admins/Users; Owners is new)                |
| Vorn.Aaas.Interface             | AAA server UI                                      | Split into `Web/Web.Admins`, `Web/Web.Users`, `Web/Web.Base`; Owners UI is new       |
| Vorn.Aaas.Interface.Tests       | Tests for AAA UI                                   | `Web/Web.Admins` / `Web/Web.Users` tests (new projects)                              |
| Vorn.Aaas.Parties               | Tenants / owners / parties domain                  | `Identity/Identity.Core` (+ maybe shared types in `Base.Domain` where truly generic) |
| Vorn.Aaas.Parties.Tests         | Tests for parties                                  | `Identity/Identity.Core.Tests`                                                       |
| Vorn.Aaas.RequestIdentities     | Request identity abstraction                       | **Deleted** as the concept of identifying requests will be based on claims           |
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
| Vorn.Interface                  | Ready-to-use UI components for client integrations | `Web/Web.Clients`                                                                    |

## Rules of Thumb

When deciding where to place migrated code, follow these guidelines:

| Pattern                                | Destination                                 |
| -------------------------------------- | ------------------------------------------- |
| Server/host functionality              | Module `*.Api` or `Web/Web.Api`             |
| Domain/business logic                  | Module `*.Core`                             |
| Cross-cutting infrastructure           | `Base.*` or `*.Infrastructure`              |
| Client-facing SDK or UI components     | `Web/Web.Clients*`                              |
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
│   ├── Web.Base                 # Shared web components for host interfaces
│   ├── Web.Base                 # Shared web components for client interfaces (client SDK)
│   ├── Web.Owners               # Owners UI (new)
│   └── Web.Users                # Users UI
└── _old/                        # Legacy projects (for reference)
```

## Migration Status

This document will be updated as migration work proceeds. Check individual sub-issues for detailed migration status of each legacy project.

### Completed Migrations

#### Vorn.Aaas.Users → Identity/Identity.Core ✅

**Status**: Domain models and CQRS commands/queries migrated

**What was migrated**:
- `Domain/Entities/User.cs` → `Identity.Core/Users/Domain/Entities/User.cs`
- `Domain/Events/*` → `Identity.Core/Users/Domain/Events/*`
- `Domain/Models/*` → `Identity.Core/Users/Domain/Models/*`
- `Application/Commands/*` → `Identity.Core/Users/Application/Commands/*` (CQRS pattern with validators)
- `Application/Queries/*` → `Identity.Core/Users/Application/Queries/*` (CQRS pattern with validators)

**Key changes during migration**:
- Updated namespaces from `Vorn.Aaas.Users.*` to `Vitarish.Identity.Core.Users.*`
- User entity now extends `Entity<string>` from `Base.Domain.Primitives`
- Commands/Queries use `Result` pattern from `Base.Domain.Results`
- Validators separated into individual files following existing patterns
- Tests migrated to `Identity.Core.Tests/Users/*`

**Not migrated** (Infrastructure/Presentation layer - separate issue):
- Infrastructure services (DbContext, caching, event publishing)
- Presentation layer (API endpoints, DI extensions)
- These belong in `Identity.Infrastructure` and `Identity.Api` respectively