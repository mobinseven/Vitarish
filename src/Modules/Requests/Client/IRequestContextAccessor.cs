using Vitarish.Requests.Contracts;

namespace Vitarish.Requests.Client;

public interface IRequestContextAccessor
{
    RequestContextResult GetCurrent();
}
