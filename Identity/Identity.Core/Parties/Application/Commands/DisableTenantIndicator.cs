using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Command to disable a tenant indicator.
/// </summary>
public sealed record DisableTenantIndicator(Guid TenantId, Guid IndicatorId) : IRequest<Result>;
