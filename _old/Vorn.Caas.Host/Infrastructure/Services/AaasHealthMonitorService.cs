namespace Vorn.Caas.Host.Infrastructure.Services;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vorn.Caas.Messages.Domain.Interfaces;
using Vorn.Caas.Messages.Domain.Models;
using Vorn.Caas.Messages.Domain.Options;

internal sealed class AaasHealthMonitorService(
    IMessagesService messagesService,
    VornOptions options,
    ILogger<AaasHealthMonitorService> logger)
    : BackgroundService
{
    private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private readonly HttpClient httpClient = new() { Timeout = RequestTimeout };
    private bool failureNotified;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(messagesService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        if (string.IsNullOrWhiteSpace(options.Aaas.Authority))
        {
            logger.LogWarning("AAAS authority is not configured; skipping authority health monitoring.");
            return;
        }

        logger.LogInformation("AAAS health monitor started for {Authority}", options.Aaas.Authority);

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
                    logger.LogInformation("AAAS authority {Authority} is healthy again.", options.Aaas.Authority);
                }

                failureNotified = false;
                return;
            }

            logger.LogWarning("AAAS authority {Authority} health check failed.", options.Aaas.Authority);

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
            logger.LogError(ex, "Unexpected error while evaluating AAAS health.");
        }
    }

    private async Task<bool> CheckAuthorityHealthAsync(CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(options.Aaas.Authority, UriKind.Absolute, out Uri? authorityUri))
        {
            logger.LogWarning("AAAS authority value '{Authority}' is not a valid absolute URI.", options.Aaas.Authority);
            return false;
        }

        Uri discoveryUri = new(authorityUri, ".well-known/openid-configuration");

        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(discoveryUri, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning(
                "AAAS authority {Authority} responded with status code {StatusCode}.",
                discoveryUri,
                response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Error while checking AAAS authority {Authority}.", discoveryUri);
        }

        return false;
    }

    private async Task NotifyFailureAsync()
    {
        try
        {
            Message message = new()
            {
                Recipient = "+989212142909",
                Body = $"AAAS health check failed for {options.Aaas.Authority} at {DateTimeOffset.UtcNow:O}"
            };

            SmsSendResult result = await messagesService.SendSms(message, CancellationToken.None);
            logger.LogInformation(
                "AAAS health failure notification SMS sent to {Recipient} with message ID {MessageId}.",
                message.Recipient,
                result.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send AAAS health failure SMS notification.");
        }
    }

    public override void Dispose()
    {
        httpClient.Dispose();
        base.Dispose();
    }
}
