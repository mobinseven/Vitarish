using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Command to update an existing <see cref="Domain.Entities.Owner"/>.
/// </summary>
public sealed record UpdateOwner(Guid Id, string Name, string? LogoUrl = null, string? Description = null, string? ModifiedBy = null)
    : IRequest<Result>;
