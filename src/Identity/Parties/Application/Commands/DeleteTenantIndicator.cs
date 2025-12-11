using MediatR;

namespace Vitarish.Parties.Application.Commands;

/// <summary>
/// Command to delete a tenant indicator.
/// </summary>
public sealed record DeleteTenantIndicator(Guid TenantId, Guid IndicatorId) : IRequest<bool>;
