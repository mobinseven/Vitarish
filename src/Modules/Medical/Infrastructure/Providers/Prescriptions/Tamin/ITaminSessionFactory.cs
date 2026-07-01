using Vorn.Tamin;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal interface ITaminSessionFactory
{
    TaminSession CreateTokenSession(string oauthToken, bool needToken = true);
    TaminSession CreateReferenceDataSession();
    Task<TaminSession> CreatePasswordSessionAsync(string username, string password, CancellationToken cancellationToken);
    AuthClient CreateAuthClient();
}
