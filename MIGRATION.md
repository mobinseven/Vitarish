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

## Migration Notes

- The legacy `Vorn.Aaas.Interface` project uses `Vorn.Caas.Client` to, e.g., send OTP codes. In the new design the `Web.Users` must inject some `Communications` service from `Communications.Contracts` to perform such tasks.

## Migration Status

This document will be updated as migration work proceeds. Check individual sub-issues for detailed migration status of each legacy project.

### Completed Migrations

#### Vorn.Caas.Messages → Communications/Communications.Core ✅

**Status**: Domain models, interfaces, and CQRS commands/queries migrated

**What was migrated**:
- `Models/Message.cs` → `Communications.Core/Messages/Domain/Models/Message.cs`
- `Models/SmsSendResult.cs` → `Communications.Core/Messages/Domain/Models/SmsSendResult.cs`
- `Models/SmsDeliveryState.cs` → `Communications.Core/Messages/Domain/Models/SmsDeliveryState.cs`
- `Abstractions/IMessagesService.cs` → `Communications.Core/Messages/Domain/Interfaces/IMessagesService.cs`
- `Abstractions/ISmsProvider.cs` → `Communications.Core/Messages/Domain/Interfaces/ISmsProvider.cs`
- `Commands/SendSms.cs` → `Communications.Core/Messages/Application/Commands/SendSms.cs` (CQRS pattern with validators)
- `Commands/SendSmsCode.cs` → `Communications.Core/Messages/Application/Commands/SendSmsCode.cs` (CQRS pattern with validators)
- `Commands/SendEmail.cs` → `Communications.Core/Messages/Application/Commands/SendEmail.cs` (CQRS pattern with validators)
- `Queries/GetSmsDeliveryState.cs` → `Communications.Core/Messages/Application/Queries/GetSmsDeliveryState.cs` (CQRS pattern with validators)

**Key changes during migration**:
- Updated namespaces from `Vorn.Communications.*` to `Vitarish.Communications.Core.Messages.*`
- Commands/Queries now use MediatR `IRequest<T>` pattern with FluentValidation validators
- Commands use `Result` pattern from `Base.Domain.Results`
- Static class pattern replaced with MediatR request/handler pattern
- Tests migrated to `Communications.Core.Tests/Messages/*`

**Test files in `Communications.Core.Tests/Messages/`**:
- `MessageCommandValidatorTests.cs` - Tests for all command validators (SendSms, SendSmsCode, SendEmail)
- `MessageQueryValidatorTests.cs` - Tests for query validators (GetSmsDeliveryState)

**Not migrated** (Infrastructure layer - future work):
- `Services/MessagesService.cs` → Should go to `Communications.Infrastructure`
- `Wrappers/SmsDotIr/*` → Should go to `Communications.Infrastructure`
- `Options/VornOptions.cs` → Should be refactored and go to `Communications.Infrastructure`

#### Vorn.Caas.Messages.Tests → Communications/Communications.Core.Tests ✅

**Status**: Validator tests migrated and updated to match new CQRS architecture

**What was migrated**:
- `Application/GetSmsDeliveryStateTests.cs` → Validator tests now in `Communications.Core.Tests/Messages/MessageQueryValidatorTests.cs`
- `Application/SendSmsTests.cs` → Validator tests now in `Communications.Core.Tests/Messages/MessageCommandValidatorTests.cs`

**Key changes during migration**:
- Old tests verified static `ExecuteAsync` methods with fake services
- New architecture uses MediatR handlers (not yet implemented in Infrastructure)
- Validation logic tests migrated to validator tests (following Core.Tests pattern)
- Service interaction tests will be added to `Communications.Infrastructure.Tests` when handlers are implemented

**Test coverage in Communications.Core.Tests**:
- ✅ Command validation (SendSms, SendSmsCode, SendEmail validators)
- ✅ Query validation (GetSmsDeliveryState validator)
- ⏳ Handler tests (pending handler implementation in Communications.Infrastructure)
- ⏳ Integration tests (pending handler implementation in Communications.Infrastructure)

