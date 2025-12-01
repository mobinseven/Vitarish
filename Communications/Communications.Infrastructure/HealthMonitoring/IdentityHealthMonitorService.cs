namespace Vitarish.Communications.Infrastructure.HealthMonitoring;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitarish.Communications.Core.Messages.Abstractions;
using Vitarish.Communications.Core.Messages.Models;
using Vitarish.Communications.Infrastructure.Messages.Options;

internal sealed class IdentityHealthMonitorService(
    IMessagesService messagesService,
    CommunicationsOptions options,
    ILogger<IdentityHealthMonitorService> logger,
    IHttpClientFactory httpClientFactory)
    : BackgroundService
{
    private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private bool failureNotified;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(messagesService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        if (string.IsNullOrWhiteSpace(options.IdentityAuthority))
        {
            logger.LogWarning("Identity authority is not configured; skipping authority health monitoring.");
            return;
        }

        logger.LogInformation("Identity health monitor started for {Authority}", options.IdentityAuthority);

        await EvaluateHealthAsync(stoppingToken);

        using PeriodicTimer timer = new(HealthCheckInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await EvaluateHealthAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown.
        }
    }

    private async Task EvaluateHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            bool healthy = await CheckAuthorityHealthAsync(cancellationToken);

            if (healthy)
            {
                if (failureNotified)
                {
                    logger.LogInformation("Identity authority {Authority} is healthy again.", options.IdentityAuthority);
                }

                failureNotified = false;
                return;
            }

            logger.LogWarning("Identity authority {Authority} health check failed.", options.IdentityAuthority);

            if (failureNotified)
            {
                return;
            }

            await NotifyFailureAsync();
            failureNotified = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Ignore cancellations triggered by shutdown.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while evaluating Identity authority health.");
        }
    }

    private async Task<bool> CheckAuthorityHealthAsync(CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(options.IdentityAuthority, UriKind.Absolute, out Uri? authorityUri))
        {
            logger.LogWarning("Identity authority value '{Authority}' is not a valid absolute URI.", options.IdentityAuthority);
            return false;
        }

        Uri discoveryUri = new(authorityUri, ".well-known/openid-configuration");

        try
        {
            using var timeoutCts = new CancellationTokenSource(RequestTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            using var httpClient = httpClientFactory.CreateClient();
            using HttpResponseMessage response = await httpClient.GetAsync(discoveryUri, linkedCts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning(
                "Identity authority {Authority} responded with status code {StatusCode}.",
                discoveryUri,
                response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Error while checking Identity authority {Authority}.", discoveryUri);
        }

        return false;
    }

    private async Task NotifyFailureAsync()
    {
        if (string.IsNullOrWhiteSpace(options.HealthCheckNotificationRecipient))
        {
            logger.LogWarning("Health check notification recipient not configured; skipping notification.");
            return;
        }

        try
        {
            Message message = new()
            {
                Recipient = options.HealthCheckNotificationRecipient,
                Body = $"Identity authority health check failed for {options.IdentityAuthority} at {DateTimeOffset.UtcNow:O}"
            };

            SmsSendResult result = await messagesService.SendSmsAsync(message, CancellationToken.None);
            logger.LogInformation(
                "Identity health failure notification SMS sent to {Recipient} with message ID {MessageId}.",
                message.Recipient,
                result.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Identity health failure SMS notification.");
        }
    }
}
