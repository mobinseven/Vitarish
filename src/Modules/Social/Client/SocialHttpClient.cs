using System.Net;
using Vitarish.Modules.Base.Client;
using Vitarish.Social.Contracts;

namespace Vitarish.Social.Client;

public sealed class SocialHttpClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), ISocialClient
{
    public async Task<SocialQueryResult<IReadOnlyList<SocialConnectionResponse>>> GetConnectionsAsync(CancellationToken cancellationToken) =>
        MapQuery(await GetJsonAsync<IReadOnlyList<SocialConnectionResponse>>(SocialApiRoutes.Base + SocialApiRoutes.Connections, cancellationToken).ConfigureAwait(false));

    public async Task<SocialQueryResult<SocialWebhookEventResponse?>> GetLastWebhookAsync(Guid connectionId, CancellationToken cancellationToken) =>
        MapQuery(await GetJsonAsync<SocialWebhookEventResponse?>(SocialApiRoutes.Base + SocialApiRoutes.LatestWebhookForConnectionPath(connectionId), cancellationToken).ConfigureAwait(false));

    public async Task<SocialReplayWebhookResponse> ReplayAsync(Guid rawWebhookEventId, CancellationToken cancellationToken)
    {
        ModuleApiClientResult<SocialReplayWebhookResponse> result = await PostJsonAsync<object, SocialReplayWebhookResponse>(
            SocialApiRoutes.Base + SocialApiRoutes.ReplayWebhookPath(rawWebhookEventId),
            new { },
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? result.Value ?? SocialReplayWebhookResponse.Queued()
            : SocialReplayWebhookResponse.Failed(ToReplayStatus(result.Failure!), ToError(result.Failure!));
    }

    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);

    private static SocialQueryResult<T> MapQuery<T>(ModuleApiClientResult<T> result) =>
        result.IsSuccess ? SocialQueryResult<T>.Success(result.Value!) : SocialQueryResult<T>.Failed(ToError(result.Failure!));

    private static SocialErrorResponse ToError(ModuleApiClientFailure failure) => new(ToErrorKind(failure), failure.Message, failure.StatusCode);

    private static SocialReplayWebhookStatus ToReplayStatus(ModuleApiClientFailure failure) => ToErrorKind(failure) switch
    {
        SocialErrorKind.Validation => SocialReplayWebhookStatus.ValidationFailure,
        SocialErrorKind.Unauthorized => SocialReplayWebhookStatus.Unauthorized,
        SocialErrorKind.Forbidden => SocialReplayWebhookStatus.Forbidden,
        SocialErrorKind.NotFound => SocialReplayWebhookStatus.NotFound,
        SocialErrorKind.Conflict => SocialReplayWebhookStatus.Conflict,
        SocialErrorKind.Timeout => SocialReplayWebhookStatus.Timeout,
        SocialErrorKind.Cancellation => SocialReplayWebhookStatus.Cancellation,
        SocialErrorKind.Transport => SocialReplayWebhookStatus.TransportFailure,
        SocialErrorKind.Deserialization => SocialReplayWebhookStatus.DeserializationFailure,
        _ => SocialReplayWebhookStatus.UnexpectedFailure,
    };

    private static SocialErrorKind ToErrorKind(ModuleApiClientFailure failure) => failure.Code switch
    {
        "Canceled" => SocialErrorKind.Cancellation,
        "Timeout" => SocialErrorKind.Timeout,
        "Transport" => SocialErrorKind.Transport,
        "Deserialization" => SocialErrorKind.Deserialization,
        nameof(HttpStatusCode.BadRequest) => SocialErrorKind.Validation,
        nameof(HttpStatusCode.Unauthorized) => SocialErrorKind.Unauthorized,
        nameof(HttpStatusCode.Forbidden) => SocialErrorKind.Forbidden,
        nameof(HttpStatusCode.NotFound) => SocialErrorKind.NotFound,
        nameof(HttpStatusCode.Conflict) => SocialErrorKind.Conflict,
        nameof(HttpStatusCode.GatewayTimeout) => SocialErrorKind.Timeout,
        _ => SocialErrorKind.UnexpectedFailure,
    };
}
