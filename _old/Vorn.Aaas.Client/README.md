# Vorn.Aaas.Client

Client library for integrating ASP.NET Core (.NET 8) Razor Components/Blazor Server apps with Vorn AAAS (Authentication and Access-as-a-Service).

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

- .NET 8
- ASP.NET Core app (Razor Components/Blazor Server or MVC + Components)
- An OpenID Connect authority (e.g., IdentityServer, Entra ID, Auth0, etc.) configured for your client


## Installation

Install from NuGet:

```
dotnet add package Vorn.Aaas.Client
```

(The API client is brought in transitively.)


## Configuration

Add the `Vorn:Aaas` section to your `appsettings.json` or environment variables:

```json
{
  "Vorn": {
    "Aaas": {
      "Authority": "https://auth.example.com",
      "ClientId": "your-client-id",
      "Secret": "your-client-secret",
      "CookieExpirationDays": 14,
      "DataProtectionPassword": "use-a-strong-secret"
    }
  }
}
```

Settings reference (see `Vorn.Aaas.Client.Api.Models.AaasOptions`):

- `Authority` (required): OIDC authority base URL
- `ClientId` (required): Registered OIDC client ID
- `Secret` (required): Client secret used for token revocation and API calls if applicable
- `CookieExpirationDays` (optional): Sliding cookie expiration window; default `14`
- `DataProtectionPassword` (recommended): Password used to protect the generated `DataProtection.pfx`
- `DataProtectionDomain` (optional): Not required by the client; the domain is inferred from `Authority`


## Quick start

Program setup in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Registers OIDC Cookie + JWT auth, authorization policies, services, components, API client, data protection, etc.
builder.AddAaasClient();

var app = builder.Build();

// Adds HSTS/HTTPS, static files, authN, owner-id middleware, authZ, antiforgery, and auth endpoints
app.UseAaasClient();

// Map your components/routes as usual
// app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
```

Add component import(s) once (e.g., to `_Imports.razor`):

```razor
@using Vorn.Aaas.Client.Components
@using Vorn.Aaas.Client.Components.Admin
```


## Authentication endpoints

The library maps the following endpoints under the `/authentication` route group:

- `GET /authentication/login?returnUrl=/path` — challenges OIDC and redirects back
- `POST /authentication/logout` — signs out, revokes refresh token, and redirects


## Authorization

The default policy requires an authenticated user and a valid access claim. An `Admin` policy is defined for privileged operations.

Constants are available in `Vorn.Aaas.Client.Constants.AaasConstants`:

- `AaasConstants.Admin` — policy name for admin access
- `AaasConstants.AccessClaim` — the access claim type (`"access"`)

Examples:

```csharp
using Microsoft.AspNetCore.Authorization;
using Vorn.Aaas.Client.Constants;

[Authorize] // default policy requires access claim
public class SecurePageModel : PageModel { }

[Authorize(Policy = AaasConstants.Admin)]
public class AdminPageModel : PageModel { }
```


## Access service

Use `IAccessService` to initialize and query the current user’s access and manage client users/roles.

```razor
@inject Vorn.Aaas.Client.Services.IAccessService AccessService

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


## UI components

These Razor components are available to drop into your app:

- `AaasLogin` — show a login button/flow
- `AaasLogout` — show a logout button/flow
- `AaasProfile` — basic profile info for the current user
- Admin components:
  - `AccessUsersMatrix`
  - `AccessClaimsMatrix`
  - `RoleEditor`

Example:

```razor
<AaasLogin />
<AaasProfile />

@attribute [Authorize(Policy = Vorn.Aaas.Client.Constants.AaasConstants.Admin)]
<AccessUsersMatrix />
```


## Data Protection

`AddAaasClient` configures ASP.NET Core Data Protection to:

- Persist keys to `./Keys`
- Protect keys with a generated `DataProtection.pfx` (CN derived from `Vorn:Aaas:Authority`)
- Use `Vorn:Aaas:DataProtectionPassword` to secure the PFX

Recommendations:

- Do not commit `DataProtection.pfx` or `Keys/` to source control; persist them across deployments with external storage
- Provide `DataProtectionPassword` via environment variables or user-secrets in development


## JWT Bearer for APIs

`AddAaasClient` also configures `JwtBearerDefaults.AuthenticationScheme` with:

- `Authority` = `Vorn:Aaas:Authority`
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


## Troubleshooting

- Correlation failed during OIDC login: The library clears cookies and redirects to a user-friendly error. Ensure your app’s public URL, proxy headers, and cookie domains are configured correctly.
- Infinite login loop: Check the `Vorn:Aaas:Authority`, client registration, and required scopes/claims. Clear cookies and try again.
- Data protection key issues across instances: Persist the `Keys/` folder and `DataProtection.pfx` across all instances, or configure a shared key repository (e.g., Redis, blob storage) and a shared certificate.


## Example minimal `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddAaasClient();
var app = builder.Build();
app.UseAaasClient();
app.Run();
```


## License

See the repository’s license file for details.

