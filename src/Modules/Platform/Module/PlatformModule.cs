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

    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => endpoints.MapPlatformApi();

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
                SeaReminderEmailSubject = "یادآوری نوبت",
                SeaReminderEmailTextBody = "بیمار عزیز، نوبت شما برای {ProcedureName} با {ProjectName} در {EventTitle} تعیین شده است.",
                SeaReminderEmailHtmlBody = "<p>بیمار عزیز، نوبت شما برای <strong>{ProcedureName}</strong> با <strong>{ProjectName}</strong> در <strong>{EventTitle}</strong> تعیین شده است.</p>",
                SeaReminderSmsTemplateId = "100005",
                AppointmentReminderEmailSubject = "نوبت آینده",
                AppointmentReminderEmailTextBody = "نوبت شما در {OccurrenceStart} تعیین شده است.",
                AppointmentReminderEmailHtmlBody = "<p>نوبت شما در <strong>{OccurrenceStart}</strong> تعیین شده است.</p>",
                AppointmentReminderSmsTemplateId = "100006"
            });
        }
    }
}
