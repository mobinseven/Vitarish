using System.Security.Cryptography;
using MediatR;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Routing;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Landing.Application;
using Vitarish.Landing.Contracts;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Landing.Api;

public static class LandingApiEndpointMapping
{
    private const string SessionCookie = "__Host-vitarish-landing-session";
    private const string AnonymousJustification = "The public landing assistant accepts short visitor messages without an account; access is bound to a protected, HttpOnly public-session cookie and per-IP rate limits.";
    private static readonly HashSet<string> OwnerKeys = new(["home", "about", "portfolio", "contact", "explore"], StringComparer.Ordinal);

    public static IEndpointRouteBuilder MapLandingApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapAnonymousModuleGroup(LandingApiRoutes.Base, AnonymousJustification);
        _ = group.MapModulePost(
                LandingApiRoutes.Conversations,
                (SubmitLandingMessageRequest request, HttpContext context, IOptions<LandingPlatformOptions> options, IPartiesService parties, IDataProtectionProvider protection, ISender sender, CancellationToken ct) => SubmitAsync(request, context, options, parties, protection, sender, ct),
                "LandingSubmitConversation",
                typeof(LandingConversationResponse),
                profile: ModuleEndpointProfile.AnonymousJson,
                anonymousJustification: AnonymousJustification)
            .WithMetadata(new RequestSizeLimitAttribute(4_096))
            .RequireRateLimiting(LandingRateLimitPolicies.PublicConversation);
        _ = group.MapModuleDelete(LandingApiRoutes.ConversationById, (Guid conversationId, string ownerKey, HttpContext context, IOptions<LandingPlatformOptions> options, IPartiesService parties, IDataProtectionProvider protection, ISender sender, CancellationToken ct) => DeleteAsync(conversationId, ownerKey, context, options, parties, protection, sender, ct), "LandingDeleteConversation", profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: AnonymousJustification).RequireRateLimiting(LandingRateLimitPolicies.PublicConversation);
        RouteGroupBuilder administration = endpoints.MapProtectedModuleGroup($"{LandingApiRoutes.Base}{LandingApiRoutes.Administration}");
        administration.WithGroupName("Landing").WithTags("Landing");
        administration.RequireAuthorization(new AuthorizeAttribute { Policy = "PlatformAdmin" });
        _ = administration.MapModuleGet("/host-binding", async (IOptions<LandingPlatformOptions> options, IPartiesService parties, CancellationToken ct) =>
        {
            Uri landingUrl = new(options.Value.LandingUrl, UriKind.Absolute);
            var binding = await parties.GetLandingHostBindingAsync(landingUrl.Host, ct);
            return Results.Ok(new LandingHostBindingResponse(binding.Hostname, binding.TenantId));
        }, "LandingAdministrationGetHostBinding", typeof(LandingHostBindingResponse));
        _ = administration.MapModulePut("/host-binding", async (SetLandingHostBindingRequest request, IOptions<LandingPlatformOptions> options, IPartiesService parties, CancellationToken ct) =>
        {
            Uri landingUrl = new(options.Value.LandingUrl, UriKind.Absolute);
            var change = await parties.SetLandingHostBindingAsync(landingUrl.Host, request.TenantId, ct);
            return change is null ? Results.NotFound() : Results.Ok(new SetLandingHostBindingResponse(change.Hostname, change.PreviousTenantId, change.TenantId, change.Changed));
        }, "LandingAdministrationSetHostBinding", typeof(SetLandingHostBindingResponse));
        return endpoints;
    }

    private static async Task<IResult> SubmitAsync(SubmitLandingMessageRequest request, HttpContext context, IOptions<LandingPlatformOptions> options, IPartiesService parties, IDataProtectionProvider protection, ISender sender, CancellationToken ct)
    {
        if (!Valid(request.OwnerKey, request.Message) || (request.ConversationId is not null && !Guid.TryParse(request.ConversationId, out _))) return Invalid();
        Guid? requestTenantId = await ResolveBoundTenantAsync(context, options, parties, ct);
        if (requestTenantId is null)
            return Results.NotFound();
        string session = GetOrCreateSession(context, protection);
        Guid? conversationId = request.ConversationId is null ? null : Guid.Parse(request.ConversationId);
        LandingConversationResult result = await sender.Send(new RunLandingConversation(requestTenantId.Value.ToString("D"), session, request.OwnerKey, request.Message.Trim(), conversationId), ct);
        return ToHttpResult(result);
    }

    public static IResult ToHttpResult(LandingConversationResult result) => result.Outcome switch
    {
        AgentConversationOutcome.Succeeded when result.ConversationId.HasValue && !string.IsNullOrWhiteSpace(result.Message) => Results.Ok(new LandingConversationResponse(result.ConversationId.Value.ToString("N"), result.Message)),
        AgentConversationOutcome.NotFound => Results.NotFound(new LandingError(LandingErrorCodes.NotFound, "The conversation was not found.")),
        AgentConversationOutcome.Busy => Results.Conflict(new LandingError(LandingErrorCodes.Busy, "The conversation is already processing a message.")),
        AgentConversationOutcome.HistoryLimitExceeded => Results.Conflict(new LandingError(LandingErrorCodes.InvalidHistory, "The conversation history has reached its supported limit; start a new conversation.")),
        AgentConversationOutcome.InvalidHistory => Results.Conflict(new LandingError(LandingErrorCodes.InvalidHistory, "The conversation history is invalid.")),
        AgentConversationOutcome.ProfileChanged => Results.Conflict(new LandingError(LandingErrorCodes.Conflict, "The assistant configuration changed; start a new conversation.")),
        _ => Results.Json(new LandingError(LandingErrorCodes.Unavailable, "The assistant is temporarily unavailable."), statusCode: StatusCodes.Status503ServiceUnavailable)
    };

    private static async Task<IResult> DeleteAsync(Guid conversationId, string ownerKey, HttpContext context, IOptions<LandingPlatformOptions> options, IPartiesService parties, IDataProtectionProvider protection, ISender sender, CancellationToken ct)
    {
        string? session = ReadSession(context, protection);
        Guid? requestTenantId = await ResolveBoundTenantAsync(context, options, parties, ct);
        if (requestTenantId is null || session is null || conversationId == Guid.Empty || !OwnerKeys.Contains(ownerKey)) return Results.NotFound();
        AgentConversationOutcome result = await sender.Send(new DeleteLandingConversation(requestTenantId.Value.ToString("D"), session, ownerKey, conversationId), ct);
        return result switch { AgentConversationOutcome.Succeeded => Results.NoContent(), AgentConversationOutcome.Busy => Results.Conflict(new LandingError(LandingErrorCodes.Busy, "The conversation is already processing a message.")), _ => Results.NotFound() };
    }

    private static async Task<Guid?> ResolveBoundTenantAsync(HttpContext context, IOptions<LandingPlatformOptions> options, IPartiesService parties, CancellationToken ct)
    {
        if (context.GetTenantId() is not string requestTenantValue || !Guid.TryParse(requestTenantValue, out Guid requestTenantId) || requestTenantId == Guid.Empty)
            return null;
        if (!Uri.TryCreate(options.Value.LandingUrl, UriKind.Absolute, out Uri? landingUrl))
            return null;
        LandingHostBinding binding = await parties.GetLandingHostBindingAsync(landingUrl.Host, ct);
        return binding.TenantId == requestTenantId ? requestTenantId : null;
    }

    private static bool Valid(string ownerKey, string message) => OwnerKeys.Contains(ownerKey) && !string.IsNullOrWhiteSpace(message) && message.Trim().Length <= 2_000;
    private static IResult Invalid() => Results.BadRequest(new LandingError(LandingErrorCodes.Validation, "A valid page and message of at most 2,000 characters are required."));
    private static string GetOrCreateSession(HttpContext context, IDataProtectionProvider provider)
    {
        string? current = ReadSession(context, provider); if (current is not null) return current;
        string id = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        context.Response.Cookies.Append(SessionCookie, provider.CreateProtector("Vitarish.Landing.PublicSession.v1").Protect(id), new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, IsEssential = true, MaxAge = TimeSpan.FromDays(1), Path = "/" });
        return id;
    }
    private static string? ReadSession(HttpContext context, IDataProtectionProvider provider)
    {
        if (!context.Request.Cookies.TryGetValue(SessionCookie, out string? value)) return null;
        try { return provider.CreateProtector("Vitarish.Landing.PublicSession.v1").Unprotect(value); } catch (CryptographicException) { return null; }
    }
}
