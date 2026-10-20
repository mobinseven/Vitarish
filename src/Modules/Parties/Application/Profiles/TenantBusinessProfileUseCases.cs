using MediatR;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Domain.Profiles;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Parties.Application.Profiles;

/// <summary>What the owner sees and edits about their tenant's business.</summary>
public sealed record TenantBusinessProfileView(
    string Name,
    string? Description,
    string? LogoUrl,
    string? Category,
    TenantBusinessDetails? Details,
    bool LogoPromptDismissed);

public interface ITenantBusinessProfileStore
{
    Task<TenantBusinessProfileView?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the description, category, and details. False when the tenant does not exist.</summary>
    Task<bool> SaveAsync(Guid tenantId, string? description, string? category, TenantBusinessDetails? details, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>False when the tenant does not exist.</summary>
    Task<bool> SetLogoUrlAsync(Guid tenantId, string logoUrl, CancellationToken cancellationToken = default);

    /// <summary>Records that the owner dismissed the logo prompt; keeps the first time. False when the tenant does not exist.</summary>
    Task<bool> DismissLogoPromptAsync(Guid tenantId, DateTime nowUtc, CancellationToken cancellationToken = default);
}

/// <summary>The current tenant's own uploaded images, as far as its business profile cares.</summary>
public interface ITenantImageFiles
{
    /// <summary>True when every file exists in the current tenant and is an image browsers can show.</summary>
    Task<bool> AreOwnImagesAsync(IReadOnlyCollection<Guid> fileIds, CancellationToken cancellationToken = default);

    /// <summary>The address the tenant's signed-in users load the image from.</summary>
    string ContentUrl(Guid fileId);
}

public enum TenantBusinessProfileUpdateStatus
{
    Updated = 1,
    NotFound = 2,
    Invalid = 3
}

public sealed record TenantBusinessProfileUpdateResult(TenantBusinessProfileUpdateStatus Status, string? Error = null);

/// <summary>Reads the current tenant's business profile.</summary>
public sealed record GetTenantBusinessProfileQuery : IRequest<TenantBusinessProfileView?>;

/// <summary>Replaces the current tenant's description, category, and business details.</summary>
public sealed record UpdateTenantBusinessProfileCommand(string? Description, string? Category, TenantBusinessDetails? Details)
    : IRequest<TenantBusinessProfileUpdateResult>;

/// <summary>Makes one of the current tenant's uploaded images its logo.</summary>
public sealed record SetTenantLogoCommand(Guid FileId) : IRequest<TenantBusinessProfileUpdateResult>;

/// <summary>Stops prompting the current tenant's owner to add a logo.</summary>
public sealed record DismissTenantLogoPromptCommand : IRequest<TenantBusinessProfileUpdateResult>;

public sealed class GetTenantBusinessProfileQueryHandler(ITenantBusinessProfileStore profiles, IRequestContext requestContext)
    : IRequestHandler<GetTenantBusinessProfileQuery, TenantBusinessProfileView?>
{
    public async Task<TenantBusinessProfileView?> Handle(GetTenantBusinessProfileQuery request, CancellationToken cancellationToken) =>
        Guid.TryParse(requestContext.TenantId, out Guid tenantId)
            ? await profiles.GetAsync(tenantId, cancellationToken)
            : null;
}

/// <summary>
/// Concurrent edits replace each other whole; the last one saved wins. Saving the same values
/// again changes nothing, so a retried request is harmless.
/// </summary>
public sealed class UpdateTenantBusinessProfileCommandHandler(
    ITenantBusinessProfileStore profiles,
    ITenantImageFiles images,
    IRequestContext requestContext,
    TimeProvider timeProvider,
    SelfServiceTenantProfile? selfServiceProfile = null)
    : IRequestHandler<UpdateTenantBusinessProfileCommand, TenantBusinessProfileUpdateResult>
{
    public async Task<TenantBusinessProfileUpdateResult> Handle(UpdateTenantBusinessProfileCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(requestContext.TenantId, out Guid tenantId))
            return new(TenantBusinessProfileUpdateStatus.NotFound);

        string? description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (description?.Length > CreateOwnTenantCommandHandler.DescriptionMaximumLength)
            return new(TenantBusinessProfileUpdateStatus.Invalid, $"The description can have at most {CreateOwnTenantCommandHandler.DescriptionMaximumLength} characters.");
        string? category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        if (category is not null && selfServiceProfile?.AllowsCategory(category) != true)
            return new(TenantBusinessProfileUpdateStatus.Invalid, "The category is not one this app offers.");
        if (request.Details?.HasPhotos == true && !await images.AreOwnImagesAsync([.. request.Details.PhotoFileIds], cancellationToken))
            return new(TenantBusinessProfileUpdateStatus.Invalid, "Each photo must be an image uploaded to this tenant.");

        bool saved = await profiles.SaveAsync(tenantId, description, category, request.Details, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return new(saved ? TenantBusinessProfileUpdateStatus.Updated : TenantBusinessProfileUpdateStatus.NotFound);
    }
}

public sealed class SetTenantLogoCommandHandler(
    ITenantBusinessProfileStore profiles,
    ITenantImageFiles images,
    IRequestContext requestContext)
    : IRequestHandler<SetTenantLogoCommand, TenantBusinessProfileUpdateResult>
{
    public async Task<TenantBusinessProfileUpdateResult> Handle(SetTenantLogoCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(requestContext.TenantId, out Guid tenantId))
            return new(TenantBusinessProfileUpdateStatus.NotFound);
        if (request.FileId == Guid.Empty || !await images.AreOwnImagesAsync([request.FileId], cancellationToken))
            return new(TenantBusinessProfileUpdateStatus.Invalid, "The logo must be an image uploaded to this tenant.");

        bool saved = await profiles.SetLogoUrlAsync(tenantId, images.ContentUrl(request.FileId), cancellationToken);
        return new(saved ? TenantBusinessProfileUpdateStatus.Updated : TenantBusinessProfileUpdateStatus.NotFound);
    }
}

public sealed class DismissTenantLogoPromptCommandHandler(
    ITenantBusinessProfileStore profiles,
    IRequestContext requestContext,
    TimeProvider timeProvider)
    : IRequestHandler<DismissTenantLogoPromptCommand, TenantBusinessProfileUpdateResult>
{
    public async Task<TenantBusinessProfileUpdateResult> Handle(DismissTenantLogoPromptCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(requestContext.TenantId, out Guid tenantId))
            return new(TenantBusinessProfileUpdateStatus.NotFound);

        bool saved = await profiles.DismissLogoPromptAsync(tenantId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return new(saved ? TenantBusinessProfileUpdateStatus.Updated : TenantBusinessProfileUpdateStatus.NotFound);
    }
}
