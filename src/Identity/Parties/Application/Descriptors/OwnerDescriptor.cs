using Vitarish.Entities.Generators;
using Vitarish.Entities.Server.Core;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Descriptors;

/// <summary>
/// Descriptor for filtering and querying Owner entities on the server.
/// </summary>
[EntityDescriptorFor(typeof(Owner))]
public record OwnerDescriptor : EntityDescriptor
{
    public string? Name { get; init; }
}
