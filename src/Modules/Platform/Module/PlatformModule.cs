using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Platform.Api;
using Vitarish.Platform.Domain.Entities;
using Vitarish.Platform.Infrastructure.Data;
using Vitarish.Platform.Infrastructure.DependencyInjection;
using Vitarish.Platform.Infrastructure.Services;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Platform.Module;

public sealed class PlatformModule : Modules.Base.Core.Module
{
    private const int ThemeSelectionId = 1;
    public override string Name => "Platform";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.AddPlatform(configureDbContext ?? (_ => { }));
        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<PlatformDbContext>();

        using IServiceScope scope = app.Services.CreateScope();
        PlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        TimeProvider timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        DateTimeOffset now = timeProvider.GetUtcNow();

        bool hasDefault = await dbContext.Themes.AnyAsync(e => e.Id == DefaultPlatformTheme.Id);
        if (!hasDefault)
        {
            _ = dbContext.Themes.Add(DefaultPlatformTheme.Create());
        }

        ThemeSelection? selection = await dbContext.ThemeSelections.SingleOrDefaultAsync(e => e.Id == ThemeSelectionId);
        if (selection is null)
        {
            _ = dbContext.ThemeSelections.Add(new ThemeSelection
            {
                Id = ThemeSelectionId,
                ActiveThemeId = DefaultPlatformTheme.Id,
                UpdatedAt = now
            });
        }

        await SeedLocaleNotificationRecordsAsync(dbContext);

        _ = await dbContext.SaveChangesAsync();

        _ = app.MapPlatformApi();
    }

    private static async Task SeedLocaleNotificationRecordsAsync(PlatformDbContext dbContext)
    {
        bool hasEnUs = await dbContext.LocaleNotificationRecords.AnyAsync(e => e.NormalizedLocale == "en-US");
        if (!hasEnUs)
        {
            _ = dbContext.LocaleNotificationRecords.Add(new LocaleNotificationRecord
            {
                NormalizedLocale = "en-US",
                Locale = "en-US",
                VerificationSmsTemplateId = "100001",
                SeaReminderEmailSubject = "Appointment Reminder",
                SeaReminderEmailTextBody = "Dear patient, your appointment for {ProcedureName} with {ProjectName} is scheduled for {EventTitle}.",
                SeaReminderEmailHtmlBody = "<p>Dear patient, your appointment for <strong>{ProcedureName}</strong> with <strong>{ProjectName}</strong> is scheduled for <strong>{EventTitle}</strong>.</p>",
                SeaReminderSmsTemplateId = "100002",
                AppointmentReminderEmailSubject = "Upcoming Appointment",
                AppointmentReminderEmailTextBody = "Your appointment is scheduled for {OccurrenceStart}.",
                AppointmentReminderEmailHtmlBody = "<p>Your appointment is scheduled for <strong>{OccurrenceStart}</strong>.</p>",
                AppointmentReminderSmsTemplateId = "100003"
            });
        }

        bool hasFaIr = await dbContext.LocaleNotificationRecords.AnyAsync(e => e.NormalizedLocale == "fa-IR");
        if (!hasFaIr)
        {
            _ = dbContext.LocaleNotificationRecords.Add(new LocaleNotificationRecord
            {
                NormalizedLocale = "fa-IR",
                Locale = "fa-IR",
                VerificationSmsTemplateId = "100004",
                SeaReminderEmailSubject = "\u0622\u063a\u0627\u0632 \u0627\u0646\u062f\u0627\u0632\u0647",
                SeaReminderEmailTextBody = "\u0628\u0628\u0645\u0628\u0646\u062f \u063a\u0631\u06cc\u0645\u0647\u060c \u0646\u0628\u0634 \u0634\u0645\u0627 \u0628\u0631\u0627\u06cc {ProcedureName} \u0628\u0627 {ProjectName} \u0628\u0631 {EventTitle} \u062a\u0648\u0636\u0639 \u0634\u062f\u0647 \u0627\u0633\u062a.",
                SeaReminderEmailHtmlBody = "<p>\u0628\u0628\u0645\u0628\u0646\u062f \u063a\u0631\u06cc\u0645\u0647\u060c \u0646\u0628\u0634 \u0634\u0645\u0627 \u0628\u0631\u0627\u06cc <strong>{ProcedureName}</strong> \u0628\u0627 <strong>{ProjectName}</strong> \u0628\u0631 <strong>{EventTitle}</strong> \u062a\u0648\u0636\u0639 \u0634\u062f\u0647 \u0627\u0633\u062a.</p>",
                SeaReminderSmsTemplateId = "100005",
                AppointmentReminderEmailSubject = "\u0646\u0628\u0634 \u0642\u0631\u0628\u0627\u0628\u06cc",
                AppointmentReminderEmailTextBody = "\u0646\u0628\u0634 \u0634\u0645\u0627 \u0628\u0631 {OccurrenceStart} \u062a\u0648\u0636\u0639 \u0634\u062f\u0647 \u0627\u0633\u062a.",
                AppointmentReminderEmailHtmlBody = "<p>\u0646\u0628\u0634 \u0634\u0645\u0627 \u0628\u0631 <strong>{OccurrenceStart}</strong> \u062a\u0648\u0636\u0639 \u0634\u062f\u0647 \u0627\u0633\u062a.</p>",
                AppointmentReminderSmsTemplateId = "100006"
            });
        }
    }
}