**Old test scenarios and their migration status**:

| Old Test | Scenario | New Location |
| -------- | -------- | ------------ |
| `GetSmsDeliveryStateTests.ExecuteAsync_ThrowsWhenIdInvalid` | Validation of message ID | `GetSmsDeliveryStateValidatorTests` |
| `GetSmsDeliveryStateTests.ExecuteAsync_ReturnsStateFromService` | Service interaction | Infrastructure.Tests (pending) |
| `GetSmsDeliveryStateTests.ExecuteAsync_PassesCancellationToken` | Cancellation token handling | Infrastructure.Tests (pending) |
| `SendSmsTests.ExecuteAsync_ReturnsResultFromService` | Service interaction | Infrastructure.Tests (pending) |
| `SendSmsTests.ExecuteAsync_PassesCancellationToken` | Cancellation token handling | Infrastructure.Tests (pending) |
| `SendSmsTests.ExecuteAsync_CancelledTokenThrows` | Cancellation handling | Infrastructure.Tests (pending) |

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

#### Vorn.Aaas.Users Tests → Identity/Identity.Core.Tests ✅

**Status**: All Users domain tests migrated and enhanced

**Test files in `Identity.Core.Tests/Users/`**:
- `UserCommandValidatorTests.cs` - Tests for all command validators (CreateUserWithEmail, CreateUserWithPhone, UpdateUser, DeleteUser, SetUserAccessClaim, RemoveUserAccessClaim, UpdateUserProfile, SetUserEmail, SetUserPhoneNumber)
- `UserQueryValidatorTests.cs` - Tests for all query validators (GetUser, GetUsers, GetUserByName, HasUserAccess, GetUserProfile, GetClientUser, GetClientUsers)
- `UserEntityTests.cs` - Tests for User entity domain behavior (constructor, UpdateName, UpdateEmail, UpdatePhoneNumber, UpdateProfilePreferences, ProfilePreferences caching)

**Test coverage**:
- All command validators have validation tests for valid and invalid inputs
- All query validators have validation tests for valid and invalid inputs
- User entity has tests for construction, update methods, and profile preferences behavior

#### Vorn.Aaas.Users → Identity/Identity.Infrastructure ✅

**Status**: Infrastructure layer (DbContext, handlers, services, caching) migrated

**What was migrated**:
- `Infrastructure/Data/UsersDbContext.cs` → `Identity.Infrastructure/Users/Data/UsersDbContext.cs`
- `Infrastructure/Extensions/UserCacheExtensions.cs` → `Identity.Infrastructure/Users/Extensions/UserCacheExtensions.cs`
- `Infrastructure/Service/UsersService.cs` → `Identity.Infrastructure/Users/Services/UsersService.cs`
- `Infrastructure/Service/UserAccessService.cs` → `Identity.Infrastructure/Users/Services/UserAccessService.cs`
- `Infrastructure/Service/UserAccessAuditLogger.cs` → `Identity.Infrastructure/Users/Services/UserAccessAuditLogger.cs`
- `Infrastructure/Extensions/UsersWebApplicationBuilderExtensions.cs` → `Identity.Infrastructure/Users/DependencyInjection/UsersWebApplicationBuilderExtensions.cs`
- `Presentation/Contracts/ClaimDto.cs` → `Identity.Contracts/Users/ClaimDto.cs`
- `Presentation/Contracts/UserDto.cs` → `Identity.Contracts/Users/UserDto.cs`

**New handlers created in Infrastructure**:
- `GetUserHandler.cs` - Handles GetUser query with caching
- `GetUserByNameHandler.cs` - Handles GetUserByName query with caching
- `GetUsersHandler.cs` - Handles GetUsers query with caching
- `GetUserProfileHandler.cs` - Handles GetUserProfile query
- `CreateUserHandler.cs` - Handles CreateUserWithEmail and CreateUserWithPhone commands with persistence
- `UpdateUserHandler.cs` - Handles UpdateUser command with persistence
- `DeleteUserHandler.cs` - Handles DeleteUser command with persistence
- `SetUserEmailHandler.cs` - Handles SetUserEmail command with persistence
- `SetUserPhoneNumberHandler.cs` - Handles SetUserPhoneNumber command with persistence
- `UpdateUserProfileHandler.cs` - Handles UpdateUserProfile command with persistence
- Stub handlers for access claims (GetClientUsers, GetClientUser, HasUserAccess, SetUserAccessClaim, RemoveUserAccessClaim)

