using Vitarish.Entities.Generators;
using Vitarish.Entities.Server.Core;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Parties.Domain.Descriptors;

/// <summary>
/// Descriptor for filtering and querying TenantIndicator entities on the server.
/// </summary>
[EntityDescriptorFor(typeof(TenantIndicator))]
public record TenantIndicatorDescriptor : EntityDescriptor
{
    public Guid? TenantId { get; init; }
    public IndicatorMethod? Method { get; init; }
    public string? Value { get; init; }
    public IndicatorStatus? Status { get; init; }
    public bool? IsPrimary { get; init; }
}
