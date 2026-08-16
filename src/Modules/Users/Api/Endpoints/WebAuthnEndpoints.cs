using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Contracts.Requests;
using Vitarish.Users.Contracts.Routes;

namespace Vitarish.Users.Api.Endpoints;

public static class WebAuthnEndpoints
{
    public static RouteGroupBuilder MapWebAuthnEndpoints(this RouteGroupBuilder group)
    {
        group.MapModuleGet(UsersApiRoutes.WebAuthnCredentials, ListCredentialsAsync, "UsersListWebAuthnCredentials", typeof(Vitarish.Users.Contracts.Responses.WebAuthnCredentialResponse[]));
        group.MapModulePost(UsersApiRoutes.WebAuthnBeginRegister, BeginRegisterAsync, "UsersBeginWebAuthnRegister", typeof(string));
        group.MapModulePost(UsersApiRoutes.WebAuthnCompleteRegister, CompleteRegisterAsync, "UsersCompleteWebAuthnRegister", typeof(string));
        group.MapModuleDelete(UsersApiRoutes.WebAuthnDelete, DeleteCredentialAsync, "UsersDeleteWebAuthnCredential", typeof(bool));
        return group;
    }

    private static async Task<IResult> ListCredentialsAsync(string userId, IWebAuthnService webAuthnService, CancellationToken ct)
    {
        IEnumerable<WebAuthnCredentialDto> credentials = await webAuthnService.ListCredentialsAsync(userId, ct);
        return Results.Ok(credentials.Select(c => new WebAuthnCredentialResponse(c.Id, c.FriendlyName, c.RegisteredAt, c.LastUsedAt, c.Transports)).ToArray());
    }

    private static async Task<IResult> BeginRegisterAsync(string userId, IWebAuthnService webAuthnService, CancellationToken ct)
    {
        string options = await webAuthnService.BeginRegistrationAsync(userId, ct);
        return Results.Ok(options);
    }

    private static async Task<IResult> CompleteRegisterAsync(string userId, WebAuthnCompleteRegisterRequest request, IWebAuthnService webAuthnService, CancellationToken ct)
    {
        string credentialId = await webAuthnService.CompleteRegistrationAsync(userId, request.AttestationResponseJson, ct);
        return Results.Ok(credentialId);
    }

    private static async Task<IResult> DeleteCredentialAsync(string userId, Guid credentialId, IWebAuthnService webAuthnService, CancellationToken ct)
    {
        bool deleted = await webAuthnService.DeleteCredentialAsync(credentialId, userId, ct);
        return deleted ? Results.Ok(true) : Results.NotFound();
    }
}
