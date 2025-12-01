using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Command to delete a tenant.
/// </summary>
public sealed record DeleteTenant(Guid Id) : IRequest<Result>;
