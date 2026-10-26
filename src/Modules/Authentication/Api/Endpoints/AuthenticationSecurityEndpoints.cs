using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Authentication.Contracts.Authorization;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;

namespace Vitarish.Authentication.Api.Endpoints;

public static class AuthenticationSecurityEndpoints
{
    public static RouteGroupBuilder MapAuthenticationSecurityEndpoints(this RouteGroupBuilder group)
    {
        _ = group.MapModuleGet(AuthenticationRoutes.AccessHistory, async ([Microsoft.AspNetCore.Mvc.FromServices] GetAuthenticationAccessHistoryHandler handler, CancellationToken ct) =>
        {
            AuthenticationAccessHistory? history = await handler.Handle(new GetAuthenticationAccessHistoryQuery(), ct);
            return history is null ? Results.NotFound() : Results.Ok(ToDto(history));
        }, AuthenticationSecurityOperationIds.AccessHistoryGet, typeof(AuthenticationAccessHistoryDto))
            .Produces<AuthenticationAccessHistoryDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Gets the authenticated user's access history.");

        _ = group.MapModulePost(AuthenticationRoutes.AccessHistoryAcknowledge, async ([Microsoft.AspNetCore.Mvc.FromServices] AcknowledgeAuthenticationAccessHistoryHandler handler, Vitarish.Requests.Application.Abstractions.IRequestContext context, CancellationToken ct) =>
        {
            AuthenticationAccessHistory? history = await handler.Handle(context.UserId ?? string.Empty, context.TenantId, ct);
            return history is null ? Results.NotFound() : Results.Ok(ToDto(history));
        }, AuthenticationSecurityOperationIds.AccessHistoryAcknowledge, typeof(AuthenticationAccessHistoryDto));

        _ = group.MapModuleGet(AuthenticationRoutes.SessionNotices, async (HttpContext http, [Microsoft.AspNetCore.Mvc.FromServices] GetAuthenticationSessionNoticesHandler handler, CancellationToken ct) =>
        {
            string? sessionId = http.User.FindFirst(AuthenticationSessionClaimTypes.SessionId)?.Value;
            string? userId = AuthenticationSessionClaimTypes.FindSubject(http.User);
            return string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(userId)
                ? Results.Unauthorized()
                : Results.Ok((await handler.Handle(sessionId, userId, ct)).Select(notice => new AuthenticationSessionNoticeDto(notice.Id, notice.CreatedAt, ToNoticeType(notice.NoticeType))).ToArray());
        }, AuthenticationSecurityOperationIds.SessionNoticesGet, typeof(AuthenticationSessionNoticeDto[]));

        _ = group.MapModulePost(AuthenticationRoutes.SessionNoticeAcknowledge, async (AcknowledgeAuthenticationSessionNoticeRequest request, HttpContext http, [Microsoft.AspNetCore.Mvc.FromServices] AcknowledgeAuthenticationSessionNoticeHandler handler, CancellationToken ct) =>
        {
            string? sessionId = http.User.FindFirst(AuthenticationSessionClaimTypes.SessionId)?.Value;
            string? userId = AuthenticationSessionClaimTypes.FindSubject(http.User);
            if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
            return await handler.Handle(request.NoticeId, sessionId, userId, ct)
                ? Results.Ok(new AuthenticationActionResultDto(true))
                : Results.NotFound();
        }, AuthenticationSecurityOperationIds.SessionNoticeAcknowledge, typeof(AuthenticationActionResultDto))
            .Produces<AuthenticationActionResultDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group.MapModuleGet(AuthenticationRoutes.SecuritySessionPolicy, async ([Microsoft.AspNetCore.Mvc.FromServices] GetSecurityAccessPolicyHandler handler, CancellationToken ct) => ToDto(await handler.Handle(new GetSecurityAccessPolicyQuery(), ct)), AuthenticationSecurityOperationIds.SessionPolicyGet, typeof(SecuritySessionPolicyDto))
            .RequireAuthorization(AuthenticationAuthorizationPolicies.ReadSessionPolicy)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata())
            .Produces<SecuritySessionPolicyDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        _ = group.MapModulePut(AuthenticationRoutes.SecuritySessionPolicy, async (ChangeSecuritySessionPolicyRequest request, [Microsoft.AspNetCore.Mvc.FromServices] ChangeSecurityAccessPolicyHandler handler, CancellationToken ct) =>
        {
            try
            {
                SecurityAccessPolicyState saved = await handler.Handle(new ChangeSecuritySessionPolicyCommand(
                    request.MaximumConcurrentSessions,
                    request.ConcurrentSessionsFunctionallyRequired,
                    request.DefaultInactivityTimeoutMinutes,
                    request.AllowedSessionStartCidrs,
                    request.ExpectedVersion), ct);
                return Results.Ok(ToDto(saved));
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }
            catch (SecurityAccessPolicyConcurrencyException)
            {
                return Results.Conflict();
            }
            catch (SecurityAccessPolicyPersistenceException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }, AuthenticationSecurityOperationIds.SessionPolicyPut, typeof(SecuritySessionPolicyDto))
            .RequireAuthorization(AuthenticationAuthorizationPolicies.ChangeSessionPolicy)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata())
            .Produces<SecuritySessionPolicyDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        _ = group.MapModuleGet(AuthenticationRoutes.SecurityAuthenticationPolicy, async ([Microsoft.AspNetCore.Mvc.FromServices] GetSecurityAuthenticationPolicyHandler handler, CancellationToken ct) =>
        {
            SecurityAuthenticationPolicyManagementState state = await handler.Handle(ct);
            return Results.Ok(ToDto(state));
        }, AuthenticationSecurityOperationIds.AuthenticationPolicyGet, typeof(SecurityAuthenticationPolicyDto))
            .RequireAuthorization(AuthenticationAuthorizationPolicies.ReadAuthenticationPolicy)
            .Produces<SecurityAuthenticationPolicyDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        _ = group.MapModulePut(AuthenticationRoutes.SecurityAuthenticationPolicy, async (ChangeSecurityAuthenticationPolicyRequest request, [Microsoft.AspNetCore.Mvc.FromServices] ChangeSecurityAccessPolicyHandler handler, [Microsoft.AspNetCore.Mvc.FromServices] AuthenticationProfilePolicy profilePolicy, CancellationToken ct) =>
        {
            try
            {
                SecurityAccessPolicyState saved = await handler.Handle(new ChangeSecurityAuthenticationPolicyCommand(
                    request.MinimumPasswordLength,
                    request.RequirePasswordLowercase,
                    request.RequirePasswordUppercase,
                    request.RequirePasswordNumber,
                    request.RequirePasswordSpecialCharacter,
                    request.FailedAttemptThreshold,
                    request.FailedAttemptDenialDurationSeconds,
                    request.FailedAttemptResponse,
                    request.EnabledAuthenticationMechanisms,
                    request.PermittedPreAuthenticationActions,
                    request.ExpectedVersion,
                    request.SourceFailedAttemptThreshold), ct);
                SecurityAuthenticationPolicyManagementState managed = SecurityAuthenticationPolicyManagementState.ForState(saved, profilePolicy);
                return Results.Ok(ToDto(managed));
            }
            catch (SecurityAccessPolicyConcurrencyException)
            {
                return Results.Conflict();
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }
            catch (SecurityAccessPolicyPersistenceException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }, AuthenticationSecurityOperationIds.AuthenticationPolicyPut, typeof(SecurityAuthenticationPolicyDto))
            .RequireAuthorization(AuthenticationAuthorizationPolicies.ChangeAuthenticationPolicy)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata())
            .Produces<SecurityAuthenticationPolicyDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        _ = group.MapModuleGet(AuthenticationRoutes.SecurityUserInactivityPolicy, async (string userId, [Microsoft.AspNetCore.Mvc.FromServices] GetUserInactivityTimeoutHandler handler, CancellationToken ct) =>
        {
            AuthenticationUserInactivityTimeoutState? state = await handler.Handle(userId, ct);
            return state is null
                ? Results.NotFound()
                : Results.Ok(new UserInactivityTimeoutDto(state.Version, state.InactivityTimeoutMinutes));
        }, AuthenticationSecurityOperationIds.UserInactivityPolicyGet, typeof(UserInactivityTimeoutDto))
            .RequireAuthorization(AuthenticationAuthorizationPolicies.ChangeUserInactivityPolicy)
            .Produces<UserInactivityTimeoutDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group.MapModulePut(AuthenticationRoutes.SecurityUserInactivityPolicy, async (string userId, ChangeUserInactivityTimeoutRequest request, [Microsoft.AspNetCore.Mvc.FromServices] ChangeUserInactivityTimeoutHandler handler, CancellationToken ct) =>
        {
            try
            {
                long version = await handler.Handle(userId, request.InactivityTimeoutMinutes, request.ExpectedVersion, ct);
                return Results.Ok(new UserInactivityTimeoutDto(version, request.InactivityTimeoutMinutes));
            }
            catch (AuthenticationUserSessionPolicyConcurrencyException)
            {
                return Results.Conflict();
            }
            catch (AuthenticationUserNotFoundException)
            {
                return Results.NotFound();
            }
        }, AuthenticationSecurityOperationIds.UserInactivityPolicyPut, typeof(UserInactivityTimeoutDto))
            .RequireAuthorization(AuthenticationAuthorizationPolicies.ChangeUserInactivityPolicy)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata())
            .Produces<UserInactivityTimeoutDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group.MapModulePost(AuthenticationRoutes.SecurityUserSessionsRevoke, async (string userId, [Microsoft.AspNetCore.Mvc.FromServices] RevokeUserSessionsHandler handler, CancellationToken ct) =>
        {
            try
            {
                int ended = await handler.Handle(userId, ct);
                return Results.Ok(new UserSessionsRevocationDto(ended));
            }
            catch (AuthenticationUserNotFoundException)
            {
                return Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }, AuthenticationSecurityOperationIds.UserSessionsRevoke, typeof(UserSessionsRevocationDto))
            .RequireAuthorization(AuthenticationAuthorizationPolicies.ChangeUserInactivityPolicy)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata())
            .Produces<UserSessionsRevocationDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static SecuritySessionPolicyDto ToDto(SecurityAccessPolicyState state) => new(
        state.Policy.MaximumConcurrentSessions,
        state.Policy.ConcurrentSessionsFunctionallyRequired,
        state.Policy.DefaultInactivityTimeoutMinutes,
        (state.Policy.AllowedSessionStartCidrs ?? new HashSet<string>(StringComparer.Ordinal)).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        state.Version);

    private static SecurityAuthenticationPolicyDto ToDto(SecurityAuthenticationPolicyManagementState managed) => new(
        managed.State.Policy.PasswordPolicy.MinimumLength,
        managed.State.Policy.PasswordPolicy.RequireLowercase,
        managed.State.Policy.PasswordPolicy.RequireUppercase,
        managed.State.Policy.PasswordPolicy.RequireNumber,
        managed.State.Policy.PasswordPolicy.RequireSpecialCharacter,
        managed.State.Policy.FailedAttemptThreshold,
        checked((int)managed.State.Policy.FailedAttemptDenialDuration.TotalSeconds),
        managed.State.Policy.FailedAttemptResponse.ToString(),
        managed.State.Policy.EnabledAuthenticationMechanisms.Select(value => value.ToString()).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        managed.State.Policy.PermittedPreAuthenticationActions.Select(value => value.ToString()).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        managed.AvailableMechanisms.Select(value => value.ToString()).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        managed.RequiredMechanisms.Select(value => value.ToString()).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        managed.AvailableActions.Select(value => value.ToString()).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        managed.RequiredActions.Select(value => value.ToString()).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        managed.AvailableFailedAttemptResponses.Select(value => value.ToString()).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        PasswordPolicy.Default.MinimumLength,
        managed.State.Version,
        managed.State.Policy.SourceFailedAttemptThreshold);

    private static AuthenticationAccessHistoryDto ToDto(AuthenticationAccessHistory history) => new(
        history.PreviousSuccessfulAt,
        history.LastSuccessfulAt,
        history.LastUnsuccessfulAt,
        history.FailedAttemptsSincePreviousSuccessful,
        history.CurrentFailedAttempts,
        history.AcknowledgedAt,
        history.PreviousSuccessfulIpAddress,
        history.LastSuccessfulIpAddress,
        history.LastUnsuccessfulIpAddress);

    private static AuthenticationSessionNoticeType ToNoticeType(string noticeType) =>
        noticeType == AuthenticationSessionNotice.ParallelSessionEstablishedNoticeType
            ? AuthenticationSessionNoticeType.ParallelSessionEstablished
            : throw new InvalidOperationException("The persisted authentication session notice type is not supported.");
}
