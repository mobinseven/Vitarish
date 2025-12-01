using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Domain.Entities;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Command to create a new <see cref="Owner"/>.
/// </summary>
public sealed record CreateOwner(string Name, string? LogoUrl = null, string? Description = null, string? CreatedBy = null)
    : IRequest<Result<Owner>>;
