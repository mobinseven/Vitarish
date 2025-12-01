using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Command to activate a tenant indicator.
/// </summary>
public sealed record ActivateTenantIndicator(Guid TenantId, Guid IndicatorId) : IRequest<Result>;
