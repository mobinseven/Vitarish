using System;
using System.Threading;
using System.Threading.Tasks;
using Vorn.Caas.Messages.Domain.Interfaces;
using Vorn.Caas.Messages.Domain.Models;

namespace Vorn.Caas.Host.Presentation.HealthChecks;

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
                if (svc is null)
                {
                    Console.WriteLine("StartupSmsTest: IMessagesService not available");
                    return;
                }

                var msg = new Message
                {
                    Recipient = "+989212142909", // TODO: UNSAFE
                    Body = "Startup health check SMS from Vorn.Caas.Host"
                };

                SmsSendResult result = await svc.SendSms(msg, CancellationToken.None);
                Console.WriteLine($"StartupSmsTest: SMS send attempt completed with message ID {result.MessageId} and cost {result.Cost}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StartupSmsTest: exception while sending SMS: {ex.Message}");
            }
        });
    }
}
