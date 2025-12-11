using MediatR;

namespace Vitarish.Parties.Application.Commands;

/// <summary>
/// Command to activate a tenant indicator.
/// </summary>
public sealed record ActivateTenantIndicator(Guid TenantId, Guid IndicatorId) : IRequest<bool>;
