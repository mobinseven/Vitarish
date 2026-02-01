using Vitarish.Entities.Server.Core;

namespace Vitarish.Parties.Application.Descriptors;

/// <summary>
/// Descriptor for filtering and querying Owner entities on the server.
/// </summary>
public record OwnerDescriptor : EntityDescriptor
{
    public string? Name { get; init; }
}
