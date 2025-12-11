using MediatR;

namespace Vitarish.Parties.Application.Commands;

/// <summary>
/// Command to delete a <see cref="Domain.Entities.Tenant"/>.
/// </summary>
public sealed record DeleteTenant(Guid Id) : IRequest;
