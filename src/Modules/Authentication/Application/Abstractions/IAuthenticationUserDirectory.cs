namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationUserDirectory { Task<AuthenticationUserDescriptor?> FindByLoginAsync(string login, CancellationToken cancellationToken); Task<AuthenticationUserDescriptor?> GetCurrentAsync(CancellationToken cancellationToken); }
public sealed record AuthenticationUserDescriptor(string UserId, string? DisplayName, string? Email, string? PhoneNumber, bool RequiresProfileCompletion);
