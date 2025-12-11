using MediatR;

namespace Vitarish.Parties.Application.Commands;

/// <summary>
/// Command to verify a tenant indicator.
/// </summary>
public sealed record VerifyTenantIndicator(Guid TenantId, Guid IndicatorId) : IRequest<bool>;
