using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Addresses;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Domain.Profiles;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Parties.Contracts.Errors;
using Vitarish.Parties.Contracts.Routes;
using C = Vitarish.Parties.Contracts.Tenants;
using D = Vitarish.Parties.Domain.Dtos;
using DE = Vitarish.Parties.Domain.Entities;
using DEN = Vitarish.Parties.Domain.Enums;
using M = Vitarish.Parties.Domain.Models;

namespace Vitarish.Parties.Api;

public static class PartiesApiEndpointMapping
{
    // One DNS label; anything longer cannot be an address, so it never reaches the query.
    private const int SubdomainQueryMaximumLength = 63;
    private const int IdempotencyKeyMinimumLength = 8;
    private const int IdempotencyKeyMaximumLength = 100;

    public static IEndpointRouteBuilder MapPartiesApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(PartiesApiRoutes.Base);
        RouteGroupBuilder platformGroup = endpoints
            .MapProtectedModuleGroup($"{PartiesApiRoutes.Base}{PartiesApiRoutes.Platform}", requireTenantAccess: false)
            .WithGroupName(PartiesApiRoutes.DocumentTag)
            .WithTags(PartiesApiRoutes.DocumentTag);
        _ = platformGroup.MapModuleGet(
            PartiesApiRoutes.PlatformTenants,
            GetTenantsAsync,
            PartiesApiOperations.PlatformGetTenants,
            typeof(IEnumerable<C.TenantReadDto>))
            .RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        // A user creating a tenant has no role in the host's tenant yet, so onboarding
        // requires authentication but not ambient tenant access.
        RouteGroupBuilder onboardingGroup = endpoints
            .MapProtectedModuleGroup($"{PartiesApiRoutes.Base}{PartiesApiRoutes.Onboarding}", requireTenantAccess: false)
            .WithGroupName(PartiesApiRoutes.DocumentTag)
            .WithTags(PartiesApiRoutes.DocumentTag);
        _ = onboardingGroup.AddEndpointFilter(RequireTenantOnboardingAsync);
        _ = onboardingGroup.MapModuleGet(
            PartiesApiRoutes.SubdomainAvailability,
            GetSubdomainAvailabilityAsync,
            PartiesApiOperations.GetSubdomainAvailability,
            typeof(C.TenantSubdomainAvailabilityResponse));
        _ = onboardingGroup.MapModulePost(
                PartiesApiRoutes.OnboardingTenants,
                CreateOwnTenantAsync,
                PartiesApiOperations.CreateOwnTenant,
                typeof(C.CreateOwnTenantResponse),
                StatusCodes.Status201Created)
            .Produces<C.CreateOwnTenantResponse>(StatusCodes.Status200OK)
            .RequireRateLimiting(PartiesRateLimitPolicies.SelfServiceTenantCreation);
        _ = onboardingGroup.MapModuleGet(
            PartiesApiRoutes.OnboardingCategories,
            GetOnboardingCategories,
            PartiesApiOperations.GetOnboardingCategories,
            typeof(C.TenantCategoriesResponse));
        // On the shared sign-in host the user holds no role in the host's tenant, so these
        // require authentication but not ambient tenant access, and answer only on that host.
        RouteGroupBuilder signInGroup = endpoints
            .MapProtectedModuleGroup($"{PartiesApiRoutes.Base}{PartiesApiRoutes.SignIn}", requireTenantAccess: false)
            .WithGroupName(PartiesApiRoutes.DocumentTag)
            .WithTags(PartiesApiRoutes.DocumentTag);
        _ = signInGroup.AddEndpointFilter(RequireSignInHostAsync);
        _ = signInGroup.MapModuleGet(
            PartiesApiRoutes.SignInClinics,
            GetSignInClinicsAsync,
            PartiesApiOperations.GetSignInClinics,
            typeof(C.SignInClinicsResponse));
        _ = signInGroup.MapModulePost(
            PartiesApiRoutes.SignInClinicHandoff,
            IssueSignInClinicHandoffAsync,
            PartiesApiOperations.IssueSignInClinicHandoff,
            typeof(C.SignInHandoffResponse));
        _ = signInGroup.MapModulePost(
            PartiesApiRoutes.SignInOnboardingHandoff,
            IssueSignInOnboardingHandoffAsync,
            PartiesApiOperations.IssueSignInOnboardingHandoff,
            typeof(C.SignInHandoffResponse));
        _ = group.MapModuleGet(PartiesApiRoutes.TenantProfile, GetTenantProfileAsync, PartiesApiOperations.GetTenantProfile, typeof(C.TenantBusinessProfileResponse))
            .RequireAuthorization(IdentityDefinitions.TenantOwner);
        _ = group.MapModulePut(PartiesApiRoutes.TenantProfile, UpdateTenantProfileAsync, PartiesApiOperations.UpdateTenantProfile, successStatusCode: StatusCodes.Status204NoContent)
            .RequireAuthorization(IdentityDefinitions.TenantOwner);
        _ = group.MapModulePut(PartiesApiRoutes.TenantProfileLogo, SetTenantLogoAsync, PartiesApiOperations.SetTenantLogo, successStatusCode: StatusCodes.Status204NoContent)
            .RequireAuthorization(IdentityDefinitions.TenantOwner);
        _ = group.MapModulePut(PartiesApiRoutes.TenantProfileLogoPromptDismissal, DismissTenantLogoPromptAsync, PartiesApiOperations.DismissTenantLogoPrompt, successStatusCode: StatusCodes.Status204NoContent)
            .RequireAuthorization(IdentityDefinitions.TenantOwner);
        _ = group.MapModuleGet(PartiesApiRoutes.UserTenants, GetUserTenantsAsync, "PartiesGetUserTenants");
        _ = group.MapModuleGet(PartiesApiRoutes.Owners, GetOwnersAsync, "PartiesGetOwners").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet(PartiesApiRoutes.OwnerById, GetOwnerAsync, "PartiesGetOwner").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePost(PartiesApiRoutes.Owners, CreateOwnerAsync, "PartiesCreateOwner").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePut(PartiesApiRoutes.OwnerById, UpdateOwnerAsync, "PartiesUpdateOwner").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleDelete(PartiesApiRoutes.OwnerById, DeleteOwnerAsync, "PartiesDeleteOwner").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet(PartiesApiRoutes.Tenants, GetTenantsAsync, "PartiesGetTenants").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet(PartiesApiRoutes.TenantById, GetTenantAsync, "PartiesGetTenant").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet(PartiesApiRoutes.TenantsByOwner, GetTenantsByOwnerAsync, "PartiesGetTenantsByOwner").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePost(PartiesApiRoutes.Tenants, CreateTenantAsync, "PartiesCreateTenant").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePut(PartiesApiRoutes.TenantById, UpdateTenantAsync, "PartiesUpdateTenant").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleDelete(PartiesApiRoutes.TenantById, DeleteTenantAsync, "PartiesDeleteTenant").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet(PartiesApiRoutes.TenantIndicatorById, GetTenantIndicatorAsync, "PartiesGetTenantIndicator").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet(PartiesApiRoutes.TenantIndicatorsByTenant, GetTenantIndicatorsByTenantAsync, "PartiesGetTenantIndicatorsByTenant").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePost(PartiesApiRoutes.TenantIndicators, CreateTenantIndicatorAsync, "PartiesCreateTenantIndicator").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePut(PartiesApiRoutes.TenantIndicatorById, UpdateTenantIndicatorAsync, "PartiesUpdateTenantIndicator").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleDelete(PartiesApiRoutes.TenantIndicatorById, DeleteTenantIndicatorAsync, "PartiesDeleteTenantIndicator").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePost(PartiesApiRoutes.SetupTenant, SetupTenantAsync, "PartiesSetupTenant").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationOwners}", GetOwnersAsync, "PartiesAdministrationGetOwners").Produces<IEnumerable<C.OwnerReadDto>>(StatusCodes.Status200OK).RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationOwnerById}", GetOwnerAsync, "PartiesAdministrationGetOwner").Produces<C.OwnerReadDto>(StatusCodes.Status200OK).RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePost($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationOwners}", CreateOwnerAsync, "PartiesAdministrationCreateOwner").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePut($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationOwnerById}", UpdateOwnerAsync, "PartiesAdministrationUpdateOwner").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenants}", GetTenantsAsync, "PartiesAdministrationGetTenants").Produces<IEnumerable<C.TenantReadDto>>(StatusCodes.Status200OK).RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenantById}", GetTenantAsync, "PartiesAdministrationGetTenant").Produces<C.TenantReadDto>(StatusCodes.Status200OK).RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenantsByOwner}", GetTenantsByOwnerAsync, "PartiesAdministrationGetTenantsByOwner").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePost($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenants}", CreateTenantAsync, "PartiesAdministrationCreateTenant").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePut($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenantById}", UpdateAdministrationTenantAsync, "PartiesAdministrationUpdateTenant").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenantIndicatorsByTenant}", GetTenantIndicatorsByTenantAsync, "PartiesAdministrationGetTenantIndicators").Produces<IEnumerable<C.TenantIndicatorReadDto>>(StatusCodes.Status200OK).RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModuleGet($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenantIndicatorById}", GetTenantIndicatorAsync, "PartiesAdministrationGetTenantIndicator").Produces<C.TenantIndicatorReadDto>(StatusCodes.Status200OK).RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePost($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.TenantIndicators}", CreateAdministrationTenantIndicatorAsync, "PartiesAdministrationCreateTenantIndicator").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        _ = group.MapModulePut($"{PartiesApiRoutes.Administration}{PartiesApiRoutes.AdministrationTenantIndicatorById}", UpdateAdministrationTenantIndicatorAsync, "PartiesAdministrationUpdateTenantIndicator").RequireAuthorization(IdentityDefinitions.PlatformAdmin);
        return endpoints;
    }

    private static async ValueTask<object?> RequireSignInHostAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) =>
        await context.HttpContext.RequestServices.GetRequiredService<ISender>().Send(new IsSignInHostQuery(), context.HttpContext.RequestAborted)
            ? await next(context)
            : NotFound("This is not the sign-in host.");

    private static async Task<IResult> GetSignInClinicsAsync([FromServices] ISender sender, CancellationToken cancellationToken) =>
        await sender.Send(new ListSignInClinicsQuery(), cancellationToken) is { } clinics
            ? Results.Ok(new C.SignInClinicsResponse([.. clinics.Select(clinic => new C.SignInClinicDto(clinic.TenantId, clinic.Name, clinic.Hostname, clinic.Role))]))
            : Results.Unauthorized();

    // A clinic that is not the user's answers the same as one that does not exist.
    private static async Task<IResult> IssueSignInClinicHandoffAsync(Guid tenantId, [FromServices] ISender sender, CancellationToken cancellationToken) =>
        ToHandoffResult(await sender.Send(new IssueSignInHandoffCommand(tenantId), cancellationToken), "No clinic of yours was found.");

    private static async Task<IResult> IssueSignInOnboardingHandoffAsync([FromServices] ISender sender, CancellationToken cancellationToken) =>
        ToHandoffResult(await sender.Send(new IssueSignInHandoffCommand(null), cancellationToken), "Clinic creation is not available.");

    private static IResult ToHandoffResult(SignInHandoff? handoff, string notFound) =>
        handoff is null ? NotFound(notFound) : Results.Ok(new C.SignInHandoffResponse(handoff.Hostname, handoff.Token));

    private static ValueTask<object?> RequireTenantOnboardingAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        IServiceProvider services = context.HttpContext.RequestServices;
        bool available = services.GetRequiredService<IConfiguration>().GetValue<bool>(Vitarish.Modules.Base.Core.Module.TenantOnboardingFeatureKey)
            && services.GetRequiredService<IOptions<TenantAddressOptions>>().Value.IsConfigured
            && services.GetService<SelfServiceTenantProfile>() is not null;
        return available ? next(context) : ValueTask.FromResult<object?>(NotFound("Tenant onboarding is not available."));
    }

    private static async Task<IResult> GetSubdomainAvailabilityAsync(
        [FromQuery(Name = PartiesApiRoutes.SubdomainQueryParameter)] string? subdomain,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subdomain) || subdomain.Length > SubdomainQueryMaximumLength)
            return BadRequest($"A subdomain of at most {SubdomainQueryMaximumLength} characters is required.");
        TenantSubdomainAvailability availability = await sender.Send(new GetTenantSubdomainAvailabilityQuery(subdomain), cancellationToken);
        return Results.Ok(new C.TenantSubdomainAvailabilityResponse(availability.Subdomain, availability.Hostname, (C.TenantSubdomainAvailabilityStatus)availability.Status));
    }

    private static async Task<IResult> CreateOwnTenantAsync(
        C.CreateOwnTenantRequest request,
        [FromHeader(Name = PartiesApiRoutes.IdempotencyKeyHeader)] string? idempotencyKey,
        [FromServices] IRequestContext requestContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestContext.UserId))
            return Results.Unauthorized();
        if (idempotencyKey is null || idempotencyKey.Length is < IdempotencyKeyMinimumLength or > IdempotencyKeyMaximumLength
            || !idempotencyKey.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
            return BadRequest($"An {PartiesApiRoutes.IdempotencyKeyHeader} header of {IdempotencyKeyMinimumLength} to {IdempotencyKeyMaximumLength} letters, digits, hyphens, or underscores is required.");
        if (request.Name is null || request.Subdomain is null || request.Subdomain.Length > SubdomainQueryMaximumLength)
            return BadRequest("A name and a subdomain are required.");
        TenantBusinessDetails? details = TenantBusinessProfileMapping.ToDomain(request.BusinessDetails, out string? detailsError);
        if (detailsError is not null)
            return BadRequest(detailsError);

        CreateOwnTenantResult result = await sender.Send(
            new CreateOwnTenantCommand(requestContext.UserId, request.Name, request.Subdomain, request.Description, idempotencyKey, request.Category, details),
            cancellationToken);
        return result.Status switch
        {
            CreateOwnTenantStatus.Created => Results.Created((string?)null, new C.CreateOwnTenantResponse(result.TenantId!.Value, result.Hostname!, result.HandoffToken)),
            CreateOwnTenantStatus.Replayed => Results.Ok(new C.CreateOwnTenantResponse(result.TenantId!.Value, result.Hostname!, result.HandoffToken)),
            CreateOwnTenantStatus.Invalid => BadRequest(result.Error ?? "The request is invalid."),
            CreateOwnTenantStatus.AddressMalformed => BadRequest("The subdomain must be 3 to 30 lowercase letters, digits, or inner single hyphens."),
            CreateOwnTenantStatus.AddressReserved => BadRequest("This subdomain is reserved."),
            CreateOwnTenantStatus.AddressTaken => Conflict("This subdomain is already taken."),
            CreateOwnTenantStatus.IdempotencyConflict => Conflict($"This {PartiesApiRoutes.IdempotencyKeyHeader} was already used for a different request."),
            CreateOwnTenantStatus.CreatorNotFound or CreateOwnTenantStatus.PhoneNotVerified =>
                Results.Json(new PartiesError(PartiesErrorCode.Forbidden, "A verified phone number is required to create a clinic."), statusCode: StatusCodes.Status403Forbidden),
            CreateOwnTenantStatus.LimitReached =>
                Results.Json(new PartiesError(PartiesErrorCode.Conflict, "Too many clinics were created recently. Try again later."), statusCode: StatusCodes.Status429TooManyRequests),
            _ => throw new InvalidOperationException($"Unhandled tenant creation status {result.Status}.")
        };
    }

    // Arguments bind before the onboarding filter runs, so a missing profile must not fail binding.
    private static IResult GetOnboardingCategories([FromServices] SelfServiceTenantProfile? profile) =>
        profile is null
            ? NotFound("Tenant onboarding is not available.")
            : Results.Ok(new C.TenantCategoriesResponse(profile.Categories));

    private static async Task<IResult> GetTenantProfileAsync([FromServices] ISender sender, CancellationToken cancellationToken) =>
        await sender.Send(new GetTenantBusinessProfileQuery(), cancellationToken) is { } profile
            ? Results.Ok(TenantBusinessProfileMapping.ToContract(profile))
            : NotFound("The tenant was not found.");

    private static async Task<IResult> UpdateTenantProfileAsync(
        C.UpdateTenantBusinessProfileRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        TenantBusinessDetails? details = TenantBusinessProfileMapping.ToDomain(request.BusinessDetails, out string? detailsError);
        if (detailsError is not null)
            return BadRequest(detailsError);
        return ToResult(await sender.Send(new UpdateTenantBusinessProfileCommand(request.Description, request.Category, details), cancellationToken));
    }

    private static async Task<IResult> SetTenantLogoAsync(C.SetTenantLogoRequest request, [FromServices] ISender sender, CancellationToken cancellationToken) =>
        ToResult(await sender.Send(new SetTenantLogoCommand(request.FileId), cancellationToken));

    private static async Task<IResult> DismissTenantLogoPromptAsync([FromServices] ISender sender, CancellationToken cancellationToken) =>
        ToResult(await sender.Send(new DismissTenantLogoPromptCommand(), cancellationToken));

    private static IResult ToResult(TenantBusinessProfileUpdateResult result) => result.Status switch
    {
        TenantBusinessProfileUpdateStatus.Updated => Results.NoContent(),
        TenantBusinessProfileUpdateStatus.NotFound => NotFound("The tenant was not found."),
        TenantBusinessProfileUpdateStatus.Invalid => BadRequest(result.Error ?? "The request is invalid."),
        _ => throw new InvalidOperationException($"Unhandled tenant profile status {result.Status}.")
    };

    private static async Task<IResult> GetUserTenantsAsync(IPartiesService service, CancellationToken cancellationToken) =>
        Results.Ok((await service.GetUserTenantsAsync(cancellationToken)).Select(ToContract));
    private static async Task<IResult> GetOwnerAsync(Guid ownerId, IPartiesService service, CancellationToken cancellationToken) =>
        await service.GetOwnerAsync(ownerId, cancellationToken) is { } owner ? Results.Ok(ToContract(owner)) : NotFound("Owner was not found.");
    private static async Task<IResult> GetOwnersAsync(IPartiesService service, CancellationToken cancellationToken) =>
        Results.Ok((await service.GetOwnersAsync(cancellationToken)).Select(ToContract));
    private static async Task<IResult> CreateOwnerAsync(C.OwnerWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Owner name is required.");
        D.OwnerReadDto? owner = await service.CreateOwnerAsync(ToDomain(request), cancellationToken);
        return owner is null ? Conflict("Owner could not be created.") : Results.Ok(ToContract(owner));
    }
    private static async Task<IResult> UpdateOwnerAsync(Guid ownerId, C.OwnerWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (ownerId != request.Id) return BadRequest("Owner route id must match request id.");
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Owner name is required.");
        D.OwnerReadDto? owner = await service.UpdateOwnerAsync(ToDomain(request), cancellationToken);
        return owner is null ? NotFound("Owner was not found.") : Results.Ok(ToContract(owner));
    }
    private static async Task<IResult> DeleteOwnerAsync(Guid ownerId, IPartiesService service, CancellationToken cancellationToken)
    {
        await service.DeleteOwnerAsync(ownerId, cancellationToken);
        return Results.Ok(true);
    }
    private static async Task<IResult> GetTenantAsync(Guid tenantId, IPartiesService service, CancellationToken cancellationToken) =>
        await service.GetTenantAsync(tenantId, cancellationToken) is { } tenant ? Results.Ok(ToContract(tenant)) : NotFound("Tenant was not found.");
    private static async Task<IResult> GetTenantsAsync(IPartiesService service, CancellationToken cancellationToken) =>
        Results.Ok((await service.GetTenantsAsync(cancellationToken)).Select(ToContract));
    private static async Task<IResult> GetTenantsByOwnerAsync(Guid ownerId, IPartiesService service, CancellationToken cancellationToken) =>
        Results.Ok((await service.GetTenantsByOwnerAsync(ownerId, cancellationToken)).Select(ToContract));
    private static async Task<IResult> CreateTenantAsync(C.TenantWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.AppId)) return BadRequest("Tenant name and app id are required.");
        D.TenantReadDto? tenant = await service.CreateTenantAsync(ToDomain(request), cancellationToken);
        return tenant is null ? Conflict("Tenant could not be created.") : Results.Ok(ToContract(tenant));
    }
    private static async Task<IResult> UpdateTenantAsync(Guid tenantId, C.TenantWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (tenantId != request.Id) return BadRequest("Tenant route id must match request id.");
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.AppId)) return BadRequest("Tenant name and app id are required.");
        D.TenantReadDto? tenant = await service.UpdateTenantAsync(ToDomain(request), cancellationToken);
        return tenant is null ? NotFound("Tenant was not found.") : Results.Ok(ToContract(tenant));
    }
    private static async Task<IResult> UpdateAdministrationTenantAsync(Guid tenantId, C.TenantWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        D.TenantReadDto? existing = await service.GetTenantAsync(tenantId, cancellationToken);
        if (existing is null) return NotFound("Tenant was not found.");
        if (request.OwnerId != existing.OwnerId) return BadRequest("Tenant ownership cannot be changed through platform administration.");
        return await UpdateTenantAsync(tenantId, request, service, cancellationToken);
    }
    private static async Task<IResult> DeleteTenantAsync(Guid tenantId, IPartiesService service, CancellationToken cancellationToken)
    {
        await service.DeleteTenantAsync(tenantId, cancellationToken);
        return Results.Ok(true);
    }
    private static async Task<IResult> GetTenantIndicatorAsync(Guid tenantIndicatorId, IPartiesService service, CancellationToken cancellationToken) =>
        await service.GetTenantIndicatorAsync(tenantIndicatorId, cancellationToken) is { } indicator ? Results.Ok(ToContract(indicator)) : NotFound("Tenant indicator was not found.");
    private static async Task<IResult> GetTenantIndicatorsByTenantAsync(Guid tenantId, IPartiesService service, CancellationToken cancellationToken) =>
        Results.Ok((await service.GetTenantIndicatorsByTenantAsync(tenantId, cancellationToken)).Select(ToContract));
    private static async Task<IResult> CreateAdministrationTenantIndicatorAsync(C.TenantIndicatorWriteDto request, IPartiesService service, IConfiguration configuration, CancellationToken cancellationToken)
    {
        if (IsConfiguredLandingHostname(request.Value, request.Method, configuration)) return BadRequest("The configured Landing hostname can only be changed through the Landing binding workflow.");
        return await CreateTenantIndicatorAsync(request, service, cancellationToken);
    }
    private static async Task<IResult> CreateTenantIndicatorAsync(C.TenantIndicatorWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Value)) return BadRequest("Tenant indicator value is required.");
        D.TenantIndicatorReadDto? indicator = await service.CreateTenantIndicatorAsync(ToDomain(request), cancellationToken);
        return indicator is null ? Conflict("Tenant indicator could not be created.") : Results.Ok(ToContract(indicator));
    }
    private static async Task<IResult> UpdateAdministrationTenantIndicatorAsync(Guid tenantIndicatorId, C.TenantIndicatorWriteDto request, IPartiesService service, IConfiguration configuration, CancellationToken cancellationToken)
    {
        D.TenantIndicatorReadDto? existing = await service.GetTenantIndicatorAsync(tenantIndicatorId, cancellationToken);
        if (existing is not null && existing.TenantId != request.TenantId) return BadRequest("Tenant indicator ownership cannot be changed through platform administration.");
        if (existing is not null && IsConfiguredLandingHostname(existing.Value, (C.IndicatorMethod)existing.Method, configuration)) return BadRequest("The configured Landing hostname can only be changed through the Landing binding workflow.");
        if (IsConfiguredLandingHostname(request.Value, request.Method, configuration)) return BadRequest("The configured Landing hostname can only be changed through the Landing binding workflow.");
        return await UpdateTenantIndicatorAsync(tenantIndicatorId, request, service, cancellationToken);
    }
    private static async Task<IResult> UpdateTenantIndicatorAsync(Guid tenantIndicatorId, C.TenantIndicatorWriteDto request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (tenantIndicatorId != request.Id) return BadRequest("Tenant indicator route id must match request id.");
        if (string.IsNullOrWhiteSpace(request.Value)) return BadRequest("Tenant indicator value is required.");
        D.TenantIndicatorReadDto? indicator = await service.UpdateTenantIndicatorAsync(ToDomain(request), cancellationToken);
        return indicator is null ? NotFound("Tenant indicator was not found.") : Results.Ok(ToContract(indicator));
    }

    private static bool IsConfiguredLandingHostname(string value, C.IndicatorMethod method, IConfiguration configuration)
    {
        if (method != C.IndicatorMethod.Hostname) return false;
        string? configured = configuration["Vitarish:LandingUrl"];
        return Uri.TryCreate(configured, UriKind.Absolute, out Uri? uri) &&
            string.Equals(uri.Host.TrimEnd('.'), value.Trim().TrimEnd('.'), StringComparison.OrdinalIgnoreCase);
    }
    private static async Task<IResult> DeleteTenantIndicatorAsync(Guid tenantIndicatorId, IPartiesService service, CancellationToken cancellationToken)
    {
        await service.DeleteTenantIndicatorAsync(tenantIndicatorId, cancellationToken);
        return Results.Ok(true);
    }
    private static async Task<IResult> SetupTenantAsync(C.SetupTenantRequest request, IPartiesService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OwnerName) || string.IsNullOrWhiteSpace(request.TenantName) || string.IsNullOrWhiteSpace(request.AppId)) return BadRequest("Owner name, tenant name, and app id are required.");
        D.SetupTenantRequest domain = new(request.OwnerName, request.TenantName, request.AppId, (DEN.IndicatorMethod)request.IndicatorMethod, request.IndicatorValue, (DEN.IndicatorStatus)request.IndicatorStatus, request.SelectedModules, request.UserId);
        D.TenantReadDto? tenant = await service.SetupTenantAsync(domain, cancellationToken);
        return tenant is null ? Conflict("Tenant setup could not be completed.") : Results.Ok(ToContract(tenant));
    }
    private static D.OwnerWriteDto ToDomain(C.OwnerWriteDto dto) => new() { Id = dto.Id, Name = dto.Name, LogoUrl = dto.LogoUrl, Description = dto.Description };
    private static D.TenantWriteDto ToDomain(C.TenantWriteDto dto) => new() { Id = dto.Id, OwnerId = dto.OwnerId, Name = dto.Name, LogoUrl = dto.LogoUrl, Description = dto.Description, AppId = dto.AppId, Metadata = dto.Metadata };
    private static D.TenantIndicatorWriteDto ToDomain(C.TenantIndicatorWriteDto dto) => new() { Id = dto.Id, TenantId = dto.TenantId, Method = (DEN.IndicatorMethod)dto.Method, Value = dto.Value, Status = (DEN.IndicatorStatus)dto.Status, IsPrimary = dto.IsPrimary, Verification = ToDomain(dto.Verification), VerifiedAt = dto.VerifiedAt, LastCheckedAt = dto.LastCheckedAt };
    private static C.OwnerReadDto ToContract(D.OwnerReadDto dto) => new() { Id = dto.Id, Name = dto.Name, LogoUrl = dto.LogoUrl, Description = dto.Description };
    private static C.TenantReadDto ToContract(D.TenantReadDto dto) => new() { Id = dto.Id, OwnerId = dto.OwnerId, Name = dto.Name, LogoUrl = dto.LogoUrl, Description = dto.Description, AppId = dto.AppId, Metadata = dto.Metadata };
    private static C.TenantIndicatorReadDto ToContract(D.TenantIndicatorReadDto dto) => new() { Id = dto.Id, TenantId = dto.TenantId, Method = (C.IndicatorMethod)dto.Method, Value = dto.Value, Status = (C.IndicatorStatus)dto.Status, IsPrimary = dto.IsPrimary, Verification = ToContract(dto.Verification), VerifiedAt = dto.VerifiedAt, LastCheckedAt = dto.LastCheckedAt };
    private static C.UserTenantDto ToContract(M.UserTenant dto) => new(dto.TenantId, Guid.Empty, null, dto.TenantName, dto.TenantAppId, dto.TenantLogoUrl, dto.TenantDescription, null, dto.IndicatorValue);
    private static DE.VerificationInfo? ToDomain(C.VerificationInfoDto? dto) => dto is null ? null : new() { Method = (DEN.IndicatorVerificationMethod)dto.Method, Evidence = dto.Evidence, EvidenceExpectedHash = dto.EvidenceExpectedHash, Token = dto.Token };
    private static C.VerificationInfoDto? ToContract(DE.VerificationInfo? dto) => dto is null ? null : new() { Method = (C.IndicatorVerificationMethod)dto.Method, Evidence = dto.Evidence, EvidenceExpectedHash = dto.EvidenceExpectedHash, Token = dto.Token };
    private static IResult BadRequest(string message) => Results.BadRequest(new PartiesError(PartiesErrorCode.Validation, message));
    private static IResult NotFound(string message) => Results.NotFound(new PartiesError(PartiesErrorCode.NotFound, message));
    private static IResult Conflict(string message) => Results.Conflict(new PartiesError(PartiesErrorCode.Conflict, message));
}
