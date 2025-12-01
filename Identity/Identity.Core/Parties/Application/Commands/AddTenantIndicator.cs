using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Core.Parties.Domain.Enums;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Command to add a tenant indicator.
/// </summary>
public sealed record AddTenantIndicator(
    Guid TenantId,
    IndicatorMethod Method,
    string Value,
    bool IsPrimary = false,
    IndicatorVerificationMethod VerificationMethod = IndicatorVerificationMethod.DnsTxt) : IRequest<Result<TenantIndicator>>;
