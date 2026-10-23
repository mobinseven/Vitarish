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
using Vitarish.Intelligence.Application.Memory;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Intelligence.Contracts;
using Vitarish.Landing.Application;
using Vitarish.Landing.Contracts;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Landing.Api;

public static class LandingApiEndpointMapping
{
    private const string SessionCookie = "__Host-vitarish-landing-session";
    private const string CatalogJustification = "The public landing page reads which product features are live and its consultation questions; the catalog holds no tenant or user data and is rate limited per IP.";
    private const string AnonymousJustification = "The public landing assistant accepts short visitor messages without an account; access is bound to a protected, HttpOnly public-session cookie and per-IP rate limits.";
    private static readonly HashSet<string> OwnerKeys = new(["home", "about", "portfolio", "contact", "explore"], StringComparer.Ordinal);

    public static IEndpointRouteBuilder MapLandingApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapAnonymousModuleGroup(LandingApiRoutes.Base, AnonymousJustification);
        _ = group.MapModulePost(
                LandingApiRoutes.Conversations,
                (SubmitLandingMessageRequest request, HttpContext context, IOptions<LandingPlatformOptions> options, IPartiesService parties, IDataProtectionProvider protection, [FromServices] IRequestContext requestContext, ISender sender, CancellationToken ct) => SubmitAsync(request, context, options, parties, protection, requestContext, sender, ct),
                "LandingSubmitConversation",
                typeof(LandingConversationResponse),
                profile: ModuleEndpointProfile.AnonymousJson,
                anonymousJustification: AnonymousJustification)
            .WithMetadata(new RequestSizeLimitAttribute(4_096))
            .RequireRateLimiting(LandingRateLimitPolicies.PublicConversation);
        _ = group.MapModuleGet(
                LandingApiRoutes.Catalog,
                GetCatalog,
                "LandingGetCatalog",
                typeof(LandingCatalogResponse),
                profile: ModuleEndpointProfile.AnonymousJson,
                anonymousJustification: CatalogJustification)
            .RequireRateLimiting(LandingRateLimitPolicies.PublicCatalog);
        _ = group.MapModuleDelete(LandingApiRoutes.ConversationById, (Guid conversationId, string ownerKey, HttpContext context, IOptions<LandingPlatformOptions> options, IPartiesService parties, IDataProtectionProvider protection, ISender sender, CancellationToken ct) => DeleteAsync(conversationId, ownerKey, context, options, parties, protection, sender, ct), "LandingDeleteConversation", profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: AnonymousJustification).RequireRateLimiting(LandingRateLimitPolicies.PublicConversation);
        // Signed-in visitors on the landing host, who need not have a clinic (role-free sessions, ADR-0017).
        RouteGroupBuilder visitors = endpoints.MapProtectedModuleGroup(LandingApiRoutes.Base, requireTenantAccess: false);
        visitors.WithGroupName(LandingApiRoutes.DocumentTag).WithTags(LandingApiRoutes.DocumentTag);
        _ = visitors.MapModulePost(
                LandingApiRoutes.OnboardingHandoff,
                IssueOnboardingHandoffAsync,
                "LandingIssueOnboardingHandoff",
                typeof(LandingOnboardingHandoffResponse))
            .RequireRateLimiting(LandingRateLimitPolicies.PublicConversation);
        RouteGroupBuilder administration = endpoints.MapProtectedModuleGroup($"{LandingApiRoutes.Base}{LandingApiRoutes.Administration}");
        administration.WithGroupName(LandingApiRoutes.DocumentTag).WithTags(LandingApiRoutes.DocumentTag);
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

    private static async Task<IResult> SubmitAsync(SubmitLandingMessageRequest request, HttpContext context, IOptions<LandingPlatformOptions> options, IPartiesService parties, IDataProtectionProvider protection, IRequestContext requestContext, ISender sender, CancellationToken ct)
    {
        if (!Valid(request.OwnerKey, request.Message) || (request.ConversationId is not null && !Guid.TryParse(request.ConversationId, out _))) return Invalid();
        Guid? requestTenantId = await ResolveBoundTenantAsync(context, options, parties, ct);
        if (requestTenantId is null)
            return Results.NotFound();
        string session = GetOrCreateSession(context, protection);
        Guid? conversationId = request.ConversationId is null ? null : Guid.Parse(request.ConversationId);
        LandingConversationResult result = await sender.Send(new RunLandingConversation(requestTenantId.Value.ToString("D"), session, request.OwnerKey, request.Message.Trim(), conversationId, requestContext.UserId), ct);
        return ToHttpResult(result);
    }

