using System.Linq.Expressions;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Clinic.Infrastructure.Data;

namespace Vitarish.Clinic.Infrastructure.Handlers;

/// <summary>
/// Owns which surgeries count as a patient's last and next surgery, for patient sorting and display.
/// The last surgery is the latest non-cancelled one that already started; the next is the earliest planned one still ahead.
/// </summary>
internal static class PatientSurgeryDates
{
    public static IQueryable<SurgeryView> LastCandidates(ClinicDbContext dbContext, DateTimeOffset now)
        => dbContext.SurgeryViews.Where(surgery => surgery.Status != nameof(SurgeryStatus.Cancelled) && surgery.ScheduledAt < now);

    public static IQueryable<SurgeryView> NextCandidates(ClinicDbContext dbContext, DateTimeOffset now)
        => dbContext.SurgeryViews.Where(surgery => surgery.Status == nameof(SurgeryStatus.Planned) && surgery.ScheduledAt >= now);

    /// <summary>A patient's last surgery start, as a key that sorts in the database.</summary>
    public static Expression<Func<PatientView, DateTimeOffset?>> LastKey(ClinicDbContext dbContext, DateTimeOffset now)
    {
        IQueryable<SurgeryView> candidates = LastCandidates(dbContext, now);
        return patient => candidates.Where(surgery => surgery.PatientId == patient.Id).Max(surgery => (DateTimeOffset?)surgery.ScheduledAt);
    }

    /// <summary>A patient's next surgery start, as a key that sorts in the database.</summary>
    public static Expression<Func<PatientView, DateTimeOffset?>> NextKey(ClinicDbContext dbContext, DateTimeOffset now)
    {
        IQueryable<SurgeryView> candidates = NextCandidates(dbContext, now);
        return patient => candidates.Where(surgery => surgery.PatientId == patient.Id).Min(surgery => (DateTimeOffset?)surgery.ScheduledAt);
    }
}