**Key changes during migration**:
- Updated namespaces from `Vorn.Aaas.Users.*` to `Vitarish.Identity.Infrastructure.Users.*`
- DbContext now uses standalone `User` entity (not ASP.NET Identity's IdentityDbContext)
- Handlers follow the same pattern as `AuthenticationClients` handlers
- Cache extensions use distributed cache with 5-minute expiration
- Service interfaces moved to Infrastructure layer (not Core, following existing patterns)
- DI registration via `AddIdentityUsers()` extension method

**Not migrated** (Presentation layer / API - future work):
- API endpoint mappings (PersonalAccessTokenApiExtensions, UserAccessApiMappingExtension)
- PersonalAccessTokenGenerator (requires Duende IdentityServer integration)
- ASP.NET Identity integration (legacy used IdentityDbContext with UserClaims)
- Full implementation of UserClaims-based access control (stub handlers provided)

#### Vorn.Aaas.Parties.Tests → Identity/Identity.Core.Tests ✅

**Status**: Validator tests migrated for Parties domain

**What was migrated**:
- Validator tests for Tenant commands → `Identity.Core.Tests/Parties/TenantCommandValidatorTests.cs`
- Validator tests for TenantIndicator commands → `Identity.Core.Tests/Parties/TenantIndicatorCommandValidatorTests.cs`
- Validator tests for Owner commands → `Identity.Core.Tests/Parties/OwnerCommandValidatorTests.cs`
- Validator tests for Tenant/TenantIndicator queries → `Identity.Core.Tests/Parties/TenantQueryValidatorTests.cs`
- Validator tests for Owner queries → `Identity.Core.Tests/Parties/OwnerQueryValidatorTests.cs`

**Test coverage includes**:
- CreateTenantValidator, UpdateTenantValidator, DeleteTenantValidator
- AddTenantIndicatorValidator, ActivateTenantIndicatorValidator, DisableTenantIndicatorValidator, VerifyTenantIndicatorValidator, DeleteTenantIndicatorValidator
- CreateOwnerValidator, UpdateOwnerValidator, DeleteOwnerValidator
- GetTenantValidator, GetTenantIndicatorsValidator, GetTenantsByClientIdValidator, GetTenantsByOwnerValidator
- GetOwnerValidator

**Key changes during migration**:
- Updated test namespaces from `Vorn.Aaas.Parties.Tests` to `Identity.Core.Tests.Parties`
- Tests now use FluentAssertions (consistent with existing Identity.Core.Tests patterns)
- Tests reference validators from `Vitarish.Identity.Core.Parties.Application.*` namespaces

**Not migrated to Identity.Core.Tests** (Infrastructure tests - belong in Identity.Infrastructure.Tests when handlers are implemented):
- DatabaseConstraintsTests (requires PartiesDbContext)
- DnsVerificationTests (requires handlers and DNS verification service)
- GetTenantsByClientIdTests (requires handler implementation)
- IndicatorStatusTransitionTests (requires handlers)
- OnDemandAllowEndpointTests (requires API/endpoint tests)
- ResyncAuthenticationClientsTests (requires handler implementation)
- TenantIndicatorCrudTests (requires handlers)
- TenantIndicatorProjectionTests (requires event handlers)
- TenantResolverTests (requires TenantResolver service)
- TenantsServiceTests (requires TenantsService implementation)
- AuthClientIdGeneratorTests (requires AuthClientIdGenerator utility in Infrastructure)
- HostnameNormalizerTests (requires HostnameNormalizer utility in Infrastructure)