namespace Vitarish.Landing.Application;

public enum LandingFeatureStatus
{
    Live,
    InProgress,
    NotYet
}

/// <summary>A feature the landing page may mention, with the fact the assistant is allowed to state about it.</summary>
public sealed record LandingFeature(string Key, LandingFeatureStatus Status, string Fact);

/// <summary>
/// The only clinic features the landing page and its assistant may claim, checked against the clinic owner app.
/// Change a status here when the feature changes; the page and the assistant both read this list.
/// </summary>
public static class LandingFeatureCatalog
{
    public static IReadOnlyList<LandingFeature> Clinic { get; } =
    [
        new("online-booking", LandingFeatureStatus.Live, "Patients browse the clinic's shifts, book an open slot, and cancel when allowed."),
        new("reservations", LandingFeatureStatus.Live, "The owner sees and manages every reservation."),
        new("shifts-and-services", LandingFeatureStatus.Live, "Recurring shifts per provider, changes to a single day, services, service types and providers."),
        new("check-in-sms", LandingFeatureStatus.Live, "Patient check-in, with arrival confirmed by SMS."),
        new("reminders", LandingFeatureStatus.Live, "Reminders the clinic sets for patients and for its team."),
        new("patient-records", LandingFeatureStatus.Live, "Patient records: find by national ID, advanced search, find or create."),
        new("care-timeline", LandingFeatureStatus.Live, "Each patient's procedures and visits as one timeline of events."),
        new("dictated-notes", LandingFeatureStatus.Live, "Clinical notes dictated in Persian or English and turned into text; audio is never stored."),
        new("clinical-templates", LandingFeatureStatus.Live, "Prescription and procedure templates, surgeries and surgery types."),
        new("staff-roles", LandingFeatureStatus.Live, "Staff users and roles that control who sees and does what."),
        new("audit-log", LandingFeatureStatus.Live, "An audit log of who changed what, and when."),
        new("clinic-address", LandingFeatureStatus.Live, "The clinic's own web address, with one shared sign-in for all of a person's clinics."),
        new("bilingual", LandingFeatureStatus.Live, "Persian and English, with right-to-left layout throughout."),
        new("tamin-insurance", LandingFeatureStatus.InProgress, "Connecting a Tamin insurance account works today; the full electronic prescription flow is still being built."),
        new("clinic-statistics", LandingFeatureStatus.NotYet, "Clinic statistics and reports are not available yet.")
    ];

    public static IReadOnlyList<string> Keys { get; } = Clinic.Select(feature => feature.Key).ToArray();
}

/// <summary>The next steps the landing assistant may offer; the page decides what each one does.</summary>
public static class LandingNextActions
{
    public const string OpenClinic = "open-clinic";
    public const string TalkToTeam = "talk-to-team";
    public const string TryVoice = "try-voice";

    public static IReadOnlyList<string> All { get; } = [OpenClinic, TalkToTeam, TryVoice];
}
