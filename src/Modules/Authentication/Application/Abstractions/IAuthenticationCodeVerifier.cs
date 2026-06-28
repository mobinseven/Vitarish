namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationCodeVerifier
{
    Task<bool> VerifyAsync(string userId, string tokenPurpose, string code, CancellationToken cancellationToken);
}
