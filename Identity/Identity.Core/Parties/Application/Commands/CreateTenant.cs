using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Domain.Entities;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Command to create a new <see cref="Tenant"/>.
/// </summary>
public sealed record CreateTenant(
    Guid OwnerId,
    string Name,
    string ClientId,
    string? LogoUrl = null,
    string? Description = null,
    string? CreatedBy = null) : IRequest<Result<Tenant>>;
