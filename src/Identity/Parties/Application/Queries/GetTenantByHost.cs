using MediatR;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Queries;

public record GetTenantByHost(string Host) : IRequest<Tenant?>;
