using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Contracts.Requests;
using Vitarish.Users.Contracts.Routes;

namespace Vitarish.Users.Api.Endpoints;

public static class PatEndpoints
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<string, byte>> tokenStore = new();

    public static RouteGroupBuilder MapPatEndpoints(this RouteGroupBuilder group)
    {
        group.MapModulePost(UsersApiRoutes.PatBase, CreatePatAsync, "UsersCreatePat", typeof(PatCreateResponse));
        group.MapModuleGet(UsersApiRoutes.PatBase, ListPatsAsync, "UsersListPats", typeof(PatListResponse));
        group.MapModuleDelete(UsersApiRoutes.PatRevoke, RevokePatAsync, "UsersRevokePat", typeof(bool));
        return group;
    }

    private static async Task<IResult> CreatePatAsync(string userId, CreatePatRequest request, UserSubjectAccessPolicy access, IPersonalAccessTokenGenerator generator, CancellationToken ct)
    {
        if (!access.AllowsSelf(userId)) return Results.Forbid();
        string token = await generator.CreateTokenAsync(userId, request.LifetimeDays, request.Scopes, false, ct);
        System.Collections.Concurrent.ConcurrentDictionary<string, byte> tokens = tokenStore.GetOrAdd(userId, _ => new());
        tokens.TryAdd(token, 0);
        return Results.Ok(new PatCreateResponse(token));
    }

    private static IResult ListPatsAsync(string userId, UserSubjectAccessPolicy access)
    {
        if (!access.AllowsSelf(userId)) return Results.Forbid();
        if (!tokenStore.TryGetValue(userId, out System.Collections.Concurrent.ConcurrentDictionary<string, byte>? tokens))
            return Results.Ok(new PatListResponse(Array.Empty<string>()));
        return Results.Ok(new PatListResponse([.. tokens.Keys]));
    }

    private static IResult RevokePatAsync(string userId, string token, UserSubjectAccessPolicy access)
    {
        if (!access.AllowsSelf(userId)) return Results.Forbid();
        if (tokenStore.TryGetValue(userId, out System.Collections.Concurrent.ConcurrentDictionary<string, byte>? tokens))
            tokens.TryRemove(token, out _);
        return Results.Ok(true);
    }
}
