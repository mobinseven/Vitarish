using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Contracts.Responses;
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

    private static async Task<IResult> CreatePatAsync(string userId, CreatePatRequest request, IPersonalAccessTokenGenerator generator, CancellationToken ct)
    {
        string token = await generator.CreateTokenAsync(userId, request.LifetimeDays, request.Scopes, false, ct);
        System.Collections.Concurrent.ConcurrentDictionary<string, byte> tokens = tokenStore.GetOrAdd(userId, _ => new());
        tokens.TryAdd(token, 0);
        return Results.Ok(new PatCreateResponse(token));
    }

    private static Task<IResult> ListPatsAsync(string userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!tokenStore.TryGetValue(userId, out System.Collections.Concurrent.ConcurrentDictionary<string, byte>? tokens))
            return Task.FromResult<IResult>(Results.Ok(new PatListResponse(Array.Empty<string>())));
        return Task.FromResult<IResult>(Results.Ok(new PatListResponse([.. tokens.Keys])));
    }

    private static Task<IResult> RevokePatAsync(string userId, string token, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (tokenStore.TryGetValue(userId, out System.Collections.Concurrent.ConcurrentDictionary<string, byte>? tokens))
            tokens.TryRemove(token, out _);
        return Task.FromResult<IResult>(Results.Ok(true));
    }
}

public sealed record CreatePatRequest(int? LifetimeDays = null, IEnumerable<string>? Scopes = null);
public sealed record PatCreateResponse(string Token);
public sealed record PatListResponse(IReadOnlyCollection<string> Tokens);
