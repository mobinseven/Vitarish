using Microsoft.Extensions.Logging;

namespace Vorn.Caas.Client.Abstractions;

/// <summary>
/// Stub implementation of IMessageClient for testing until Vorn.Caas dependency is available.
/// This implementation logs messages instead of actually sending them.
/// </summary>
public class MessageClientStub(ILogger<MessageClientStub> logger) : IMessageClient
{
    public Task SendEmailAsync(string to, string subject, string textBody, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Stub: Would send email with subject '{Subject}' (actual sending not implemented)", subject);
        return Task.CompletedTask;
    }

    public Task SendSmsCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Stub: Would send SMS code (actual sending not implemented)");
        return Task.CompletedTask;
    }
}
