using Microsoft.Extensions.Options;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Audit.Server;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Modules.Base.Core.BackgroundJobs;

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
            string idempotencyKey = ThresholdIdempotencyKey();
            AlertDeliveryResult delivery = await SendAlertAsync(
                CapacityExceededSubject,
                $"The audit table and indexes use {storageBytes} bytes, reaching the configured warning threshold of {threshold} bytes. Review audit capacity and PostgreSQL disk availability.",
                "The audit storage warning threshold has been reached. Review audit capacity and PostgreSQL disk availability.",
                cancellationToken).ConfigureAwait(false);

            await WriteAuditRecordAsync(
                CreateThresholdAuditEvent(storageBytes, threshold, delivery, idempotencyKey),
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
            string idempotencyKey = CheckFailureIdempotencyKey();
            _ = await SendAlertAsync(
                CheckFailedSubject,
                "Main could not read the audit storage size. Check PostgreSQL availability and audit storage.",
                "Main could not check audit storage. Check PostgreSQL availability.",
                cancellationToken).ConfigureAwait(false);

            await WriteAuditRecordAsync(
                CreateCheckFailureAuditEvent(idempotencyKey),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<AlertDeliveryResult> SendAlertAsync(
        string subject,
        string emailBody,
        string smsBody,
        CancellationToken cancellationToken)
    {
        if (!alertOptions.Value.HasRecipients)
        {
            logger.LogWarning("Audit capacity alert was detected, but administrator email and SMS recipients are not configured.");
            return AlertDeliveryResult.RecipientsNotConfigured;
        }

        bool emailAttempted = false;
        bool emailSucceeded = false;
        bool smsAttempted = false;
        bool smsSucceeded = false;

        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IMessagesService messages = scope.ServiceProvider.GetRequiredService<IMessagesService>();

            emailAttempted = true;
            emailSucceeded = await TrySendAsync(
                "email",
                async token => await messages.SendEmail(new Message
                {
                    Recipient = alertOptions.Value.EmailRecipient,
                    Subject = subject,
                    Body = emailBody
                }, token).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

            smsAttempted = true;
            smsSucceeded = await TrySendAsync(
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

        return new AlertDeliveryResult(
            RecipientsConfigured: true,
            EmailAttempted: emailAttempted,
            EmailSucceeded: emailSucceeded,
            SmsAttempted: smsAttempted,
            SmsSucceeded: smsSucceeded);
    }

    private async Task<bool> TrySendAsync(string channel, Func<CancellationToken, Task> send, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(AuditCapacityAlertOptions.OperationTimeout);

        try
        {
            await send(timeout.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError("Audit capacity alert delivery failed for {Channel} ({FailureType}).", channel, exception.GetType().Name);
            return false;
        }
    }

    private async Task WriteAuditRecordAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IBackgroundRequestContextWriter contextWriter = scope.ServiceProvider.GetRequiredService<IBackgroundRequestContextWriter>();
            contextWriter.SetContext(new BackgroundRequestContext(
                TenantBackgroundExecutor.BackgroundAuthority,
                ClientId: null,
                OwnerId: null,
                TenantId: SecurityAuditScopeCatalog.PlatformScopeId,
                ResourceId: auditEvent.Resource,
                RequestId: auditEvent.IdempotencyKey));

            ISecurityAuditWriter auditWriter = scope.ServiceProvider.GetRequiredService<ISecurityAuditWriter>();
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(AuditCapacityAlertOptions.OperationTimeout);
            _ = await auditWriter.WriteAsync(auditEvent, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError("Security audit recording failed for the audit capacity monitor ({FailureType}).", exception.GetType().Name);
        }
    }

    private static SecurityAuditEvent CreateThresholdAuditEvent(
        long storageBytes,
        long threshold,
        AlertDeliveryResult delivery,
        string idempotencyKey) => new()
    {
        EventName = SecurityAuditEventCatalog.AuditStorageThresholdEvent,
        EventCategory = SecurityAuditEventCatalog.SecurityFailureCategory,
        Outcome = delivery.AnyDelivered ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Failed,
        ActorKind = SecurityAuditActorKind.System,
        TenantId = SecurityAuditScopeCatalog.PlatformScopeId,
        Resource = SecurityAuditScopeCatalog.PlatformResource,
        Operation = SecurityAuditEventCatalog.AuditStorageThresholdOperation,
        OccurredAt = DateTime.UtcNow,
        CorrelationId = idempotencyKey,
        SourceBoundary = SecurityAuditEventCatalog.AuditStorageThresholdSourceBoundary,
        SourceIpKind = SecurityAuditSourceKind.System,
        RedactedReason = delivery.RecipientsConfigured
            ? SecurityAuditReasonCatalog.AuditStorageThreshold(storageBytes, threshold, delivery.EmailOutcome, delivery.SmsOutcome)
            : SecurityAuditReasonCatalog.AuditStorageThresholdWithoutRecipients(storageBytes, threshold),
        IdempotencyKey = idempotencyKey
    };

    private static SecurityAuditEvent CreateCheckFailureAuditEvent(string idempotencyKey) => new()
    {
        EventName = SecurityAuditEventCatalog.SecurityFailureEvent,
        EventCategory = SecurityAuditEventCatalog.SecurityFailureCategory,
        Outcome = SecurityAuditOutcome.Failed,
        ActorKind = SecurityAuditActorKind.System,
        TenantId = SecurityAuditScopeCatalog.PlatformScopeId,
        Resource = SecurityAuditScopeCatalog.PlatformResource,
        Operation = SecurityAuditEventCatalog.SecurityFailureOperation,
        OccurredAt = DateTime.UtcNow,
        CorrelationId = idempotencyKey,
        SourceBoundary = SecurityAuditEventCatalog.AuditStorageThresholdSourceBoundary,
        SourceIpKind = SecurityAuditSourceKind.System,
        RedactedReason = SecurityAuditReasonCatalog.AuditStoreUnavailable,
        IdempotencyKey = idempotencyKey
    };

    // Each alert attempt is a distinct action (after a restart or from another replica it may
    // deliver differently), so it gets its own record; one process attempts once per crossing.
    private static string ThresholdIdempotencyKey() =>
        $"{SecurityAuditEventCatalog.AuditStorageThresholdOperation}:{Guid.NewGuid():N}";

    private static string CheckFailureIdempotencyKey() =>
        $"{SecurityAuditEventCatalog.SecurityFailureOperation}:{SecurityAuditEventCatalog.AuditStorageThresholdOperation}:{Guid.NewGuid():N}";

    private sealed record AlertDeliveryResult(
        bool RecipientsConfigured,
        bool EmailAttempted,
        bool EmailSucceeded,
        bool SmsAttempted,
        bool SmsSucceeded)
    {
        public static AlertDeliveryResult RecipientsNotConfigured { get; } = new(
            RecipientsConfigured: false,
            EmailAttempted: false,
            EmailSucceeded: false,
            SmsAttempted: false,
            SmsSucceeded: false);

        public bool AnyDelivered => EmailSucceeded || SmsSucceeded;

        public string EmailOutcome => ChannelOutcome(EmailAttempted, EmailSucceeded);

        public string SmsOutcome => ChannelOutcome(SmsAttempted, SmsSucceeded);

        private static string ChannelOutcome(bool attempted, bool succeeded) =>
            !attempted
                ? SecurityAuditReasonCatalog.NotificationChannelNotAttempted
                : succeeded
                    ? SecurityAuditReasonCatalog.NotificationChannelSucceeded
                    : SecurityAuditReasonCatalog.NotificationChannelFailed;
    }
}
