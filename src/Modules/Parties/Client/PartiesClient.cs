using System.Net;
using Vitarish.Modules.Base.Client;
using Vitarish.Parties.Contracts.Errors;
using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Routes;
using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Parties.Client;

public sealed class PartiesClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), IPartiesClient
{
    public async Task<PartiesResult<IReadOnlyList<UserTenantDto>>> GetUserTenantsAsync(CancellationToken cancellationToken = default) => MapResult(await GetJsonAsync<IReadOnlyList<UserTenantDto>>(Route(PartiesApiRoutes.UserTenants), cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<OwnerReadDto>> GetOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) => MapResult(await GetJsonAsync<OwnerReadDto>(Route($"/owners/{ownerId}"), cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<IReadOnlyList<OwnerReadDto>>> GetOwnersAsync(CancellationToken cancellationToken = default) => MapResult(await GetJsonAsync<IReadOnlyList<OwnerReadDto>>(Route(PartiesApiRoutes.Owners), cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<OwnerReadDto>> CreateOwnerAsync(OwnerWriteDto request, CancellationToken cancellationToken = default) => MapResult(await PostJsonAsync<OwnerWriteDto, OwnerReadDto>(Route(PartiesApiRoutes.Owners), request, cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<OwnerReadDto>> UpdateOwnerAsync(OwnerWriteDto request, CancellationToken cancellationToken = default) => MapResult(await PutJsonAsync<OwnerWriteDto, OwnerReadDto>(Route($"/owners/{request.Id}"), request, cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<bool>> DeleteOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) => MapResult(await DeleteJsonAsync<bool>(Route($"/owners/{ownerId}"), cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<TenantReadDto>> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) => MapResult(await GetJsonAsync<TenantReadDto>(Route($"/tenants/{tenantId}"), cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<IReadOnlyList<TenantReadDto>>> GetTenantsAsync(CancellationToken cancellationToken = default) => MapResult(await GetJsonAsync<IReadOnlyList<TenantReadDto>>(Route(PartiesApiRoutes.Tenants), cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<IReadOnlyList<TenantReadDto>>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) => MapResult(await GetJsonAsync<IReadOnlyList<TenantReadDto>>(Route($"/owners/{ownerId}/tenants"), cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<TenantReadDto>> CreateTenantAsync(TenantWriteDto request, CancellationToken cancellationToken = default) => MapResult(await PostJsonAsync<TenantWriteDto, TenantReadDto>(Route(PartiesApiRoutes.Tenants), request, cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<TenantReadDto>> UpdateTenantAsync(TenantWriteDto request, CancellationToken cancellationToken = default) => MapResult(await PutJsonAsync<TenantWriteDto, TenantReadDto>(Route($"/tenants/{request.Id}"), request, cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<bool>> DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) => MapResult(await DeleteJsonAsync<bool>(Route($"/tenants/{tenantId}"), cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<TenantIndicatorReadDto>> GetTenantIndicatorAsync(Guid tenantIndicatorId, CancellationToken cancellationToken = default) => MapResult(await GetJsonAsync<TenantIndicatorReadDto>(Route($"/tenant-indicators/{tenantIndicatorId}"), cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<IReadOnlyList<TenantIndicatorReadDto>>> GetTenantIndicatorsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) => MapResult(await GetJsonAsync<IReadOnlyList<TenantIndicatorReadDto>>(Route($"/tenants/{tenantId}/indicators"), cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<TenantIndicatorReadDto>> CreateTenantIndicatorAsync(TenantIndicatorWriteDto request, CancellationToken cancellationToken = default) => MapResult(await PostJsonAsync<TenantIndicatorWriteDto, TenantIndicatorReadDto>(Route(PartiesApiRoutes.TenantIndicators), request, cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<TenantIndicatorReadDto>> UpdateTenantIndicatorAsync(TenantIndicatorWriteDto request, CancellationToken cancellationToken = default) => MapResult(await PutJsonAsync<TenantIndicatorWriteDto, TenantIndicatorReadDto>(Route($"/tenant-indicators/{request.Id}"), request, cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<bool>> DeleteTenantIndicatorAsync(Guid tenantIndicatorId, CancellationToken cancellationToken = default) => MapResult(await DeleteJsonAsync<bool>(Route($"/tenant-indicators/{tenantIndicatorId}"), cancellationToken).ConfigureAwait(false));
    public async Task<PartiesResult<TenantReadDto>> SetupTenantAsync(SetupTenantRequest request, CancellationToken cancellationToken = default) => MapResult(await PostJsonAsync<SetupTenantRequest, TenantReadDto>(Route(PartiesApiRoutes.SetupTenant), request, cancellationToken).ConfigureAwait(false));
    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);
    private static string Route(string relative) => PartiesApiRoutes.Base + relative;
    private static PartiesResult<T> MapResult<T>(ModuleApiClientResult<T> result) => result.IsSuccess ? PartiesResult<T>.Success(result.Value!) : PartiesResult<T>.Failed(MapFailure(result.Failure!));
    private static PartiesError MapFailure(ModuleApiClientFailure failure) => new(MapCode(failure), failure.Message);
    private static PartiesErrorCode MapCode(ModuleApiClientFailure failure) => failure.Code switch { nameof(HttpStatusCode.BadRequest) => PartiesErrorCode.Validation, nameof(HttpStatusCode.Unauthorized) => PartiesErrorCode.Unauthorized, nameof(HttpStatusCode.Forbidden) => PartiesErrorCode.Forbidden, nameof(HttpStatusCode.NotFound) => PartiesErrorCode.NotFound, nameof(HttpStatusCode.Conflict) => PartiesErrorCode.Conflict, _ => PartiesErrorCode.Unexpected };
}
