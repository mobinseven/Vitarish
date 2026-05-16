using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Parties.Domain.Dtos;

/// <summary>
/// All data collected by the onboarding wizard for creating a tenant from scratch.
/// </summary>
public record SetupTenantRequest(
    string OwnerName,
    string TenantName,
    string AppId,
    IndicatorMethod IndicatorMethod,
    string IndicatorValue,
    IndicatorStatus IndicatorStatus,
    IReadOnlyList<string> SelectedModules,
    string UserId);
