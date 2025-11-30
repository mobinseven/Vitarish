using Vorn.Aaas.Parties.Domain.Enums;

namespace Vorn.Aaas.Parties.Presentation.Contracts;

/// <summary>
/// Request payload used when creating a new Owner.
/// </summary>
public sealed record CreateOwnerRequest(
    string Name,
    string? LogoUrl = null,
    string? Description = null,
    string? CreatedBy = null);

/// <summary>
/// Request payload used when updating an existing Owner.
/// </summary>
public sealed record UpdateOwnerRequest(
    Guid Id,
    string Name,
    string? LogoUrl = null,
    string? Description = null,
    string? ModifiedBy = null);

/// <summary>
/// Request payload used when creating a new Tenant.
/// </summary>
public sealed record CreateTenantRequest(
    Guid OwnerId,
    string Name,
    string ClientId,
    string? LogoUrl = null,
    string? Description = null,
    string? CreatedBy = null);

/// <summary>
/// Request payload used when updating an existing Tenant.
/// </summary>
public sealed record UpdateTenantRequest(
    Guid Id,
    Guid OwnerId,
    string Name,
    string ClientId,
    string? LogoUrl = null,
    string? Description = null,
    string? ModifiedBy = null);

/// <summary>
/// Request payload used when adding a tenant indicator.
/// </summary>
public sealed record AddTenantIndicatorRequest(
    Guid TenantId,
    IndicatorMethod Method,
    string Value,
    bool IsPrimary = false,
    IndicatorVerificationMethod VerificationMethod = IndicatorVerificationMethod.Manual);
