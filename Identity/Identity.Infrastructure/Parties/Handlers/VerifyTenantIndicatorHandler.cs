using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Domain.Enums;
using Vitarish.Identity.Infrastructure.Parties.Abstractions;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class VerifyTenantIndicatorHandler(
    PartiesDbContext db,
    IPublisher publisher,
    IDnsVerificationService dnsVerificationService) : IRequestHandler<VerifyTenantIndicator, Result>
{
    public async Task<Result> Handle(VerifyTenantIndicator request, CancellationToken cancellationToken)
    {
        var indicator = await db.TenantIndicators
            .FirstOrDefaultAsync(i => i.Id == request.IndicatorId && i.TenantId == request.TenantId, cancellationToken);

        if (indicator is null)
        {
            return Result.Failure(Error.NotFound("TenantIndicator.NotFound", "Tenant indicator was not found."));
        }

        bool verificationSucceeded = true;
        if (indicator.Status == IndicatorStatus.PendingVerification)
        {
            bool verified = indicator.Verification?.Method switch
            {
                IndicatorVerificationMethod.DnsTxt => indicator.Verification is not null &&
                    await dnsVerificationService.VerifyTxtRecordAsync(
                        indicator.Verification.Evidence,
                        indicator.Verification.EvidenceExpectedHash,
                        cancellationToken),
                _ => true
            };

            if (verified)
            {
                indicator.MarkVerified(indicator.Verification!);
            }
            else
            {
                verificationSucceeded = false;
            }
        }

        indicator.UpdateLastChecked();
        _ = await db.SaveChangesAsync(cancellationToken);

        if (!verificationSucceeded)
        {
            return Result.Failure(Error.Validation(
                "TenantIndicator.VerificationFailed",
                "Verification failed for the requested tenant indicator."));
        }

        await publisher.Publish(new Identity.Core.Parties.Domain.Events.TenantIndicatorUpdatedEvent(indicator.TenantId, indicator.Id), cancellationToken);
        return Result.Success();
    }
}
