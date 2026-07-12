using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Abstractions;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Contracts.Requests;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Contracts.Routes;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Api.Endpoints;

public static class ProfileEndpoints
{
    public static RouteGroupBuilder MapProfileEndpoints(this RouteGroupBuilder group)
    {
        group.MapModuleGet(UsersApiRoutes.Profile, GetProfileAsync, "UsersGetProfile", typeof(UserProfileResponse));
        group.MapModulePut(UsersApiRoutes.Profile, UpdateProfileAsync, "UsersUpdateProfile", typeof(UsersCommandResponse));
        group.MapModuleGet(UsersApiRoutes.Culture, GetCultureAsync, "UsersGetCulture", typeof(string));
        group.MapModulePut(UsersApiRoutes.Culture, SetCultureAsync, "UsersSetCulture", typeof(UsersCommandResponse));
        group.MapModuleGet(UsersApiRoutes.Theme, GetThemeAsync, "UsersGetTheme", typeof(string));
        group.MapModulePut(UsersApiRoutes.Theme, SetThemeAsync, "UsersSetTheme", typeof(UsersCommandResponse));
        return group;
    }

    private static async Task<IResult> GetProfileAsync(string userId, IUsersService usersService, IUserProfileService profileService, CancellationToken ct)
    {
        User? user = await usersService.GetUserAsync(userId, ct);
        if (user is null) return Results.NotFound();

        Dictionary<string, string> profile = await profileService.GetProfileAsync(userId, ct) ?? new Dictionary<string, string>();
        string? culture = profile.TryGetValue("Culture", out string? c) ? c : null;
        string? theme = profile.TryGetValue("Theme", out string? t) ? t : null;

        return Results.Ok(new UserProfileResponse(
            user.Id, user.UserName, user.FirstName, user.LastName,
            user.Email, user.PhoneNumber, culture, theme, culture));
    }

    private static async Task<IResult> UpdateProfileAsync(string userId, UpdateUserProfileRequest request, IUsersService usersService, CancellationToken ct)
    {
        User? user = await usersService.GetUserAsync(userId, ct);
        if (user is null) return Results.NotFound();

        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;

        IdentityResult result = await usersService.UpdateUserAsync(new UpdateUserCommand(user), ct);
        return result.Succeeded
            ? Results.Ok(UsersCommandResponse.Success())
            : Results.BadRequest(UsersCommandResponse.Failed(result.Errors.Select(e => new UsersCommandError(e.Code, e.Description)).ToArray()));
    }

    private static async Task<IResult> GetCultureAsync(string userId, IUserProfileService profileService, CancellationToken ct)
    {
        Dictionary<string, string> profile = await profileService.GetProfileAsync(userId, ct) ?? new Dictionary<string, string>();
        string? culture = profile.TryGetValue("Culture", out string? c) ? c : null;
        return Results.Ok(culture);
    }

    private static async Task<IResult> SetCultureAsync(string userId, SetUserCultureRequest request, IUserProfileService profileService, IValidLocaleProvider? validLocaleProvider, CancellationToken ct)
    {
        if (validLocaleProvider is not null && !string.IsNullOrWhiteSpace(request.Culture))
        {
            bool isValid = await validLocaleProvider.IsValidLocaleAsync(request.Culture, ct);
            if (!isValid)
            {
                return Results.BadRequest(UsersCommandResponse.Failed(
                    [new UsersCommandError("InvalidLocale", $"Locale '{request.Culture}' is not supported.")]));
            }
        }

        Dictionary<string, string> profile = await profileService.GetProfileAsync(userId, ct) ?? new Dictionary<string, string>();
        profile["Culture"] = request.Culture;
        await profileService.UpdateProfileAsync(userId, profile, ct);
        return Results.Ok(UsersCommandResponse.Success());
    }

    private static async Task<IResult> GetThemeAsync(string userId, IUserProfileService profileService, CancellationToken ct)
    {
        Dictionary<string, string> profile = await profileService.GetProfileAsync(userId, ct) ?? new Dictionary<string, string>();
        string? theme = profile.TryGetValue("Theme", out string? t) ? t : null;
        return Results.Ok(theme);
    }

    private static async Task<IResult> SetThemeAsync(string userId, SetUserThemeRequest request, IUserProfileService profileService, CancellationToken ct)
    {
        Dictionary<string, string> profile = await profileService.GetProfileAsync(userId, ct) ?? new Dictionary<string, string>();
        profile["Theme"] = request.Theme;
        await profileService.UpdateProfileAsync(userId, profile, ct);
        return Results.Ok(UsersCommandResponse.Success());
    }
}
