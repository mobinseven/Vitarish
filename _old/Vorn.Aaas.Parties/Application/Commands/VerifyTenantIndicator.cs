
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Enums;
using Vorn.Aaas.Parties.Infrastructure.Abstractions;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Commands;
/// <summary>
/// Command to verify a tenant indicator.
/// </summary>
public sealed record VerifyTenantIndicator(Guid TenantId, Guid IndicatorId) : IRequest<bool>;

internal sealed class VerifyTenantIndicatorHandler(
    PartiesDbContext db,
    IPublisher publisher,
    IDnsVerificationService dnsVerificationService) : IRequestHandler<VerifyTenantIndicator, bool>
{
    public async Task<bool> Handle(VerifyTenantIndicator request, CancellationToken cancellationToken)
    {
        TenantIndicator? indicator = await db.TenantIndicators
            .FirstOrDefaultAsync(i => i.Id == request.IndicatorId && i.TenantId == request.TenantId, cancellationToken);

        if (indicator is null)
        {
            return false;
        }

        // Only update status if currently PendingVerification to avoid downgrading Active indicators
        if (indicator.Status == IndicatorStatus.PendingVerification)
        {
            bool verified = false;

            // Check verification method
            verified = indicator.Verification?.Method == IndicatorVerificationMethod.DnsTxt
                ? await dnsVerificationService.VerifyTxtRecordAsync(
                    indicator.Verification.Evidence,
                    indicator.Verification.EvidenceExpectedHash,
                    cancellationToken)
                : true;

            if (verified)
            {
                indicator.Status = IndicatorStatus.Verified;
                indicator.VerifiedAt = DateTimeOffset.UtcNow;
            }
        }

        indicator.LastCheckedAt = DateTimeOffset.UtcNow;

        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new Domain.Events.TenantIndicatorUpdatedEvent(indicator.TenantId, indicator.Id), cancellationToken);

        return true;
    }
}
