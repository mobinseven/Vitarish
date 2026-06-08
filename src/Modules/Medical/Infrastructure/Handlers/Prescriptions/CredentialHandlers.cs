using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Medical.Application.Abstractions;
using Vitarish.Medical.Application.Commands;
using Vitarish.Medical.Domain.Entities;
using Vitarish.Medical.Domain.Enums;
using Vitarish.Medical.Infrastructure.Data;

namespace Vitarish.Medical.Infrastructure.Handlers.Prescriptions;

public sealed class SaveCredentialHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    IAuditWriter auditWriter,
    ILogger<SaveCredentialHandler> logger)
    : IRequestHandler<SaveCredentialCommand, SaveCredentialResult>
{
    public async Task<SaveCredentialResult> Handle(SaveCredentialCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderKey))
        {
            return SaveCredentialResult.Failure("بیمه‌گر انتخاب شده در سیستم موجود نیست.");
        }

        string providerKey = request.ProviderKey.Trim();
        if (!providerRegistry.IsProviderAvailable(providerKey))
        {
            return SaveCredentialResult.Failure("بیمه‌گر انتخاب شده در سیستم موجود نیست.");
        }

        DoctorProviderCredential? credential;

        if (request.CredentialId.HasValue)
        {
            credential = await dbContext.DoctorProviderCredentials
                .FirstOrDefaultAsync(c => c.Id == request.CredentialId.Value, cancellationToken);

            if (credential is null)
            {
                return SaveCredentialResult.Failure("اطلاعات اعتبارنامه یافت نشد.");
            }

            credential.ProviderDoctorIdentifier = request.ProviderDoctorIdentifier;
            credential.ProviderUsername = request.ProviderUsername;
            credential.ProviderClinicIdentifier = request.ProviderClinicIdentifier;
            credential.CredentialSecretRef = request.CredentialSecretRef;
            credential.Status = CredentialStatus.Pending;
            credential.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Check uniqueness
            bool exists = await dbContext.DoctorProviderCredentials.AnyAsync(
                c => c.DoctorId == request.DoctorId
                     && c.ProviderKey == providerKey
                     && c.ClinicId == request.ClinicId,
                cancellationToken);

            if (exists)
            {
                return SaveCredentialResult.Failure("اعتبارنامه برای این پزشک و بیمه‌گر قبلاً ثبت شده است.");
            }

            credential = new DoctorProviderCredential
            {
                Id = Guid.NewGuid(),
                DoctorId = request.DoctorId,
                ProviderKey = providerKey,
                ClinicId = request.ClinicId,
                ProviderDoctorIdentifier = request.ProviderDoctorIdentifier,
                ProviderUsername = request.ProviderUsername,
                ProviderClinicIdentifier = request.ProviderClinicIdentifier,
                CredentialSecretRef = request.CredentialSecretRef,
                Status = CredentialStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _ = dbContext.DoctorProviderCredentials.Add(credential);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(DoctorProviderCredential),
            EntityId = credential.Id,
            Action = request.CredentialId.HasValue ? "Updated" : AuditAction.Created,
            NewState = credential.Status.ToString(),
            OccurredAt = DateTime.UtcNow
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
        DoctorProviderCredential? credential = await dbContext.DoctorProviderCredentials
            .FirstOrDefaultAsync(c => c.Id == request.CredentialId, cancellationToken);

        if (credential is null)
        {
            return TestCredentialResult.Failure("اعتبارنامه یافت نشد.");
        }

        IInsuranceProviderAdapter? adapter = providerRegistry.GetAdapter(credential.ProviderKey);
        if (adapter is null)
        {
            return TestCredentialResult.Failure("آداپتور بیمه‌گر در دسترس نیست.");
        }

        AdapterResult authResult = await adapter.AuthenticateAsync(new AuthenticateRequest
        {
            DoctorId = credential.DoctorId,
            ClinicId = credential.ClinicId
        }, cancellationToken);

        if (authResult.IsSuccess)
        {
            credential.Status = CredentialStatus.Active;
            credential.UpdatedAt = DateTime.UtcNow;
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            await auditWriter.WriteAsync(new AuditEntry
            {
                EntityType = nameof(DoctorProviderCredential),
                EntityId = credential.Id,
                Action = "TestSucceeded",
                NewState = CredentialStatus.Active.ToString(),
                OccurredAt = DateTime.UtcNow
            }, cancellationToken);

            logger.LogInformation("Credential {CredentialId} test succeeded", credential.Id);
            return TestCredentialResult.Success(CredentialStatus.Active);
        }

        credential.Status = CredentialStatus.Pending;
        credential.UpdatedAt = DateTime.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(DoctorProviderCredential),
            EntityId = credential.Id,
            Action = "TestFailed",
            NewState = CredentialStatus.Pending.ToString(),
            OccurredAt = DateTime.UtcNow
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
        DoctorProviderCredential? credential = await dbContext.DoctorProviderCredentials
            .FirstOrDefaultAsync(c => c.Id == request.CredentialId, cancellationToken);

        if (credential is null)
        {
            return RevokeCredentialResult.Failure("اعتبارنامه یافت نشد.");
        }

        credential.Status = CredentialStatus.Revoked;
        credential.UpdatedAt = DateTime.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(DoctorProviderCredential),
            EntityId = credential.Id,
            Action = "Revoked",
            NewState = CredentialStatus.Revoked.ToString(),
            OccurredAt = DateTime.UtcNow
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
        IQueryable<DoctorProviderCredential> query = dbContext.DoctorProviderCredentials
            .Where(c => c.ClinicId == request.ClinicId);

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            query = query.Where(c => c.ProviderKey == request.ProviderKey);
        }

        DateTime now = DateTime.UtcNow;
        List<CredentialSetupDto> credentials = await query
            .Select(c => new CredentialSetupDto
            {
                CredentialId = c.Id,
                DoctorId = c.DoctorId,
                DoctorName = c.ProviderUsername ?? c.DoctorId.ToString(),
                ProviderKey = c.ProviderKey,
                ClinicId = c.ClinicId,
                ProviderDoctorIdentifier = c.ProviderDoctorIdentifier,
                ProviderUsername = c.ProviderUsername,
                ProviderClinicIdentifier = c.ProviderClinicIdentifier,
                Status = c.Status,
                ExpiresAt = c.ExpiresAt,
                DaysUntilExpiry = c.ExpiresAt.HasValue
                    ? (int)(c.ExpiresAt.Value - now).TotalDays
                    : 999,
                IsExpiringSoon = c.ExpiresAt.HasValue
                    && (c.ExpiresAt.Value - now).TotalDays <= 14,
                LastTestedAt = c.UpdatedAt
            })
            .OrderBy(x => x.ProviderKey)
            .ThenBy(x => x.DoctorName)
            .ToListAsync(cancellationToken);

        return credentials;
    }
}
