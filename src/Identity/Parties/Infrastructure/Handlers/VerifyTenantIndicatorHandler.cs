using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Abstractions;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class VerifyTenantIndicatorHandler(
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
            verified = (indicator.Verification?.Method) != IndicatorVerificationMethod.DnsTxt || await dnsVerificationService.VerifyTxtRecordAsync(
                    indicator.Verification.Evidence,
                    indicator.Verification.EvidenceExpectedHash,
                    cancellationToken);

            if (verified)
            {
                indicator.Status = IndicatorStatus.Verified;
                indicator.VerifiedAt = DateTimeOffset.UtcNow;
            }
        }

        indicator.LastCheckedAt = DateTimeOffset.UtcNow;

        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new TenantIndicatorUpdatedEvent(indicator.TenantId, indicator.Id), cancellationToken);

        return true;
    }
}
