using System.Net;
using Vitarish.Modules.Base.Client;
using Vitarish.Parties.Contracts.Errors;
using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Routes;
using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Parties.Client;

public sealed class PartiesClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), IPartiesClient
{
    public async Task<PartiesResult<TenantReadDto>> CreateTenantAsync(TenantWriteDto request, CancellationToken cancellationToken = default) =>
        MapResult(await PostJsonAsync<TenantWriteDto, TenantReadDto>(PartiesApiRoutes.Base + PartiesApiRoutes.Tenants, request, cancellationToken).ConfigureAwait(false));

    public async Task<PartiesResult<TenantReadDto>> UpdateTenantAsync(TenantWriteDto request, CancellationToken cancellationToken = default) =>
        MapResult(await PutJsonAsync<TenantWriteDto, TenantReadDto>(PartiesApiRoutes.Base + $"/tenants/{request.Id}", request, cancellationToken).ConfigureAwait(false));

    public async Task<PartiesResult<TenantIndicatorReadDto>> CreateTenantIndicatorAsync(TenantIndicatorWriteDto request, CancellationToken cancellationToken = default) =>
        MapResult(await PostJsonAsync<TenantIndicatorWriteDto, TenantIndicatorReadDto>(PartiesApiRoutes.Base + PartiesApiRoutes.TenantIndicators, request, cancellationToken).ConfigureAwait(false));

    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) =>
        new(statusCode.ToString(), message, statusCodeValue);

    private static PartiesResult<T> MapResult<T>(ModuleApiClientResult<T> result) =>
        result.IsSuccess
            ? PartiesResult<T>.Success(result.Value!)
            : PartiesResult<T>.Failed(MapFailure(result.Failure!));

    private static PartiesError MapFailure(ModuleApiClientFailure failure) =>
        new(MapCode(failure), failure.Message);

    private static PartiesErrorCode MapCode(ModuleApiClientFailure failure) => failure.Code switch
    {
        nameof(HttpStatusCode.BadRequest) => PartiesErrorCode.Validation,
        nameof(HttpStatusCode.Unauthorized) => PartiesErrorCode.Unauthorized,
        nameof(HttpStatusCode.Forbidden) => PartiesErrorCode.Forbidden,
        nameof(HttpStatusCode.NotFound) => PartiesErrorCode.NotFound,
        nameof(HttpStatusCode.Conflict) => PartiesErrorCode.Conflict,
        _ => PartiesErrorCode.Unexpected
    };
}