    public static IResult ToHttpResult(LandingConversationResult result) => result.Outcome switch
    {
        AgentConversationOutcome.Succeeded when result.ConversationId.HasValue && result.Reply is { } reply => Results.Ok(new LandingConversationResponse(result.ConversationId.Value.ToString("N"), reply.Text, ToResponse(reply, result.Remembered))),
        AgentConversationOutcome.NotFound => Results.NotFound(new LandingError(LandingErrorCodes.NotFound, "The conversation was not found.")),
        AgentConversationOutcome.Busy => Results.Conflict(new LandingError(LandingErrorCodes.Busy, "The conversation is already processing a message.")),
        AgentConversationOutcome.HistoryLimitExceeded => Results.Conflict(new LandingError(LandingErrorCodes.InvalidHistory, "The conversation history has reached its supported limit; start a new conversation.")),
        AgentConversationOutcome.InvalidHistory => Results.Conflict(new LandingError(LandingErrorCodes.InvalidHistory, "The conversation history is invalid.")),
        AgentConversationOutcome.ProfileChanged => Results.Conflict(new LandingError(LandingErrorCodes.Conflict, "The assistant configuration changed; start a new conversation.")),
        _ => Results.Json(new LandingError(LandingErrorCodes.Unavailable, "The assistant is temporarily unavailable."), statusCode: StatusCodes.Status503ServiceUnavailable)
    };

    public static LandingCatalogResponse GetCatalog([FromServices] SelfServiceTenantProfile? profile) =>
        new(LandingFeatureCatalog.Clinic.Select(feature => new LandingFeatureResponse(feature.Key, feature.Status switch
            {
                LandingFeatureStatus.Live => LandingFeatureAvailability.Live,
                LandingFeatureStatus.InProgress => LandingFeatureAvailability.InProgress,
                _ => LandingFeatureAvailability.NotYet
            })).ToArray(),
            new LandingConsultationResponse(ToResponse(LandingConsultation.Kind(profile?.Categories ?? [])), ToResponse(LandingConsultation.Booking), ToResponse(LandingConsultation.Pains)),
            new LandingNextActionsResponse(LandingNextActions.OpenClinic, LandingNextActions.TalkToTeam, LandingNextActions.TryVoice));

    private static LandingConsultationQuestionResponse ToResponse(LandingConsultationQuestion question) =>
        new(question.Key, question.MaxSelections, question.Options.Select(option => new LandingConsultationOptionResponse(
            option.Key,
            option.FeatureKeys,
            option.Proof switch
            {
                null => null,
                Application.LandingProofKind.NoShowCost => Contracts.LandingProofKind.NoShowCost,
                Application.LandingProofKind.BookingPreview => Contracts.LandingProofKind.BookingPreview,
                Application.LandingProofKind.PatientTimeline => Contracts.LandingProofKind.PatientTimeline,
                Application.LandingProofKind.Dictation => Contracts.LandingProofKind.Dictation,
                Application.LandingProofKind.ShiftWeek => Contracts.LandingProofKind.ShiftWeek,
                _ => Contracts.LandingProofKind.InProgress
            })).ToArray());

    private static AgentInteractiveReplyResponse ToResponse(AgentInteractiveReply reply, IReadOnlyList<UserMemoryItem> remembered) =>
        new(reply.Text,
            reply.Question is { } question
                ? new AgentInteractiveQuestionResponse(question.Prompt, question.Options.Select(option => new AgentInteractiveOptionResponse(option.Key, option.Label)).ToArray(), question.MaxSelections)
                : null,
            reply.References,
            reply.NextAction,
            remembered.Select(item => new AgentRememberedResponse(item.Id, item.Text)).ToArray());

    private static async Task<IResult> IssueOnboardingHandoffAsync(LandingOnboardingHandoffRequest request, HttpContext context, IOptions<LandingPlatformOptions> options, IPartiesService parties, ISender sender, CancellationToken ct)
    {
        IReadOnlyList<string> answers = request.Answers ?? [];
        if (!LandingConsultation.AreKnownAnswers(answers) || answers.Count > OnboardingSetupDraftRules.MaxAnswers)
            return Invalid("Only the consultation's own answers can be carried into sign-up.");
        if (await ResolveBoundTenantAsync(context, options, parties, ct) is null)
            return Results.NotFound();
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            // The draft only pre-fills sign-up, so failing to save it never blocks the handoff;
            // an answer sign-up does not offer is refused instead of being silently dropped.
            SaveOnboardingSetupDraftOutcome saved = await sender.Send(new SaveOnboardingSetupDraftCommand(request.Category, answers), ct);
            if (saved == SaveOnboardingSetupDraftOutcome.Invalid)
                return Invalid("The clinic kind is not one sign-up offers.");
        }
        SignInHandoff? handoff = await sender.Send(new IssueSignInHandoffCommand(null), ct);
        return handoff is null
            ? Results.NotFound(new LandingError(LandingErrorCodes.NotFound, "Clinic creation is not available."))
            : Results.Ok(new LandingOnboardingHandoffResponse(handoff.Hostname, handoff.Token));
    }

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
    private static IResult Invalid() => Invalid("A valid page and message of at most 2,000 characters are required.");
    private static IResult Invalid(string message) => Results.BadRequest(new LandingError(LandingErrorCodes.Validation, message));
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
