
# Vorn.Files.Client

Blazor-ready client SDK that connects to `Vorn.Files.Host` for secure file streaming via SignalR. Includes `IFilesClient`, a `<StreamTransfer />` component, checksum helpers, and DI extensions.

Prerequisites

- ASP.NET Core 8 application (Blazor Server or WASM)
- Configured `Vorn.Files.Host` and AAAS client (`Vorn.Aaas.Client.Api`)

Installation

```bash
dotnet add package Vorn.Files.Client
```

Usage

- Register services with `builder.AddFilesClient()` after configuring AAAS.
- Inject `IFilesClient` or use the `StreamTransfer` component in Razor pages.

Component

- `<StreamTransfer />` provides upload UI, automatic SHA-256 hashing, optional thumbnails, and streaming to the server via a bounded `Channel<byte[]>`.

Advanced

- Use `ChecksumHelpers` for standalone hashing scenarios.
- Use `NamespaceExtension.NamespaceToPath` for custom asset paths.

Contributing

Follow project guidelines in `AGENTS.md`. Open issues and PRs against `main`.

License

See repository `LICENSE` file.
