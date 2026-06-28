namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationReturnUrlStore { Task StoreAsync(string returnUrl, CancellationToken cancellationToken); Task<string?> ReadAsync(CancellationToken cancellationToken); }
