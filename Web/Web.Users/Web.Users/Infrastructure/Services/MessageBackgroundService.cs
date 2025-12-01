namespace Vitarish.Web.Users.Infrastructure.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Vorn.Caas.Client.Abstractions;

internal class MessageBackgroundService(
    ILogger<MessageBackgroundService> logger,
    IMessageQueue queue,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Message message = await queue.DequeueAsync(stoppingToken);
            string descriptor = $"{nameof(MessageBackgroundService)}:{message.GetType().Name}";
            using IServiceScope scope = scopeFactory.CreateScope();
            var messageClient = scope.ServiceProvider.GetRequiredService<IMessageClient>();
            using IDisposable? requestScope = CreateIdentityScope(scope.ServiceProvider, descriptor);
            try
            {
                switch (message)
                {
                    case EmailMessage email:
                        await messageClient.SendEmailAsync(email.To, email.Subject, email.TextBody, email.HtmlBody);
                        break;
                    case SmsMessage sms:
                        await messageClient.SendSmsCodeAsync(sms.PhoneNumber, sms.Code);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing background message");
            }
        }
    }
    private static IDisposable? CreateIdentityScope(IServiceProvider services, string descriptor)
    {
        Type? supplierType = Type.GetType(
            "Vorn.Aaas.Client.Api.Abstractions.IRequestIdentityContextBackgroundSupplier, Vorn.Aaas.Client.Api",
            throwOnError: false);
        if (supplierType is null)
        {
            return null;
        }

        object? supplier = services.GetService(supplierType);
        if (supplier is null)
        {
            return null;
        }

        MethodInfo? supply = supplierType.GetMethod("Supply");
        return supply?.Invoke(supplier, new object?[] { descriptor, null, null, null, descriptor }) as IDisposable;
    }
}
