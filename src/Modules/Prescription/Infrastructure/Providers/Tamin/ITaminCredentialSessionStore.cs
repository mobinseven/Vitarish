using Vitarish.Modules.Base.Platform.Errors;

namespace Vitarish.Prescription.Infrastructure.Providers.Tamin;

public interface ITaminCredentialSessionStore
{
    Task<TaminSessionResult> AuthenticateAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<TaminSessionResult> GetOrRefreshSessionAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);
}

public sealed record TaminSessionResult(
    Vorn.Tamin.TaminSession? Session,
    IntegrationError? Error,
    string? DoctorTaminId = null,
    string? DoctorNationalCode = null)
{
    public bool IsSuccess => Session is not null && Error is null;

    public static TaminSessionResult Success(
        Vorn.Tamin.TaminSession session,
        string? doctorTaminId = null,
        string? doctorNationalCode = null) => new(session, null, doctorTaminId, doctorNationalCode);

    public static TaminSessionResult Failure(IntegrationError error) => new(null, error);
}
