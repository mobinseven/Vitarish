using Vitarish.Requests.Contracts;

namespace Vitarish.Requests.Client;

public interface IRequestsClient
{
    Task<RequestsClientResult<RequestIdentityPageResponse>> QueryRequestIdentitiesAsync(RequestIdentityQueryParameters parameters, CancellationToken cancellationToken);
}
