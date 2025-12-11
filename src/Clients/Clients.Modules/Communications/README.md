# Communications client

Messaging client for sending email and SMS via the VORN CAAS endpoints. The client now lives under the `Communications/` module of `Vitarish.Client.Api` and reuses the shared AAAS authentication infrastructure.

## Prerequisites
- .NET 10 SDK
- Configuration for your CAAS endpoint (base address, default sender, timeout)

## Registration
```csharp
builder.Services.AddCommunicationsClient();
```

The extension registers `IMessageClient`, which you can inject into application services to send messages.

## Usage
- `SendEmailAsync` to send plain-text or HTML emails.
- `SendSmsAsync` to send SMS messages (returns `SmsSendResult` with provider metadata).
- `SendSmsCodeAsync` for one-time codes.
- `GetSmsDeliveryStateAsync` to poll for delivery status.

The client automatically creates an authenticated HTTP client using the AAAS infrastructure (`IApiClientService`).

## Configuration
Provide CAAS settings through configuration (e.g., `appsettings.json`):

```json
{
  "Vorn": {
    "Caas": {
      "BaseAddress": "https://caas.example.com/api/v1/caas/",
      "DefaultSender": "no-reply@example.com",
      "Timeout": 30
    }
  }
}
```

Override the section name or individual values via environment variables if needed.
