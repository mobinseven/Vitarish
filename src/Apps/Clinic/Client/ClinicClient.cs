using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Vitarish.Clinic.Contracts.Errors;
using Vitarish.Clinic.Contracts.Responses;
using Vitarish.Clinic.Contracts.Results;
using Vitarish.Clinic.Contracts.Routes;
using Vitarish.Modules.Base.Client;

namespace Vitarish.Clinic.Client;

public sealed class ClinicClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), IClinicClient
{
    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);

    private static ClinicError Error(ClinicErrorCode code, string message) => new(code, message);

    private static async Task<ClinicError> MapErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        ClinicErrorCode code = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => ClinicErrorCode.ValidationFailure,
            HttpStatusCode.Unauthorized => ClinicErrorCode.Unauthorized,
            HttpStatusCode.Forbidden => ClinicErrorCode.Forbidden,
            HttpStatusCode.NotFound => ClinicErrorCode.NotFound,
            HttpStatusCode.Conflict => ClinicErrorCode.Conflict,
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => ClinicErrorCode.Timeout,
            _ => ClinicErrorCode.UnexpectedFailure
        };

        try
        {
            return await response.Content.ReadFromJsonAsync<ClinicError>(ct).ConfigureAwait(false)
                ?? Error(code, $"Clinic API returned {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
        catch (Exception ex)
        {
            return Error(code, $"Clinic API returned {(int)response.StatusCode} {response.ReasonPhrase}. Deserialization failed: {ex.Message}");
        }
    }

    private static ClinicError MapTransportFailure(ModuleApiClientFailure? failure) => failure?.Code switch
    {
        "Canceled" => Error(ClinicErrorCode.Cancellation, failure.Message),
        "Timeout" => Error(ClinicErrorCode.Timeout, failure.Message),
        null => Error(ClinicErrorCode.TransportFailure, "Clinic API request failed."),
        _ => Error(ClinicErrorCode.TransportFailure, failure.Message)
    };

    private async Task<ClinicResult<T>> SendAsync<T>(HttpMethod method, string route, object? body, CancellationToken ct)
    {
        try
        {
            using HttpRequestMessage request = new(method, ClinicApiRoutes.Base + route);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            ModuleApiClientResult<HttpResponseMessage> sent = await SendRawAsync(request, ct).ConfigureAwait(false);
            if (!sent.IsSuccess)
            {
                return ClinicResult<T>.Failure(MapTransportFailure(sent.Failure));
            }

            using HttpResponseMessage response = sent.Value!;
            if (!response.IsSuccessStatusCode)
            {
                return ClinicResult<T>.Failure(await MapErrorAsync(response, ct).ConfigureAwait(false));
            }

            T? value = await response.Content.ReadFromJsonAsync<T>(ct).ConfigureAwait(false);
            return value is null
                ? ClinicResult<T>.Failure(Error(ClinicErrorCode.DeserializationFailure, "The Clinic API returned an empty response."))
                : ClinicResult<T>.Success(value);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ClinicResult<T>.Failure(Error(ClinicErrorCode.Cancellation, "The Clinic request was cancelled by the caller."));
        }
        catch (TaskCanceledException ex)
        {
            return ClinicResult<T>.Failure(Error(ClinicErrorCode.Timeout, ex.Message));
        }
        catch (JsonException ex)
        {
            return ClinicResult<T>.Failure(Error(ClinicErrorCode.DeserializationFailure, ex.Message));
        }
        catch (HttpRequestException ex)
        {
            return ClinicResult<T>.Failure(Error(ClinicErrorCode.TransportFailure, ex.Message));
        }
        catch (Exception ex)
        {
            return ClinicResult<T>.Failure(Error(ClinicErrorCode.UnexpectedFailure, ex.Message));
        }
    }

    public Task<ClinicResult<IReadOnlyList<PatientViewDto>>> ListPatientsAsync(PatientListRequest request, CancellationToken ct) => SendAsync<IReadOnlyList<PatientViewDto>>(HttpMethod.Post, ClinicApiRoutes.Patients, request, ct);

    public Task<ClinicResult<int>> CountPatientsAsync(PatientCountRequest request, CancellationToken ct) => SendAsync<int>(HttpMethod.Post, ClinicApiRoutes.PatientCount, request, ct);

    public Task<ClinicResult<IReadOnlyList<PatientViewDto>>> SearchPatientsAsync(PatientSearchRequest request, CancellationToken ct) => SendAsync<IReadOnlyList<PatientViewDto>>(HttpMethod.Post, ClinicApiRoutes.PatientSearch, request, ct);

    public Task<ClinicResult<PatientViewDto>> LookupPatientByIdentificationCodeAsync(PatientIdentificationLookupRequest request, CancellationToken ct) => SendAsync<PatientViewDto>(HttpMethod.Post, ClinicApiRoutes.PatientIdentificationLookup, request, ct);

    public Task<ClinicResult<PatientViewDto>> FindOrCreatePatientAsync(PatientFindOrCreateRequest request, CancellationToken ct) => SendAsync<PatientViewDto>(HttpMethod.Post, ClinicApiRoutes.PatientFindOrCreate, request, ct);

    public Task<ClinicResult<PatientViewDto>> GetPatientDetailsAsync(Guid patientId, CancellationToken ct) => SendAsync<PatientViewDto>(HttpMethod.Get, $"/patients/{patientId}", null, ct);
}
