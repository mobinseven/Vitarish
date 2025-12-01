using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Core.Parties.Domain.Enums;
using Vitarish.Identity.Core.Parties.Domain.Events;
using Vitarish.Identity.Infrastructure.Parties.Data;
using Vitarish.Identity.Infrastructure.Parties.Extensions;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class AddTenantIndicatorHandler(PartiesDbContext db, IPublisher publisher)
    : IRequestHandler<AddTenantIndicator, Result<TenantIndicator>>
{
    public async Task<Result<TenantIndicator>> Handle(AddTenantIndicator request, CancellationToken cancellationToken)
    {
        Tenant? tenant = await db.Tenants.Include(t => t.Indicators).FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<TenantIndicator>(Error.NotFound("Tenant.NotFound", $"Tenant with ID '{request.TenantId}' was not found."));
        }

        VerificationInfo? verificationInfo = null;
        if (request.VerificationMethod == IndicatorVerificationMethod.DnsTxt)
        {
            string token = Guid.NewGuid().ToString("N");
            byte[] tokenBytes = Encoding.UTF8.GetBytes($"verify={token}");
            string hash = Convert.ToHexString(SHA256.HashData(tokenBytes)).ToLowerInvariant();

            verificationInfo = new VerificationInfo(
                IndicatorVerificationMethod.DnsTxt,
                $"_verify.{request.Value}",
                hash,
                token);
        }

        string canonicalValue = HostnameNormalizer.Canonical(request.Value);
        TenantIndicator indicator = tenant.AddIndicator(
            request.Method,
            canonicalValue,
            request.IsPrimary,
            IndicatorStatus.PendingVerification,
            verificationInfo);

        try
        {
            _ = await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Result.Failure<TenantIndicator>(Error.Conflict(
                "TenantIndicator.Duplicate",
                $"An indicator with method '{request.Method}' and value '{canonicalValue}' already exists."));
        }

        await publisher.Publish(new TenantIndicatorCreatedEvent(indicator.TenantId, indicator.Id), cancellationToken);
        return Result.Success(indicator);
    }
}
