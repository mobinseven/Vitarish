namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationCodeGenerator
{
    Task<string> GenerateAsync(string userId, string tokenPurpose, CancellationToken cancellationToken);
}
