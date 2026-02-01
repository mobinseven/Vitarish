using Vitarish.Entities.Server.Core;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Parties.Application.Descriptors;

/// <summary>
/// Descriptor for filtering and querying TenantIndicator entities on the server.
/// </summary>
public record TenantIndicatorDescriptor : EntityDescriptor
{
    public Guid? TenantId { get; init; }
    public IndicatorMethod? Method { get; init; }
    public string? Value { get; init; }
    public IndicatorStatus? Status { get; init; }
    public bool? IsPrimary { get; init; }
}
