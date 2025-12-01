using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Command to update an existing <see cref="Domain.Entities.Tenant"/>.
/// </summary>
public sealed record UpdateTenant(
    Guid Id,
    string Name,
    string ClientId,
    string? LogoUrl = null,
    string? Description = null,
    string? ModifiedBy = null) : IRequest<Result>;
