# Vitarish Web Clients

Client library for integrating ASP.NET Core (.NET 10) Razor Components/Blazor Server apps with Vitarish Identity.

It wires up OpenID Connect cookie authentication, exposes login/logout endpoints, configures authorization with access-claim requirements, provides a strongly-typed access service for querying/updating user access, and includes reusable UI components.

## Features

- OpenID Connect cookie authentication with refresh token revocation on logout
- Login/logout endpoints mapped under `/authentication`
- Default authorization policy requiring an `access` claim; built-in `Admin` policy
- `IAccessService` for reading and mutating client-specific access and roles
- Razor Components/Blazor Server ready (adds authentication state and SignalR options)
- Data Protection keys persisted to `Keys/` and protected by a generated PFX certificate
- Preconfigured JWT Bearer authentication for API endpoints (accepts `at+jwt` access tokens)
- Drop-in UI components for login, logout, profile, and simple admin experiences

## Requirements

- .NET 10
- ASP.NET Core app (Razor Components/Blazor Server or MVC + Components)
- An OpenID Connect authority (e.g., IdentityServer, Entra ID, Auth0, etc.) configured for your client

## Configuration

Add the `Vitarish` section to your `appsettings.json` or environment variables:

```json
{
  "Vitarish": {
    "Identity": {
      "Authority": "https://auth.example.com",
      "ClientId": "your-client-id",
      "Secret": "your-client-secret",
      "CookieExpirationDays": 14,
      "DataProtectionPassword": "use-a-strong-secret"
    }
  }
}
```

Settings reference (see `Vitarish.Web.Clients.Options.IdentityOptions`):

- `Authority` (required): OIDC authority base URL
- `ClientId` (required): Registered OIDC client ID
- `Secret` (required): Client secret used for token revocation and API calls if applicable
- `CookieExpirationDays` (optional): Sliding cookie expiration window; default `14`
- `DataProtectionPassword` (recommended): Password used to protect the generated `DataProtection.pfx`
- `DataProtectionDomain` (optional): Not required by the client; the domain is inferred from `Authority`

## Quick Start

Program setup in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Registers OIDC Cookie + JWT auth, authorization policies, services, components, API client, data protection, etc.
builder.AddIdentityClient();

var app = builder.Build();

// Adds HSTS/HTTPS, static files, authN, authZ, antiforgery, and auth endpoints
app.UseIdentityClient();

// Map your components/routes as usual
// app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
```

Add component import(s) once (e.g., to `_Imports.razor`):

```razor
@using Vitarish.Web.Clients
@using Vitarish.Web.Clients.Admin
```

## Authentication Endpoints

The library maps the following endpoints under the `/authentication` route group:

- `GET /authentication/login?returnUrl=/path` — challenges OIDC and redirects back
- `POST /authentication/logout` — signs out, revokes refresh token, and redirects

## Authorization

The default policy requires an authenticated user and a valid access claim. An `Admin` policy is defined for privileged operations.

Constants are available in `Vitarish.Web.Clients.Constants.AuthConstants`:

- `AuthConstants.Admin` — policy name for admin access
- `AuthConstants.AccessClaim` — the access claim type (`"access"`)

Examples:

```csharp
using Microsoft.AspNetCore.Authorization;
using Vitarish.Web.Clients.Constants;

[Authorize] // default policy requires access claim
public class SecurePageModel : PageModel { }

[Authorize(Policy = AuthConstants.Admin)]
public class AdminPageModel : PageModel { }
```

## Access Service

Use `IAccessService` to initialize and query the current user's access and manage client users/roles.

```razor
@inject Vitarish.Web.Clients.Abstractions.IAccessService AccessService

@code {
    protected override async Task OnInitializedAsync()
    {
        await AccessService.InitializeAsync(); // uses configured ClientId by default

        // Current user
        var me = AccessService.User;
        var access = AccessService.Access; // includes roles/readers/writers/Admin flag

        // All client users (if you have Admin access)
        var users = AccessService.Users;
    }
}
```

Admin flows can update access (when `Access.Admin` is true):

```csharp
await AccessService.SetUserAccessAsync(userId, clientId, accessValue);
await AccessService.RemoveUserAccessAsync(userId, clientId);
```

## UI Components

These Razor components are available to drop into your app:

- `IdentityLogin` — show a login button/flow
- `IdentityLogout` — show a logout button/flow
- `IdentityProfile` — basic profile info for the current user
- Admin components:
  - `AccessUsersMatrix`
  - `AccessClaimsMatrix`
  - `RoleEditor`

Example:

```razor
<IdentityLogin />
<IdentityProfile />

@attribute [Authorize(Policy = Vitarish.Web.Clients.Constants.AuthConstants.Admin)]
<AccessUsersMatrix />
```

## Data Protection

`AddIdentityClient` configures ASP.NET Core Data Protection to:

- Persist keys to `./Keys`
- Protect keys with a generated `DataProtection.pfx` (CN derived from `Vitarish:Identity:Authority`)
- Use `Vitarish:Identity:DataProtectionPassword` to secure the PFX

Recommendations:

- Do not commit `DataProtection.pfx` or `Keys/` to source control; persist them across deployments with external storage
- Provide `DataProtectionPassword` via environment variables or user-secrets in development

## JWT Bearer for APIs

`AddIdentityClient` also configures `JwtBearerDefaults.AuthenticationScheme` with:

- `Authority` = `Vitarish:Identity:Authority`
- `ValidateAudience = false` (adjust to your needs)
- `ValidTypes = [ "at+jwt" ]`

Use it on API controllers as needed:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("api/[controller]")]
public class ValuesController : ControllerBase { }
```

## Migration from Vorn.Aaas.Client

This library is the successor to `Vorn.Aaas.Client`. Key changes:

- Namespace: `Vorn.Aaas.Client` → `Vitarish.Web.Clients`
- Configuration section: `Vorn:Aaas` → `Vitarish:Identity`
- Component names: `AaasLogin`, `AaasLogout`, `AaasProfile` → `IdentityLogin`, `IdentityLogout`, `IdentityProfile`
- Constants: `AaasConstants` → `AuthConstants`
- Extension methods: `AddAaasClient()`, `UseAaasClient()` → `AddIdentityClient()`, `UseIdentityClient()`

## License

See the repository's license file for details.
