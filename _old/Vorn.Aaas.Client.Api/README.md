# Vorn.Aaas.Client.Api

Lightweight .NET 8 library for consuming VORN AAAS-protected APIs from your ASP.NET Core applications.

It provides DI-ready services that:

- Register with a single extension `AddAaasApiClient`.
- Acquire OAuth2 client-credentials access tokens using Duende IdentityModel.
- Cache tokens in-memory and refresh them proactively.
- Attach owner/client context headers per request.
- Create `HttpClient` instances preconfigured with base address and bearer tokens.

## Packages and target framework

- Target framework: .NET 8
- Uses Duende IdentityModel and Microsoft.Extensions.*

## Getting started

1) Add a project reference to `Vorn.Aaas.Client.Api` from your web/API project.

2) Configure options in `appsettings.json` under the `Vorn:Aaas` section.

3) Call `AddAaasApiClient` on your `WebApplicationBuilder` and inject the services you need.

### Configuration

Add the following section to your configuration (appsettings.json, user secrets, or environment variables):

```json
{
  "Vorn": {
    "Aaas": {
      "Authority": "https://auth.yourdomain.tld",
      "Secret": "your-access-client-secret",
      "ClientId": "your-default-owner-client-id",
      "Local": null,
      "DataProtectionDomain": null,
      "DataProtectionPassword": null,
      "CookieExpirationDays": 14
    }
  }
}
```

Notes:
- `Authority` is your AAAS authority/STS base URL.
- `Secret` is used by the internal access client (see `AaasConstants.AccessClient`).
- `ClientId` is an optional default owner/client id used when no explicit owner is set.
- `Local` (any non-null value) appends a `.local` suffix to resolved API client ids (used by `IApiClientService`).

Environment variables (example):
- `Vorn__Aaas__Authority`
- `Vorn__Aaas__Secret`
- `Vorn__Aaas__ClientId`
- `Vorn__Aaas__Local`

### Registering services

In `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Registers options and the AAAS client services
builder.AddAaasApiClient();

var app = builder.Build();
app.Run();
```

This registers:
- `IAaasClientFactory` for generic AAAS-backed clients.
- `IApiClientService` for endpoint-specific API clients discovered by endpoint naming.
- `IOwnerContext` for per-request owner/client id handling.

## Usage

### 1) Create a client for an AAAS endpoint using `IAaasClientFactory`

```csharp
public class MyService(IAaasClientFactory factory)
{
    public async Task<HttpResponseMessage> GetAsync()
    {
        // Optional: provide endpoint path and an overriding owner client id
        using var client = await factory.CreateClient("/api/resource/", clientId: "owner.client.id");
        return await client.GetAsync("items");
    }
}
```

Behavior:
- Base address: `Authority` combined with the provided endpoint via `UrlExtensions.UrlCombine`.
- Header added: `Content-Owner` (when available).
- Bearer token is automatically acquired and attached.

### 2) Create a client for a specific API using `IApiClientService`

`IApiClientService` resolves an `ApiClient` by the last segment of the endpoint. For example, for endpoint `/api/orders/`, it looks up client id `api.orders` (or `api.orders.local` when `Vorn:Aaas:Local` is set).

```csharp
public class OrdersService(IApiClientService apiClientService)
{
    public async Task<IReadOnlyList<Order>> GetOrdersAsync()
    {
        using var client = await apiClientService.CreateClient("/api/orders/");
        var response = await client.GetAsync("list");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Order>>() ?? [];
    }
}
```

Behavior:
- Discovers `ApiClient` (`id`, `secret`, `scopes`, `hostUri`) via AAAS at `/api/apiclients/{clientId}`.
- Acquires a client-credentials token for the discovered client.
- Base address: `https://{HostUri}` combined with the provided endpoint.
- Header added: `Content-Owner` (when available).

### 3) Manually retrieving an access token

```csharp
public class TokenService(IApiClientService apiClientService)
{
    public async Task<string> GetTokenAsync()
    {
        var apiClient = await apiClientService.GetApiClient("api.orders")
                        ?? throw new InvalidOperationException("Client not found");
        return await apiClientService.GetAccessTokenAsync(apiClient)
               ?? throw new InvalidOperationException("Token unavailable");
    }
}
```

### 4) Register and create SignalR hub connections

Implement `ISignalRClientDefinition` on your hub client to provide its default registration. Use `AddSignalRClient` only when you need to customize the defaults, and `ISignalRClientFactory` to create connections with AAAS headers and access tokens applied automatically.

```csharp
builder.Services
       .AddAaasApiClient(builder.Configuration)
       .AddSignalRClient<FileTransferHub>();

public class FileTransferHub(ISignalRClientFactory connectionFactory) : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration)
    {
        registration.ClientId = "/files";
        registration.ResourceId = "/hubs/transfer";
    }

    public Task<HubConnection> CreateAsync(CancellationToken cancellationToken = default) =>
        connectionFactory.CreateConnection<FileTransferHub>(cancellationToken);
}

// Usage example
HubConnection connection = await connectionFactory.CreateConnection<FileTransferHub>();
await connection.StartAsync();
```

`SignalRClientRegistration` automatically resolves the AAAS API client based on the configured client id (override with `ApiClientId` if necessary), composes the hub path as `/{clientId}/{resourceId}`, applies owner headers, and configures reconnects and MessagePack by default. Customize the registration by calling `AddSignalRClient<FileTransferHub>(options => { ... })` or tweak the underlying SignalR builder through the `ConfigureBuilder` and `ConfigureHttpConnection` delegates when additional options are required.

Per-connection adjustments are also supported through `ISignalRClientFactory`:

```csharp
HubConnection connection = await connectionFactory.CreateConnection<FileTransferHub>(
    new SignalRConnectionOptions
    {
        UseMessagePack = false,
        ConfigureBuilder = builder => builder.WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5) })
    });
```

## Headers

- `Content-Owner` header is sent by clients created via `IAaasClientFactory` and `IApiClientService`.

Make sure your downstream APIs expect and authorize these headers as needed.

## Token caching and concurrency

- Tokens are cached in-memory with `IMemoryCache`.
- Proactive refresh: tokens are refreshed 60 seconds before expiry.
- Concurrent requests are synchronized with a per-service `SemaphoreSlim` to avoid token stampedes.

## Errors and diagnostics

- Missing or invalid configuration throws `InvalidOperationException` during startup.
- Discovery or token acquisition failures throw detailed exceptions including the underlying protocol error.
- `HttpResponseMessage.EnsureSuccessStatusCode()` is used where appropriate; handle exceptions accordingly.

## Development and configuration tips

- Use ASP.NET Core user secrets for local development instead of committing secrets to source control.
- Environment variable mapping uses double underscores: `Vorn__Aaas__Secret`, etc.
- Setting `Vorn:Aaas:Local` to any non-null value appends `.local` to resolved API client ids (useful for local stacks).

## Public surface

- `BuilderExtensions.AddAaasApiClient`
- `IAaasClientFactory` / `AaasClientFactory`
- `IApiClientService` / `ApiClientService`
- `ISignalRClientFactory` / `SignalRClientFactory`
- `IOwnerContext` / `OwnerContext`
- `VornOptions`, `AaasOptions`, `ApiClientDto`
- `SignalRClientRegistration`, `SignalRClientOptions`
- `AaasConstants`, `UrlExtensions`

## Contributing

Issues and contributions are welcome. Please include a clear description and, if possible, a minimal reproduction.

## Security

Store secrets securely (Key Vault, environment variables, or user secrets). Do not store secrets in source control.
