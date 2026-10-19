using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Extensions.Options;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Parties.Application.Addresses;
using Vitarish.Parties.Domain.Addresses;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Parties.Application.Onboarding;

public enum CreateOwnTenantStatus
{
    Created = 1,
    /// <summary>The same creator repeated the same request; nothing new was created.</summary>
    Replayed = 2,
    Invalid = 3,
    AddressMalformed = 4,
    AddressReserved = 5,
    AddressTaken = 6,
    CreatorNotFound = 7,
    PhoneNotVerified = 8,
    LimitReached = 9,
    /// <summary>The idempotency key was already used for a different request.</summary>
    IdempotencyConflict = 10
}

public sealed record CreateOwnTenantResult(CreateOwnTenantStatus Status, Guid? TenantId = null, string? Hostname = null, string? Error = null);

/// <summary>
/// A signed-in user creates their own tenant at <c>&lt;subdomain&gt;.&lt;base domain&gt;</c> and becomes
/// its owner. <paramref name="CreatorUserId"/> comes from the session, never from the request body.
/// </summary>
public sealed record CreateOwnTenantCommand(
    string CreatorUserId,
    string Name,
    string Subdomain,
    string? Description,
    string IdempotencyKey) : IRequest<CreateOwnTenantResult>;

public sealed class CreateOwnTenantCommandHandler(
    IOptions<TenantAddressOptions> addressOptions,
    SelfServiceTenantProfile profile,
    ITenantCreatorDirectory creators,
    ISelfServiceTenantStore tenants,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext,
    TimeProvider timeProvider) : IRequestHandler<CreateOwnTenantCommand, CreateOwnTenantResult>
{
    public const int NameMaximumLength = 100;
    public const int DescriptionMaximumLength = 500;
    public const int CreationsPerWindow = 3;
    public static readonly TimeSpan CreationWindow = TimeSpan.FromDays(1);

    public async Task<CreateOwnTenantResult> Handle(CreateOwnTenantCommand request, CancellationToken cancellationToken)
    {
        TenantAddressOptions addresses = addressOptions.Value;
        if (!addresses.IsConfigured)
            throw new InvalidOperationException("Tenants cannot be created because no tenant base domain is configured.");

        string name = request.Name.Trim();
        string? description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (name.Length is 0 or > NameMaximumLength)
            return new(CreateOwnTenantStatus.Invalid, Error: $"A name of 1 to {NameMaximumLength} characters is required.");
        if (description?.Length > DescriptionMaximumLength)
            return new(CreateOwnTenantStatus.Invalid, Error: $"The description can have at most {DescriptionMaximumLength} characters.");

        TenantSubdomainRejection rejection = TenantSubdomain.TryCreate(request.Subdomain, out TenantSubdomain? subdomain);
        if (subdomain is null)
            return new(rejection == TenantSubdomainRejection.Reserved ? CreateOwnTenantStatus.AddressReserved : CreateOwnTenantStatus.AddressMalformed);
        string hostname = subdomain.ToHostname(addresses.TenantBaseDomain!);

        (Guid tenantId, Guid ownerId) = DeriveIdentifiers(request.CreatorUserId, request.IdempotencyKey);
        if (await FindReplayAsync(tenantId, name, description, hostname, cancellationToken) is { } replay)
            return replay;

        TenantCreator? creator = await creators.FindAsync(request.CreatorUserId, cancellationToken);
        if (creator is null)
            return await DeniedAsync(CreateOwnTenantStatus.CreatorNotFound, request, tenantId, SecurityAuditReasonCatalog.UserNotFound, cancellationToken);
        if (!creator.HasVerifiedPhone)
            return await DeniedAsync(CreateOwnTenantStatus.PhoneNotVerified, request, tenantId, SecurityAuditReasonCatalog.Unauthorized, cancellationToken);

        DateTime since = timeProvider.GetUtcNow().UtcDateTime - CreationWindow;
        if (await tenants.CountCreatedByAsync(creator.UserId, since, cancellationToken) >= CreationsPerWindow)
            return await DeniedAsync(CreateOwnTenantStatus.LimitReached, request, tenantId, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);

        TenantProvisioningOutcome outcome = await tenants.CreateAsync(
            new NewSelfServiceTenant(tenantId, ownerId, creator.UserId, name, description, hostname, profile),
            cancellationToken);
        if (outcome == TenantProvisioningOutcome.AddressTaken)
        {
            // A concurrent identical request may have created this very tenant first.
            return await FindReplayAsync(tenantId, name, description, hostname, cancellationToken)
                ?? await DeniedAsync(CreateOwnTenantStatus.AddressTaken, request, tenantId, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
        }

        await WriteAuditAsync(request, tenantId, SecurityAuditOutcome.Succeeded, null, CancellationToken.None);
        return new(CreateOwnTenantStatus.Created, tenantId, hostname);
    }

    private async Task<CreateOwnTenantResult?> FindReplayAsync(Guid tenantId, string name, string? description, string hostname, CancellationToken cancellationToken)
    {
        if (await tenants.FindAsync(tenantId, cancellationToken) is not { } existing)
            return null;
        return existing.Name == name && existing.Description == description && existing.PrimaryHostname == hostname
            ? new(CreateOwnTenantStatus.Replayed, tenantId, hostname)
            : new(CreateOwnTenantStatus.IdempotencyConflict);
    }

    /// <summary>
    /// The same creator and idempotency key always name the same tenant and owner, so a
    /// retried request finds what the first attempt created instead of creating another.
    /// </summary>
    internal static (Guid TenantId, Guid OwnerId) DeriveIdentifiers(string creatorUserId, string idempotencyKey)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{PartiesSecurityAuditEvents.SelfServiceTenantCreationOperation}\n{creatorUserId}\n{idempotencyKey}"));
        return (new Guid(hash.AsSpan(0, 16)), new Guid(hash.AsSpan(16, 16)));
    }

    private async Task<CreateOwnTenantResult> DeniedAsync(CreateOwnTenantStatus status, CreateOwnTenantCommand request, Guid tenantId, string reason, CancellationToken cancellationToken)
    {
        await WriteAuditAsync(request, tenantId, SecurityAuditOutcome.Denied, reason, cancellationToken);
        return new(status);
    }

    private async Task WriteAuditAsync(CreateOwnTenantCommand request, Guid tenantId, string outcome, string? reason, CancellationToken cancellationToken)
    {
        string operationKey = $"{tenantId:N}";
        try
        {
            _ = await securityAuditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = PartiesSecurityAuditEvents.SelfServiceTenantCreation,
                EventCategory = SecurityAuditEventCatalog.ManagementCategory,
                Outcome = outcome,
                ActorId = request.CreatorUserId,
                SubjectId = request.CreatorUserId,
                // Denials stay with the tenant the request came from; a created tenant owns its own record.
                TenantId = outcome == SecurityAuditOutcome.Succeeded ? tenantId.ToString() : requestContext.TenantId ?? tenantId.ToString(),
                Resource = PartiesSecurityAuditEvents.TenantResourcePrefix + tenantId,
                Operation = PartiesSecurityAuditEvents.SelfServiceTenantCreationOperation,
                OccurredAt = timeProvider.GetUtcNow().UtcDateTime,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? operationKey,
                SourceBoundary = PartiesSecurityAuditEvents.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = $"{operationKey}:{outcome}{PartiesSecurityAuditEvents.SelfServiceTenantCreationIdempotencySuffix}"
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the creation result when the mandatory audit store is unavailable.
        }
    }
}
