using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Application.Validation;

namespace Vitarish.Platform.Tests.Application;

public sealed class LocaleNotificationRecordValidatorTests
{
    [Fact]
    public void Validate_returns_missing_placeholder_rule_from_application_definitions()
    {
        LocaleNotificationRecordWriteModel model = ValidModel() with
        {
            SeaReminderEmailTextBody = "Only {EventTitle} and {ProcedureName}."
        };

        LocaleNotificationValidationFailure? failure = LocaleNotificationRecordValidator.Validate(model);

        failure.Should().BeEquivalentTo(new LocaleNotificationValidationFailure(
            LocaleNotificationValidationRule.MissingPlaceholder,
            LocaleNotificationRecordValidator.SeaReminderEmailTextBodyField,
            "{ProjectName}"));
    }

    [Fact]
    public void GetRequirements_returns_same_placeholder_definitions_used_by_validation()
    {
        IReadOnlyList<LocaleNotificationRequirement> requirements = LocaleNotificationRecordValidator.GetRequirements();

        requirements.Should().ContainEquivalentOf(new LocaleNotificationRequirement(
            LocaleNotificationPurpose.SeaReminder,
            [LocaleNotificationRecordValidator.SeaReminderEmailTextBodyField, LocaleNotificationRecordValidator.SeaReminderEmailHtmlBodyField],
            ["{EventTitle}", "{ProcedureName}", "{ProjectName}"],
            ["{EventTitle}", "{ProcedureName}", "{ProjectName}"]));
    }

    [Theory]
    [InlineData("en-US", "en-US")]
    [InlineData("fa-IR", "fa-IR")]
    public void ValidateNormalizedLocale_accepts_canonical_culture(string locale, string expected)
    {
        LocaleNotificationValidationFailure? failure = LocaleNotificationRecordValidator.ValidateNormalizedLocale(locale, out string? normalized);

        failure.Should().BeNull();
        normalized.Should().Be(expected);
    }

    private static LocaleNotificationRecordWriteModel ValidModel() => new(
        "en-US",
        "100001",
        "Appointment Reminder",
        "{EventTitle} {ProcedureName} {ProjectName}",
        "<p>{EventTitle} {ProcedureName} {ProjectName}</p>",
        "100002",
        "Upcoming Appointment",
        "{OccurrenceStart}",
        "<p>{OccurrenceStart}</p>",
        "100003");
}
