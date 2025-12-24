# UI preview host

Use the lightweight preview host when you only need to render interface components without
IdentityServer, Redis, SQL, or module backends.

## Run

```bash
dotnet run --project tests/Hosts/Main/Main.Mock.csproj
```

The preview host uses a mock authentication handler and stub user profile service to keep
interface workflows available for design work. Configuration lives in
`tests/Hosts/Main/appsettings.Development.json`.
