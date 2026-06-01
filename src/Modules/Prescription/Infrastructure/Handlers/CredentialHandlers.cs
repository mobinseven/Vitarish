using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Application.Commands;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;

namespace Vitarish.Prescription.Infrastructure.Handlers;

public sealed class SaveCredentialHandler(
    PrescriptionDbContext dbContext,
    IAuditWriter auditWriter,
    ILogger<SaveCredentialHandler> logger)
    : IRequestHandler<SaveCredentialCommand, SaveCredentialResult>
{
    public async Task<SaveCredentialResult> Handle(SaveCredentialCommand request, CancellationToken cancellationToken)
    {
        DoctorProviderCredential? credential;

        if (request.CredentialId.HasValue)
        {
            credential = await dbContext.DoctorProviderCredentials
                .FirstOrDefaultAsync(c => c.Id == request.CredentialId.Value, cancellationToken);

            if (credential is null)
                return SaveCredentialResult.Failure("اطلاعات اعتبارنامه یافت نشد.");

            credential.ProviderDoctorIdentifier = request.ProviderDoctorIdentifier;
            credential.ProviderUsername = request.ProviderUsername;
            credential.CredentialSecretRef = request.CredentialSecretRef;
            credential.Status = CredentialStatus.Pending;
            credential.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            // Check uniqueness
            var exists = await dbContext.DoctorProviderCredentials.AnyAsync(
                c => c.DoctorId == request.DoctorId
                     && c.InsuranceProviderId == request.InsuranceProviderId
                     && c.ClinicId == request.ClinicId,
                cancellationToken);

            if (exists)
                return SaveCredentialResult.Failure("اعتبارنامه برای این پزشک و بیمه‌گر قبلاً ثبت شده است.");

            credential = new DoctorProviderCredential
            {
                Id = Guid.NewGuid(),
                DoctorId = request.DoctorId,
                InsuranceProviderId = request.InsuranceProviderId,
                ClinicId = request.ClinicId,
                ProviderDoctorIdentifier = request.ProviderDoctorIdentifier,
                ProviderUsername = request.ProviderUsername,
                CredentialSecretRef = request.CredentialSecretRef,
                Status = CredentialStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.DoctorProviderCredentials.Add(credential);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(DoctorProviderCredential),
            EntityId = credential.Id,
            Action = request.CredentialId.HasValue ? "Updated" : AuditAction.Created,
            NewState = credential.Status.ToString(),
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        logger.LogInformation("Credential {CredentialId} saved for doctor {DoctorId}", credential.Id, request.DoctorId);
        return SaveCredentialResult.Success(credential.Id);
    }
}

public sealed class TestCredentialHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    IAuditWriter auditWriter,
    ILogger<TestCredentialHandler> logger)
    : IRequestHandler<TestCredentialCommand, TestCredentialResult>
{
    public async Task<TestCredentialResult> Handle(TestCredentialCommand request, CancellationToken cancellationToken)
    {
        var credential = await dbContext.DoctorProviderCredentials
            .FirstOrDefaultAsync(c => c.Id == request.CredentialId, cancellationToken);

        if (credential is null)
            return TestCredentialResult.Failure("اعتبارنامه یافت نشد.");

        var provider = await dbContext.InsuranceProviders
            .FirstOrDefaultAsync(p => p.Id == credential.InsuranceProviderId, cancellationToken);

        if (provider is null)
            return TestCredentialResult.Failure("بیمه‌گر یافت نشد.");

        var adapter = providerRegistry.GetAdapter(provider.ProviderKey);
        if (adapter is null)
            return TestCredentialResult.Failure("آداپتور بیمه‌گر در دسترس نیست.");

        var authResult = await adapter.AuthenticateAsync(new AuthenticateRequest
        {
            DoctorId = credential.DoctorId,
            ClinicId = credential.ClinicId
        }, cancellationToken);

        if (authResult.IsSuccess)
        {
            credential.Status = CredentialStatus.Active;
            credential.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditWriter.WriteAsync(new AuditEntry
            {
                EntityType = nameof(DoctorProviderCredential),
                EntityId = credential.Id,
                Action = "TestSucceeded",
                NewState = CredentialStatus.Active.ToString(),
                OccurredAt = DateTimeOffset.UtcNow
            }, cancellationToken);

            logger.LogInformation("Credential {CredentialId} test succeeded", credential.Id);
            return TestCredentialResult.Success(CredentialStatus.Active);
        }

        credential.Status = CredentialStatus.Pending;
        credential.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(DoctorProviderCredential),
            EntityId = credential.Id,
            Action = "TestFailed",
            NewState = CredentialStatus.Pending.ToString(),
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        return TestCredentialResult.Failure(authResult.Error?.UserMessage ?? "احراز هویت ناموفق بود.");
    }
}

public sealed class RevokeCredentialHandler(
    PrescriptionDbContext dbContext,
    IAuditWriter auditWriter,
    ILogger<RevokeCredentialHandler> logger)
    : IRequestHandler<RevokeCredentialCommand, RevokeCredentialResult>
{
    public async Task<RevokeCredentialResult> Handle(RevokeCredentialCommand request, CancellationToken cancellationToken)
    {
        var credential = await dbContext.DoctorProviderCredentials
            .FirstOrDefaultAsync(c => c.Id == request.CredentialId, cancellationToken);

        if (credential is null)
            return RevokeCredentialResult.Failure("اعتبارنامه یافت نشد.");

        credential.Status = CredentialStatus.Revoked;
        credential.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(DoctorProviderCredential),
            EntityId = credential.Id,
            Action = "Revoked",
            NewState = CredentialStatus.Revoked.ToString(),
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        logger.LogInformation("Credential {CredentialId} revoked", credential.Id);
        return RevokeCredentialResult.Success();
    }
}

public sealed class GetCredentialSetupGridHandler(
    PrescriptionDbContext dbContext)
    : IRequestHandler<GetCredentialSetupGridQuery, IReadOnlyList<CredentialSetupDto>>
{
    public async Task<IReadOnlyList<CredentialSetupDto>> Handle(GetCredentialSetupGridQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.DoctorProviderCredentials
            .Where(c => c.ClinicId == request.ClinicId);

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            var providerId = await dbContext.InsuranceProviders
                .Where(p => p.ProviderKey == request.ProviderKey)
                .Select(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (providerId != Guid.Empty)
                query = query.Where(c => c.InsuranceProviderId == providerId);
        }

        var credentials = await query
            .Join(dbContext.InsuranceProviders,
                c => c.InsuranceProviderId,
                p => p.Id,
                (c, p) => new { Credential = c, Provider = p })
            .Select(x => new CredentialSetupDto
            {
                CredentialId = x.Credential.Id,
                DoctorId = x.Credential.DoctorId,
                DoctorName = x.Credential.ProviderUsername ?? x.Credential.DoctorId.ToString(),
                InsuranceProviderId = x.Credential.InsuranceProviderId,
                ProviderKey = x.Provider.ProviderKey,
                ProviderName = x.Provider.Name,
                ClinicId = x.Credential.ClinicId,
                ProviderDoctorIdentifier = x.Credential.ProviderDoctorIdentifier,
                ProviderUsername = x.Credential.ProviderUsername,
                Status = x.Credential.Status,
                ExpiresAt = x.Credential.ExpiresAt,
                DaysUntilExpiry = x.Credential.ExpiresAt.HasValue
                    ? (int)(x.Credential.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays
                    : 999,
                IsExpiringSoon = x.Credential.ExpiresAt.HasValue
                    && (x.Credential.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays <= 14,
                LastTestedAt = x.Credential.UpdatedAt
            })
            .OrderBy(x => x.ProviderKey)
            .ThenBy(x => x.DoctorName)
            .ToListAsync(cancellationToken);

        return credentials;
    }
}
