using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class AddTenantIndicatorHandler(PartiesDbContext db, IPublisher publisher) : IRequestHandler<AddTenantIndicator, TenantIndicator?>
{
    public async Task<TenantIndicator?> Handle(AddTenantIndicator request, CancellationToken cancellationToken)
    {
        // Verify tenant exists
        if (!await db.Tenants.AnyAsync(t => t.Id == request.TenantId, cancellationToken))
        {
            return null;
        }

        VerificationInfo? verificationInfo = null;

        // Generate verification info only for DNS TXT method
        if (request.VerificationMethod == IndicatorVerificationMethod.DnsTxt)
        {
            string token = Guid.NewGuid().ToString("N");
            byte[] tokenBytes = Encoding.UTF8.GetBytes($"verify={token}");
            string hash = Convert.ToHexString(SHA256.HashData(tokenBytes)).ToLowerInvariant();

            verificationInfo = new VerificationInfo
            {
                Method = IndicatorVerificationMethod.DnsTxt,
                Evidence = $"_verify.{request.Value}",
                EvidenceExpectedHash = hash,
                Token = token
            };
        }

        TenantIndicator indicator = new()
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Method = request.Method,
            Value = request.Value,
            Status = IndicatorStatus.PendingVerification,
            IsPrimary = request.IsPrimary,
            Verification = verificationInfo,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _ = db.TenantIndicators.Add(indicator);
        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new TenantIndicatorCreatedEvent(indicator.TenantId, indicator.Id), cancellationToken);

        return indicator;
    }
}
