using System.Security.Claims;
using Vitarish.Requests.Contracts;

namespace Vitarish.Requests.Client;

public interface IRequestContextResolver
{
    RequestContextResult Resolve(ClaimsPrincipal principal);
}
