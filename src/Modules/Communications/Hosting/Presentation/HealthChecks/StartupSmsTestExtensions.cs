
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Communications.Domain.Options;

namespace Vitarish.Communications.Hosting.Presentation.HealthChecks;

public static class StartupSmsTestExtensions
{
    /// <summary>
    /// Send a test SMS on application startup to the configured phone number to exercise SMS provider.
    /// The call is fire-and-forget and exceptions are logged to the console only so startup is not blocked.
    /// </summary>
    public static void UseStartupSmsTest(this WebApplication app) => _ = Task.Run(async () =>
                                                                          {
                                                                              try
                                                                              {
                                                                                  using IServiceScope scope = app.Services.CreateScope();
                                                                                  IMessagesService? svc = scope.ServiceProvider.GetService<IMessagesService>();
                                                                                  IOptionsMonitor<NotificationOptions>? options = scope.ServiceProvider.GetService<IOptionsMonitor<NotificationOptions>>();

                                                                                  if (svc is null)
                                                                                  {
                                                                                      Console.WriteLine("StartupSmsTest: IMessagesService not available");
                                                                                      return;
                                                                                  }

                                                                                  if (options is null)
                                                                                  {
                                                                                      Console.WriteLine("StartupSmsTest: NotificationOptions not available");
                                                                                      return;
                                                                                  }

                                                                                  string recipient = options.CurrentValue.StartupSmsRecipient;
                                                                                  if (string.IsNullOrWhiteSpace(recipient))
                                                                                  {
                                                                                      Console.WriteLine("StartupSmsTest: Startup SMS recipient not configured; skipping send");
                                                                                      return;
                                                                                  }

                                                                                  Message msg = new()
                                                                                  {
                                                                                      Recipient = recipient,
                                                                                      Body = "Startup health check SMS from Vorn Communications"
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
