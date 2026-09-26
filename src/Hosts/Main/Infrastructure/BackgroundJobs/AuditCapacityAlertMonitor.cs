using Microsoft.Extensions.Options;
using Vitarish.Audit.Application;
using Vitarish.Audit.Server;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Main.Infrastructure.Options;

namespace Vitarish.Main.Infrastructure.BackgroundJobs;

internal sealed class AuditCapacityAlertMonitor(
    IServiceScopeFactory scopeFactory,
    IOptions<SecurityAuditOptions> auditOptions,
    IOptions<AuditCapacityAlertOptions> alertOptions,
    ILogger<AuditCapacityAlertMonitor> logger) : BackgroundService
{
    private const string CapacityExceededSubject = "Audit storage capacity warning";
    private const string CheckFailedSubject = "Audit storage check failed";

    private bool capacityAlertAttempted;
    private bool checkFailureAlertAttempted;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(AuditCapacityAlertOptions.CheckInterval);

        do
        {
            await CheckOnceAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    internal async Task CheckOnceAsync(CancellationToken cancellationToken)
    {
        long storageBytes;

        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(AuditCapacityAlertOptions.OperationTimeout);

            IAuditStorageSizeReader storageSizeReader = scope.ServiceProvider.GetRequiredService<IAuditStorageSizeReader>();
            storageBytes = await storageSizeReader.GetStorageSizeBytesAsync(timeout.Token).ConfigureAwait(false);

            if (storageBytes < 0)
                throw new InvalidOperationException("Audit storage size cannot be negative.");

            checkFailureAlertAttempted = false;

            long threshold = auditOptions.Value.CapacityWarningBytes;
            if (threshold <= 0)
                return;

            if (storageBytes < threshold)
            {
                capacityAlertAttempted = false;
                return;
            }

            if (capacityAlertAttempted)
                return;

            capacityAlertAttempted = true;
            await SendAlertAsync(
                CapacityExceededSubject,
                $"The audit table and indexes use {storageBytes} bytes, reaching the configured warning threshold of {threshold} bytes. Review audit capacity and PostgreSQL disk availability.",
                "The audit storage warning threshold has been reached. Review audit capacity and PostgreSQL disk availability.",
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError("Audit storage check failed ({FailureType}).", exception.GetType().Name);

            if (checkFailureAlertAttempted)
                return;

            checkFailureAlertAttempted = true;
            await SendAlertAsync(
                CheckFailedSubject,
                "Main could not read the audit storage size. Check PostgreSQL availability and audit storage.",
                "Main could not check audit storage. Check PostgreSQL availability.",
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendAlertAsync(
        string subject,
        string emailBody,
        string smsBody,
        CancellationToken cancellationToken)
    {
        if (!alertOptions.Value.HasRecipients)
        {
            logger.LogWarning("Audit capacity alert was detected, but administrator email and SMS recipients are not configured.");
            return;
        }

        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IMessagesService messages = scope.ServiceProvider.GetRequiredService<IMessagesService>();

            await TrySendAsync(
                "email",
                async token => await messages.SendEmail(new Message
                {
                    Recipient = alertOptions.Value.EmailRecipient,
                    Subject = subject,
                    Body = emailBody
                }, token).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

            await TrySendAsync(
                "SMS",
                async token => _ = await messages.SendSms(new Message
                {
                    Recipient = alertOptions.Value.SmsRecipient,
                    Body = smsBody
                }, token).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError("Audit capacity alert setup failed ({FailureType}).", exception.GetType().Name);
        }
    }

    private async Task TrySendAsync(string channel, Func<CancellationToken, Task> send, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(AuditCapacityAlertOptions.OperationTimeout);

        try
        {
            await send(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError("Audit capacity alert delivery failed for {Channel} ({FailureType}).", channel, exception.GetType().Name);
        }
    }
}
