using Vitarish.Modules.Base.Core.Platform.Errors;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal interface ITaminCredentialSessionStore
{
    Task<TaminSessionResult> AuthenticateAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<TaminSessionResult> GetOrRefreshSessionAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<TaminAuthorizationStartResult> BeginAuthorizationAsync(
        Guid credentialId,
        CancellationToken cancellationToken = default);
    Task<TaminSessionResult> CompleteAuthorizationAsync(
        Guid credentialId,
        string authorizationCode,
        string state,
        CancellationToken cancellationToken = default);
}

internal sealed record TaminAuthorizationStartResult(Uri? AuthorizationUri, IntegrationError? Error)
{
    public bool IsSuccess => AuthorizationUri is not null && Error is null;
}

internal sealed record TaminSessionResult(
    TaminProviderSession? Session,
    IntegrationError? Error,
    string? DoctorTaminId = null,
    string? DoctorNationalCode = null,
    string? ClinicSiamId = null)
{
    public bool IsSuccess => Session is not null && Error is null;

    public static TaminSessionResult Success(
        TaminProviderSession session,
        string? doctorTaminId = null,
        string? doctorNationalCode = null,
        string? clinicSiamId = null) => new(session, null, doctorTaminId, doctorNationalCode, clinicSiamId);

    public static TaminSessionResult Failure(IntegrationError error) => new(null, error);
}
