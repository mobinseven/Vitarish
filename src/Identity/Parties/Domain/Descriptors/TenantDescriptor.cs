using Vitarish.Entities.Generators;
using Vitarish.Entities.Server.Core;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Domain.Descriptors;

/// <summary>
/// Descriptor for filtering and querying Tenant entities on the server.
/// </summary>
[EntityDescriptorFor(typeof(Tenant))]
public record TenantDescriptor : EntityDescriptor
{
    public Guid? OwnerId { get; init; }
    public string? Name { get; init; }
    public string? ClientId { get; init; }
}
