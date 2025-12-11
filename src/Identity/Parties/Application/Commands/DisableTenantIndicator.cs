using MediatR;

namespace Vitarish.Parties.Application.Commands;

/// <summary>
/// Command to disable a tenant indicator.
/// </summary>
public sealed record DisableTenantIndicator(Guid TenantId, Guid IndicatorId) : IRequest<bool>;
