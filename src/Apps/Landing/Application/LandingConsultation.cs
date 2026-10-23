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
/// The kinds of clinic are the self-service categories sign-up stores, so the answer carries over as is.
/// </summary>
public static class LandingConsultation
{
    public const string KindKey = "kind";

    public static LandingConsultationQuestion Kind(IReadOnlyList<string> categories) =>
        new(KindKey, 1, categories.Select(category => new LandingConsultationOption(category, [])).ToArray());

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

    /// <summary>The answer keys the page may carry into sign-up besides the kind: booking and pains.</summary>
    public static bool AreKnownAnswers(IReadOnlyList<string> answers) =>
        answers.All(answer => Booking.Options.Concat(Pains.Options).Any(option => option.Key == answer));
}
