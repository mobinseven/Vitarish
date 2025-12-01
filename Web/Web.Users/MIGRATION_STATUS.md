# Web.Users Migration Status

## Overview
This document tracks the migration of user-facing UI from `Vorn.Aaas.Interface` to `Web/Web.Users`.

## What Has Been Migrated

### ✅ Components Copied
- **Account Components** (30+ files)
  - Email login/verification forms
  - Phone login/verification forms
  - Password login/change forms
  - Profile editor
  - Status messages
  - All associated CSS and JavaScript files

- **Base Components** (User-specific)
  - `AccountLayout.razor` - Layout for account pages
  - `IdentityBase.cs` - Base class for identity components
  - `CommonBase.cs` - Common base with service injections
  - `Countdown.razor` - Countdown timer component

### ✅ Pages Copied
- **Account Pages**
  - Email/Phone/Password login pages
  - Logout page
  - Manage pages (Email, Phone, Password, Profile, Personal Access Tokens)
  - Server-side submit pages (.cshtml)

- **Base Pages**
  - Privacy page

### ✅ Domain Layer Copied  
- **Attributes** - Custom validation attributes (PhoneNumber, Password, AuthenticationCode)
- **Models** - Form models for authentication flows

### ✅ Infrastructure Copied
- **Services**
  - `EmailAuthenticationService` - Email-based authentication
  - `SmsAuthenticationService` - SMS-based authentication
  - `IdentityRedirectManager` - Navigation manager for identity flows
  - `IdentityRevalidatingAuthenticationStateProvider` - Auth state provider
  - `CipherService` - Encryption/decryption service
  - `PersonalAccessTokenService` - PAT management
  - `MessageQueue` & `MessageBackgroundService` - Message queuing
  - `MessageClientStub` - Message client stub
  - `AdminContext` - Admin context service

- **Extensions**
  - Various utility extensions (URL, Namespace, Number, etc.)
  - Blazor SSR redirect manager extensions
  - Identity components endpoint route builder extensions
  - JS modules extensions

- **Localization**
  - `Strings.cs` - Localization strings

- **WebApplication**
  - `WebApplicationBuilderExtensions.cs` - App builder configuration

### ✅ Namespaces Updated
All copied files have had their namespaces updated from:
- `Vorn.Aaas.Interface.*` → `Vitarish.Web.Users.*`

## What Still Needs To Be Done

### ⏳ Architecture Refactoring Required

The migrated code currently depends on:
1. **ASP.NET Identity** (`UserManager<User>`, `SignInManager<User>`, `IdentityDbContext`)
2. **Duende IdentityServer** (`IIdentityServerInteractionService`)
3. **Old Vorn services** from `_old` projects:
   - `Vorn.Aaas.Users` - User management
   - `Vorn.Aaas.Parties` - Tenant/owner management
   - `Vorn.Aaas.Client` - Client library
   - `Vorn.Aaas.AuthenticationClients` - Auth clients
   - `Vorn.Aaas.ApiClients` - API clients
   - `Vorn.Aaas.RequestIdentities` - Request identity abstraction

These need to be replaced with the new architecture:
- **Identity.Core** - User domain and CQRS commands/queries
- **Identity.Infrastructure** - User services and handlers
- **Communications.Contracts** - Messaging contracts (for OTP codes, etc.)
- **Communications.Infrastructure** - Messaging services

### 🔄 Required Architectural Changes

#### 1. Replace ASP.NET Identity with Custom Identity
The current code uses ASP.NET Identity's `UserManager` and `SignInManager`. This needs to be replaced with:
- Custom authentication services using Identity.Core entities
- CQRS pattern for user operations (MediatR)
- JWT-based authentication instead of cookie-based

#### 2. Replace Duende IdentityServer Integration
The current code integrates with Duende IdentityServer. This needs to be refactored to:
- Use the new `Identity.Api` for authentication
- Update OAuth/OIDC flows to work with new architecture

#### 3. Replace Message Client with Communications Services
Currently uses `Vorn.Caas.Client` for sending SMS/email. Should be updated to:
- Inject `IMessagesService` from `Communications.Contracts`
- Use MediatR commands (`SendSms`, `SendEmail`) from `Communications.Core`

#### 4. Update User Management
Replace direct `User` entity usage with:
- Commands: `CreateUserWithEmail`, `CreateUserWithPhone`, `UpdateUser`, etc.
- Queries: `GetUser`, `GetUserByName`, `GetUserProfile`, etc.
- From `Identity.Core.Users.Application.*`

#### 5. Project References
Currently has temporary references to `_old` projects. These should be replaced with:
```xml
<ProjectReference Include="..\..\..\Identity\Identity.Core\Identity.Core.csproj" />
<ProjectReference Include="..\..\..\Identity\Identity.Infrastructure\Identity.Infrastructure.csproj" />
<ProjectReference Include="..\..\..\Identity\Identity.Contracts\Identity.Contracts.csproj" />
<ProjectReference Include="..\..\..\Communications\Communications.Contracts\Communications.Contracts.csproj" />
<ProjectReference Include="..\..\..\Communications\Communications.Infrastructure\Communications.Infrastructure.csproj" />
```

### 📝 Testing
- Migrate tests from `Vorn.Aaas.Interface.Tests` to `Web.Users.Tests` (new project)
- Adapt tests to work with new architecture

### 🔐 Security Review
- Review authentication flows for security vulnerabilities
- Ensure proper CSRF protection
- Validate all input handling
- Review encryption/hashing implementations

## Notes

- The migration focused on getting files in the right structure with updated namespaces
- Functional integration with the new architecture is a separate, larger effort
- The old Vorn.Aaas.Interface project used .NET 8, while Web.Users uses .NET 10
- MudBlazor UI library is included (version 8.15.0)

## References

- [MIGRATION.md](../../MIGRATION.md) - Overall migration guide
- Issue #21 - Epic: Migrate legacy Vorn.* projects to Vitarish modular structure
- Vorn.Aaas.Interface → Split into Web.Admins, Web.Users, Web.Base

