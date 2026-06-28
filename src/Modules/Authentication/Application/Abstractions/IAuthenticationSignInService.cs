namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationSignInService { Task<bool> PasswordSignInAsync(string login, string password, bool rememberMe, CancellationToken cancellationToken); Task SignInAsync(string userId, bool rememberMe, CancellationToken cancellationToken); Task SignOutAsync(CancellationToken cancellationToken); }
