namespace Vitarish.Landing.Application;

/// <summary>The kind of working example the page shows for a pain.</summary>
public enum LandingProofKind
{
    NoShowCost,
    BookingPreview,
    PatientTimeline,
    Dictation,
    ShiftWeek,
    InProgress
}

/// <summary>An answer the owner can tap, the features it points to, and the example it leads to.</summary>
public sealed record LandingConsultationOption(string Key, IReadOnlyList<string> FeatureKeys, LandingProofKind? Proof = null);

public sealed record LandingConsultationQuestion(string Key, int MaxSelections, IReadOnlyList<LandingConsultationOption> Options);

/// <summary>
/// The guided consultation's questions and their answer keys. The page localizes them by key, the
/// saved setup draft stores these keys, and each pain maps to features from <see cref="LandingFeatureCatalog"/>.
/// </summary>
public static class LandingConsultation
{
    public static LandingConsultationQuestion Kind { get; } = new("kind", 1,
    [
        new("dental", []),
        new("dermatology", []),
        new("physiotherapy", []),
        new("general-practice", []),
        new("psychology", [])
    ]);

    public static LandingConsultationQuestion Booking { get; } = new("booking", 1,
    [
        new("phone", []),
        new("messaging", []),
        new("walk-in", []),
        new("other-software", [])
    ]);

    public static LandingConsultationQuestion Pains { get; } = new("pains", 3,
    [
        new("no-shows", ["reminders", "check-in-sms"], LandingProofKind.NoShowCost),
        new("phone-load", ["online-booking", "reservations"], LandingProofKind.BookingPreview),
        new("patient-history", ["patient-records", "care-timeline"], LandingProofKind.PatientTimeline),
        new("after-hours-notes", ["dictated-notes", "clinical-templates"], LandingProofKind.Dictation),
        new("shifts-and-roles", ["shifts-and-services", "staff-roles"], LandingProofKind.ShiftWeek),
        new("insurance", ["tamin-insurance"], LandingProofKind.InProgress)
    ]);

    public static IReadOnlyList<LandingConsultationQuestion> Questions { get; } = [Kind, Booking, Pains];
}
