using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Vitarish.Parties.Contracts.Errors;
using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Routes;
using Vitarish.Parties.Contracts.Tenants;
using Vitarish.Modules.Base.Client;

namespace Vitarish.Parties.Client;
public sealed class PartiesClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), IPartiesClient
{
    public Task<PartiesResult<TenantReadDto>> CreateTenantAsync(TenantWriteDto request, CancellationToken cancellationToken = default) => SendAsync<TenantWriteDto,TenantReadDto>(HttpMethod.Post, PartiesApiRoutes.Base + PartiesApiRoutes.Tenants, request, cancellationToken);
    public Task<PartiesResult<TenantReadDto>> UpdateTenantAsync(TenantWriteDto request, CancellationToken cancellationToken = default) => SendAsync<TenantWriteDto,TenantReadDto>(HttpMethod.Put, PartiesApiRoutes.Base + $"/tenants/{request.Id}", request, cancellationToken);
    public Task<PartiesResult<TenantIndicatorReadDto>> CreateTenantIndicatorAsync(TenantIndicatorWriteDto request, CancellationToken cancellationToken = default) => SendAsync<TenantIndicatorWriteDto,TenantIndicatorReadDto>(HttpMethod.Post, PartiesApiRoutes.Base + PartiesApiRoutes.TenantIndicators, request, cancellationToken);
    private async Task<PartiesResult<TResponse>> SendAsync<TRequest,TResponse>(HttpMethod method,string uri,TRequest request,CancellationToken cancellationToken)
    {
        try { using HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(method, uri){ Content = JsonContent.Create(request)}, cancellationToken); if(response.IsSuccessStatusCode){ TResponse? value=await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken); return value is null ? PartiesResult<TResponse>.Failed(new(PartiesErrorCode.Unexpected,"Parties API returned an empty response.")) : PartiesResult<TResponse>.Success(value);} return PartiesResult<TResponse>.Failed(await CreateErrorAsync(response,cancellationToken)); }
        catch(OperationCanceledException) { return PartiesResult<TResponse>.Failed(new(PartiesErrorCode.Unexpected, cancellationToken.IsCancellationRequested ? "Parties request was canceled." : "Parties request timed out.")); }
        catch(Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException) { return PartiesResult<TResponse>.Failed(new(PartiesErrorCode.Unexpected, ex.Message)); }
    }
    private static async Task<PartiesError> CreateErrorAsync(HttpResponseMessage response,CancellationToken cancellationToken)
    { try { PartiesError? error=await response.Content.ReadFromJsonAsync<PartiesError>(cancellationToken); if(error is not null) return error; } catch(Exception ex) when (ex is JsonException or NotSupportedException) { } PartiesErrorCode code=response.StatusCode switch { HttpStatusCode.BadRequest=>PartiesErrorCode.Validation, HttpStatusCode.Unauthorized=>PartiesErrorCode.Unauthorized, HttpStatusCode.Forbidden=>PartiesErrorCode.Forbidden, HttpStatusCode.NotFound=>PartiesErrorCode.NotFound, HttpStatusCode.Conflict=>PartiesErrorCode.Conflict, _=>PartiesErrorCode.Unexpected}; return new(code,"Parties API request failed."); }
    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);

}
