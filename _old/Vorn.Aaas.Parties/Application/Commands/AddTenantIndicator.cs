
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Enums;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Commands;
/// <summary>
/// Command to add a tenant indicator.
/// </summary>
public sealed record AddTenantIndicator(
    Guid TenantId,
    IndicatorMethod Method,
    string Value,
    bool IsPrimary = false,
    IndicatorVerificationMethod VerificationMethod = IndicatorVerificationMethod.DnsTxt) : IRequest<TenantIndicator?>;

/// <summary>
/// Validator for <see cref="AddTenantIndicator"/>.
/// </summary>
public class AddTenantIndicatorValidator : AbstractValidator<AddTenantIndicator>
{
    public AddTenantIndicatorValidator()
    {
        _ = RuleFor(c => c.Value)
            .NotEmpty()
            .MaximumLength(253)
            .Must(BeValidHostname).WithMessage("Value must be a valid hostname");
        _ = RuleFor(c => c.Method).IsInEnum();
    }

    private bool BeValidHostname(string value) =>
        // Basic hostname validation
        !string.IsNullOrWhiteSpace(value) &&
               Uri.CheckHostName(value) != UriHostNameType.Unknown;
}

internal sealed class AddTenantIndicatorHandler(PartiesDbContext db, IPublisher publisher) : IRequestHandler<AddTenantIndicator, TenantIndicator?>
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

        await publisher.Publish(new Domain.Events.TenantIndicatorCreatedEvent(indicator.TenantId, indicator.Id), cancellationToken);

        return indicator;
    }
}
