# Vorn.Caas.Client

Client SDK for sending email and SMS via the Vorn CAAS (Content as a Service).

Prerequisites

- .NET 8 SDK

Installation

Add the package or project reference:

```bash
dotnet add package Vorn.Caas.Client
```

Or reference the project in your solution.

Configuration

Use the `Caas` configuration section or `AddCaasClient` to provide `BaseAddress`, `DefaultSender`, and `Timeout`.

Usage

- Register with `builder.Services.AddCaasClient(...)`.
- Inject `IMessageClient` and call `SendEmailAsync`, `SendSmsAsync`, or `SendSmsCodeAsync`.
  `SendSmsAsync` returns an `SmsSendResult` containing the provider-agnostic message identifier, pack ID, pricing, and metadata.
- Use `GetSmsDeliveryStateAsync` to poll for delivery updates when required; it returns an `SmsDeliveryState` with recipient, delivery timestamps, status code, and provider metadata.

Development and testing

- Target: .NET 8
- Mock `IMessageClient` or `HttpMessageHandler` for unit tests; integration tests can run against a test CAAS API.

Contributing

Open issues and PRs against `main`. Follow the repository contribution guidelines.

License

See repository `LICENSE` file.
