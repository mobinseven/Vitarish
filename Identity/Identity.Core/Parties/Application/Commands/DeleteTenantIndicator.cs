using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Command to delete a tenant indicator.
/// </summary>
public sealed record DeleteTenantIndicator(Guid TenantId, Guid IndicatorId) : IRequest<Result>;
