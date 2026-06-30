namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationUserDirectory
{
    Task<AuthenticationUserDescriptor?> FindByLoginAsync(string login, CancellationToken cancellationToken);
    Task<AuthenticationUserDescriptor?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<AuthenticationUserDescriptor> FindOrCreateByEmailAsync(string email, CancellationToken cancellationToken);
    Task<AuthenticationUserDescriptor?> FindByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken);
    Task<AuthenticationUserDescriptor> FindOrCreateByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken);
    Task<AuthenticationUserDescriptor?> GetCurrentAsync(CancellationToken cancellationToken);
}

public sealed record AuthenticationUserDescriptor(
    string UserId,
    string? UserName,
    string? DisplayName,
    string? GivenName,
    string? FamilyName,
    string? Email,
    string? PhoneNumber,
    bool RequiresProfileCompletion);
