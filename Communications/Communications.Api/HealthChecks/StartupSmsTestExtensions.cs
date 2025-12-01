using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Communications.Core.Messages.Abstractions;
using Vitarish.Communications.Core.Messages.Models;
using Vitarish.Communications.Infrastructure.Messages.Options;

namespace Vitarish.Communications.Api.HealthChecks;

public static class StartupSmsTestExtensions
{
    /// <summary>
    /// Send a test SMS on application startup to the configured phone number to exercise SMS provider.
    /// The call is fire-and-forget and exceptions are logged to the console only so startup is not blocked.
    /// </summary>
    public static void UseStartupSmsTest(this WebApplication app)
    {
        // Run after the app is built and before handling requests.
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var svc = scope.ServiceProvider.GetService<IMessagesService>();
                var options = scope.ServiceProvider.GetService<CommunicationsOptions>();
                
                if (svc is null)
                {
                    Console.WriteLine("StartupSmsTest: IMessagesService not available");
                    return;
                }

                if (options is null || string.IsNullOrWhiteSpace(options.HealthCheckNotificationRecipient))
                {
                    Console.WriteLine("StartupSmsTest: HealthCheckNotificationRecipient not configured; skipping startup test");
                    return;
                }

                var msg = new Message
                {
                    Recipient = options.HealthCheckNotificationRecipient,
                    Body = "Startup health check SMS from Communications.Api"
                };

                SmsSendResult result = await svc.SendSmsAsync(msg, CancellationToken.None);
                Console.WriteLine($"StartupSmsTest: SMS send attempt completed with message ID {result.MessageId} and cost {result.Cost}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StartupSmsTest: exception while sending SMS: {ex.Message}");
            }
        });
    }
}
