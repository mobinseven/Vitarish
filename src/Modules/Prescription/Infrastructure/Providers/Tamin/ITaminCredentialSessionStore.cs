using Vitarish.Modules.Base.Platform.Errors;
using Vitarish.TaminInsurance.Client;

namespace Vitarish.Prescription.Infrastructure.Providers.Tamin;

public interface ITaminCredentialSessionStore
{
    Task<TaminSessionResult> AuthenticateAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<TaminSessionResult> GetOrRefreshSessionAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);
}

public sealed record TaminSessionResult(TaminSession? Session, IntegrationError? Error)
{
    public bool IsSuccess => Session is not null && Error is null;

    public static TaminSessionResult Success(TaminSession session) => new(session, null);
    public static TaminSessionResult Failure(IntegrationError error) => new(null, error);
}
