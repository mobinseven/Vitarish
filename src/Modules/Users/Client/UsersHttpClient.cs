using System.Globalization;
using System.Net;
using Vitarish.Modules.Base.Client;
using Vitarish.Users.Contracts.Requests;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Contracts.Routes;

namespace Vitarish.Users.Client;

public sealed class UsersHttpClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), IUsersClient
{
    public Task<UsersClientResult<IReadOnlyList<UserSummaryResponse>>> SearchUsersAsync(SearchUsersQueryParameters parameters, CancellationToken cancellationToken) =>
        Get<IReadOnlyList<UserSummaryResponse>>(BuildSearchUri(parameters), cancellationToken);

    public Task<UsersClientResult<UserSummaryResponse>> GetUserAsync(string userId, CancellationToken cancellationToken) =>
        Get<UserSummaryResponse>(UsersApiRoutes.Base + UsersApiRoutes.ById.Replace("{userId}", Uri.EscapeDataString(userId)), cancellationToken);

    public Task<UsersClientResult<UserSummaryResponse>> CreateUserWithPhoneAsync(CreateUserWithPhoneRequest request, CancellationToken cancellationToken) =>
        Post<CreateUserWithPhoneRequest, UserSummaryResponse>(UsersApiRoutes.CreateWithPhone, request, cancellationToken);

    public Task<UsersClientResult<UserSummaryResponse>> CreateUserWithEmailAsync(CreateUserWithEmailRequest request, CancellationToken cancellationToken) =>
        Post<CreateUserWithEmailRequest, UserSummaryResponse>(UsersApiRoutes.CreateWithEmail, request, cancellationToken);

    public Task<UsersClientResult<UserExistenceResponse>> CheckUserPhoneNumberInUseAsync(CheckUserPhoneNumberInUseQueryParameters parameters, CancellationToken cancellationToken) =>
        Get<UserExistenceResponse>(BuildPhoneUri(parameters), cancellationToken);

    public Task<UsersClientResult<UserExistenceResponse>> CheckUserEmailInUseAsync(CheckUserEmailInUseQueryParameters parameters, CancellationToken cancellationToken) =>
        Get<UserExistenceResponse>(BuildEmailUri(parameters), cancellationToken);

    public Task<UsersClientResult<UsersCommandResponse>> SetUserEmailAsync(string userId, SetUserEmailRequest request, CancellationToken cancellationToken) =>
        Put<SetUserEmailRequest, UsersCommandResponse>(UsersApiRoutes.SetEmail.Replace("{userId}", Uri.EscapeDataString(userId)), request, cancellationToken);

    public Task<UsersClientResult<UserRoleCheckResponse>> CheckUserRoleAsync(CheckUserRoleQueryParameters parameters, CancellationToken cancellationToken) =>
        Get<UserRoleCheckResponse>(BuildRoleUri(parameters), cancellationToken);

    public Task<UsersClientResult<UsersCommandResponse>> AddUserToRoleAsync(string userId, AddUserToRoleRequest request, CancellationToken cancellationToken) =>
        Post<AddUserToRoleRequest, UsersCommandResponse>(UsersApiRoutes.ById.Replace("{userId}", Uri.EscapeDataString(userId)) + "/roles", request, cancellationToken);

    private async Task<UsersClientResult<T>> Get<T>(string requestUri, CancellationToken cancellationToken) => Map(await GetJsonAsync<T>(requestUri, cancellationToken).ConfigureAwait(false));
    private async Task<UsersClientResult<TResponse>> Post<TRequest,TResponse>(string route,TRequest request,CancellationToken cancellationToken) => Map(await PostJsonAsync<TRequest,TResponse>(UsersApiRoutes.Base + route, request, cancellationToken).ConfigureAwait(false));
    private async Task<UsersClientResult<TResponse>> Put<TRequest,TResponse>(string route,TRequest request,CancellationToken cancellationToken) => Map(await PutJsonAsync<TRequest,TResponse>(UsersApiRoutes.Base + route, request, cancellationToken).ConfigureAwait(false));

    private static string BuildSearchUri(SearchUsersQueryParameters p) => WithQuery(UsersApiRoutes.Search, [("query", p.Query), ("maxResults", p.MaxResults?.ToString(CultureInfo.InvariantCulture))]);
    private static string BuildEmailUri(CheckUserEmailInUseQueryParameters p) => WithQuery(UsersApiRoutes.CheckEmail, [("email", p.Email), ("excludeUserId", p.ExcludeUserId)]);
    private static string BuildPhoneUri(CheckUserPhoneNumberInUseQueryParameters p) => WithQuery(UsersApiRoutes.CheckPhone, [("phoneNumber", p.PhoneNumber), ("excludeUserId", p.ExcludeUserId)]);
    private static string BuildRoleUri(CheckUserRoleQueryParameters p) => WithQuery(UsersApiRoutes.CheckRole, [("userId", p.UserId), ("roleName", p.RoleName)]);
    private static string WithQuery(string route, IEnumerable<(string Key,string? Value)> values) { var q=values.Where(v=>!string.IsNullOrWhiteSpace(v.Value)).Select(v=>$"{Uri.EscapeDataString(v.Key)}={Uri.EscapeDataString(v.Value!)}").ToArray(); return UsersApiRoutes.Base + route + (q.Length==0 ? string.Empty : "?" + string.Join("&", q)); }

    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);
    private static UsersClientResult<T> Map<T>(ModuleApiClientResult<T> result) => result.IsSuccess ? UsersClientResult<T>.Success(result.Value!) : UsersClientResult<T>.Failed(MapFailure(result.Failure!));
    private static UsersClientFailure MapFailure(ModuleApiClientFailure failure) => new(MapKind(failure), failure.Message, failure.StatusCode);
    private static UsersClientFailureKind MapKind(ModuleApiClientFailure failure) => failure.Code switch { "Canceled" => UsersClientFailureKind.Canceled, "Timeout" => UsersClientFailureKind.Timeout, "Transport" => UsersClientFailureKind.Transport, "Deserialization" => UsersClientFailureKind.Deserialization, nameof(HttpStatusCode.BadRequest) => UsersClientFailureKind.Validation, nameof(HttpStatusCode.Unauthorized) => UsersClientFailureKind.Unauthorized, nameof(HttpStatusCode.Forbidden) => UsersClientFailureKind.Forbidden, nameof(HttpStatusCode.NotFound) => UsersClientFailureKind.NotFound, nameof(HttpStatusCode.Conflict) => UsersClientFailureKind.Conflict, _ => UsersClientFailureKind.UnexpectedServerFailure };
}
