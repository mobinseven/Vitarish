using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Communications.Core.Messages.Abstractions;
using Vitarish.Communications.Core.Messages.Models;
using Vitarish.Communications.Infrastructure.Messages.Options;

namespace Vitarish.Communications.Api.HealthChecks;

public static class StartupSmsTestExtensions
{
    private sealed class StartupSmsTestLogger { }

    /// <summary>
    /// Send a test SMS on application startup to the configured phone number to exercise SMS provider.
    /// Uses proper application lifetime events for better integration with the hosting infrastructure.
    /// </summary>
    public static void UseStartupSmsTest(this WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var logger = app.Services.GetRequiredService<ILogger<StartupSmsTestLogger>>();
            var cancellationToken = app.Lifetime.ApplicationStopping;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    // Small delay to ensure all services are fully initialized
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    
                    using var scope = app.Services.CreateScope();
                    var svc = scope.ServiceProvider.GetService<IMessagesService>();
                    var options = scope.ServiceProvider.GetService<IOptions<CommunicationsOptions>>()?.Value;
                    
                    if (svc is null)
                    {
                        logger.LogWarning("StartupSmsTest: IMessagesService not available");
                        return;
                    }

                    if (options is null || string.IsNullOrWhiteSpace(options.HealthCheckNotificationRecipient))
                    {
                        logger.LogInformation("StartupSmsTest: HealthCheckNotificationRecipient not configured; skipping startup test");
                        return;
                    }

                    var msg = new Message
                    {
                        Recipient = options.HealthCheckNotificationRecipient,
                        Body = "Startup health check SMS from Communications.Api"
                    };

                    SmsSendResult result = await svc.SendSmsAsync(msg, cancellationToken);
                    logger.LogInformation(
                        "StartupSmsTest: SMS send attempt completed with message ID {MessageId} and cost {Cost}",
                        result.MessageId,
                        result.Cost);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("StartupSmsTest: Cancelled during application shutdown");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "StartupSmsTest: Exception while sending SMS");
                }
            }, cancellationToken);
        });
    }
}
