using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Domain.Entities;
using Vitarish.Platform.Application.Validation;
using Vitarish.Platform.Infrastructure.Data;

namespace Vitarish.Platform.Infrastructure.Services;

public sealed class PlatformLocaleNotificationStore(PlatformDbContext dbContext)
    : IPlatformLocaleNotificationQuery, IPlatformLocaleNotificationCommand, INotificationTemplateResolver
{

    public async Task<IReadOnlyList<LocaleListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.LocaleNotificationRecords
            .AsNoTracking()
            .OrderBy(e => e.NormalizedLocale)
            .Select(e => new LocaleListItem(e.NormalizedLocale, e.Locale))
            .ToListAsync(cancellationToken);
    }

    public async Task<LocaleNotificationRecord?> GetAsync(string normalizedLocale, CancellationToken cancellationToken = default)
    {
        return await dbContext.LocaleNotificationRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.NormalizedLocale == normalizedLocale, cancellationToken);
    }

    public async Task<LocaleNotificationRecord?> ResolveAsync(string? recipientLocale, CancellationToken cancellationToken = default)
    {
        string? effectiveLocale = NormalizeEffectiveLocale(recipientLocale);

        // Exact match
        LocaleNotificationRecord? record = await GetAsync(effectiveLocale, cancellationToken);
        if (record is not null) return record;

        // Neutral language fallback (e.g. "fa" from "fa-IR")
        if (effectiveLocale.Contains('-', StringComparison.Ordinal))
        {
            string neutral = effectiveLocale.Split('-')[0];
            record = await GetAsync(neutral, cancellationToken);
            if (record is not null) return record;
        }

        // Final fallback: fa-IR
        return await GetAsync("fa-IR", cancellationToken);
    }

    public async Task<LocaleNotificationCommandResult> CreateAsync(LocaleNotificationRecordWriteModel model, CancellationToken cancellationToken = default)
    {
        LocaleNotificationValidationFailure? validationError = LocaleNotificationRecordValidator.Validate(model);
        if (validationError is not null)
        {
            return LocaleNotificationCommandResult.Validation(validationError);
        }

        LocaleNotificationRecordValidator.TryNormalizeLocale(model.Locale, out string? normalizedLocale);

        bool exists = await dbContext.LocaleNotificationRecords
            .AnyAsync(e => e.NormalizedLocale == normalizedLocale, cancellationToken);
        if (exists)
        {
            return LocaleNotificationCommandResult.DuplicateLocale();
        }

        LocaleNotificationRecord record = new()
        {
            Id = Guid.NewGuid(),
            NormalizedLocale = normalizedLocale!,
            Locale = model.Locale,
            VerificationSmsTemplateId = model.VerificationSmsTemplateId,
            SeaReminderEmailSubject = model.SeaReminderEmailSubject,
            SeaReminderEmailTextBody = model.SeaReminderEmailTextBody,
            SeaReminderEmailHtmlBody = model.SeaReminderEmailHtmlBody,
            SeaReminderSmsTemplateId = model.SeaReminderSmsTemplateId,
            AppointmentReminderEmailSubject = model.AppointmentReminderEmailSubject,
            AppointmentReminderEmailTextBody = model.AppointmentReminderEmailTextBody,
            AppointmentReminderEmailHtmlBody = model.AppointmentReminderEmailHtmlBody,
            AppointmentReminderSmsTemplateId = model.AppointmentReminderSmsTemplateId,
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _ = dbContext.LocaleNotificationRecords.Add(record);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return LocaleNotificationCommandResult.Success();
    }

    public async Task<LocaleNotificationCommandResult> UpdateAsync(string normalizedLocale, LocaleNotificationRecordWriteModel model, long expectedVersion, CancellationToken cancellationToken = default)
    {
        LocaleNotificationValidationFailure? validationError = LocaleNotificationRecordValidator.Validate(model);
        if (validationError is not null)
        {
            return LocaleNotificationCommandResult.Validation(validationError);
        }

        LocaleNotificationRecordValidator.TryNormalizeLocale(model.Locale, out string? modelNormalizedLocale);

        LocaleNotificationRecord? existing = await dbContext.LocaleNotificationRecords
            .SingleOrDefaultAsync(e => e.NormalizedLocale == normalizedLocale, cancellationToken);
        if (existing is null)
        {
            return LocaleNotificationCommandResult.NotFound();
        }

        if (!string.Equals(existing.NormalizedLocale, modelNormalizedLocale, StringComparison.Ordinal))
        {
            return LocaleNotificationCommandResult.Validation(new LocaleNotificationValidationFailure(
                LocaleNotificationValidationRule.ImmutableLocale,
                LocaleNotificationRecordValidator.LocaleField));
        }

        if (existing.Version != expectedVersion)
        {
            return LocaleNotificationCommandResult.StaleVersion();
        }

        dbContext.LocaleNotificationRecords.Update(existing with
        {
            VerificationSmsTemplateId = model.VerificationSmsTemplateId,
            SeaReminderEmailSubject = model.SeaReminderEmailSubject,
            SeaReminderEmailTextBody = model.SeaReminderEmailTextBody,
            SeaReminderEmailHtmlBody = model.SeaReminderEmailHtmlBody,
            SeaReminderSmsTemplateId = model.SeaReminderSmsTemplateId,
            AppointmentReminderEmailSubject = model.AppointmentReminderEmailSubject,
            AppointmentReminderEmailTextBody = model.AppointmentReminderEmailTextBody,
            AppointmentReminderEmailHtmlBody = model.AppointmentReminderEmailHtmlBody,
            AppointmentReminderSmsTemplateId = model.AppointmentReminderSmsTemplateId,
            Version = existing.Version + 1,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return LocaleNotificationCommandResult.Success();
    }

    public async Task<ResolvedNotificationTemplate?> ResolveAsync(string? recipientLocale, NotificationPurpose purpose, CancellationToken cancellationToken = default)
    {
        LocaleNotificationRecord? record = await ResolveAsync(recipientLocale, cancellationToken);
        if (record is null) return null;

        return purpose switch
        {
            NotificationPurpose.VerificationCode => new ResolvedNotificationTemplate(
                purpose,
                EmailSubject: null,
                EmailTextBody: null,
                EmailHtmlBody: null,
                SmsTemplateId: record.VerificationSmsTemplateId,
                SmsParameters: new Dictionary<string, string>()),

            NotificationPurpose.SeaReminder => new ResolvedNotificationTemplate(
                purpose,
                EmailSubject: record.SeaReminderEmailSubject,
                EmailTextBody: record.SeaReminderEmailTextBody,
                EmailHtmlBody: record.SeaReminderEmailHtmlBody,
                SmsTemplateId: record.SeaReminderSmsTemplateId,
                SmsParameters: null),

            NotificationPurpose.AppointmentReminder => new ResolvedNotificationTemplate(
                purpose,
                EmailSubject: record.AppointmentReminderEmailSubject,
                EmailTextBody: record.AppointmentReminderEmailTextBody,
                EmailHtmlBody: record.AppointmentReminderEmailHtmlBody,
                SmsTemplateId: record.AppointmentReminderSmsTemplateId,
                SmsParameters: null),

            _ => null
        };
    }

    private static string NormalizeEffectiveLocale(string? recipientLocale)
    {
        if (string.IsNullOrWhiteSpace(recipientLocale))
        {
            return "fa-IR";
        }

        try
        {
            return new CultureInfo(recipientLocale).Name;
        }
        catch (CultureNotFoundException)
        {
            return "fa-IR";
        }
    }
}
