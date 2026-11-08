using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationCentralDestinationPolicy
{
    bool Owns(string returnUrl);
}

public sealed class AuthenticationCentralDestinationPolicy : IAuthenticationCentralDestinationPolicy
{
    public bool Owns(string returnUrl)
    {
        if (!ReturnUrlPolicy.IsSafeLocal(returnUrl)) return false;
        string path = returnUrl.Split('?', '#')[0];
        return path == AuthenticationBrowserPaths.Continue || path == AuthenticationBrowserPaths.Account ||
            path.StartsWith(AuthenticationBrowserPaths.Account + "/", StringComparison.Ordinal);
    }
}
