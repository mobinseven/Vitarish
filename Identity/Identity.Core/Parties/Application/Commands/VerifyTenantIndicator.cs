using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Command to verify a tenant indicator.
/// </summary>
public sealed record VerifyTenantIndicator(Guid TenantId, Guid IndicatorId) : IRequest<Result>;
