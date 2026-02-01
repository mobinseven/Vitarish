using Vitarish.Entities.Server.Core;

namespace Vitarish.Parties.Application.Descriptors;

/// <summary>
/// Descriptor for filtering and querying Tenant entities on the server.
/// </summary>
public record TenantDescriptor : EntityDescriptor
{
    public Guid? OwnerId { get; init; }
    public string? Name { get; init; }
    public string? ClientId { get; init; }
}
