using Vitarish.Modules.Aggregator.Core.Platform.Errors;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

public interface ITaminCredentialSessionStore
{
    Task<TaminSessionResult> AuthenticateAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<TaminSessionResult> GetOrRefreshSessionAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);
}

public sealed record TaminSessionResult(
    Vorn.Tamin.TaminSession? Session,
    IntegrationError? Error,
    string? DoctorTaminId = null,
    string? DoctorNationalCode = null,
    string? ClinicSiamId = null)
{
    public bool IsSuccess => Session is not null && Error is null;

    public static TaminSessionResult Success(
        Vorn.Tamin.TaminSession session,
        string? doctorTaminId = null,
        string? doctorNationalCode = null,
        string? clinicSiamId = null) => new(session, null, doctorTaminId, doctorNationalCode, clinicSiamId);

    public static TaminSessionResult Failure(IntegrationError error) => new(null, error);
}
