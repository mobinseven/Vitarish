using Vitarish.Requests.Application.Abstractions;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionReminderPolicyManager(
    ISessionReminderPolicyRepository repository,
    IAuditWriter auditWriter,
    IRequestContext requestContext) : ISessionReminderPolicyManager
{
    private const string PolicyEntityType = nameof(SessionReminderPolicy);

    public async Task<IReadOnlyList<SessionReminderPolicyReadDto>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SessionReminderPolicy> policies = await repository.GetBySessionIdAsync(sessionId, cancellationToken);
        return [.. policies.Select(p => p.ToDto())];
    }

    public async Task<EntityResult<SessionReminderPolicyReadDto>> AddPolicyAsync(
        SessionReminderPolicyWriteDto dto,
        CancellationToken cancellationToken)
    {
        EntityResult validationResult = Validate(dto);
        if (validationResult.IsFailure)
        {
            return EntityResult<SessionReminderPolicyReadDto>.Failure([.. validationResult.Errors]);
        }

        SessionReminderPolicy policy = dto.ToDomain();
        policy.SessionId = dto.SessionId;

        SessionReminderPolicy created = await repository.AddAsync(policy, cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Action = "Created",
            ActorId = requestContext.UserId,
            EntityType = PolicyEntityType,
            EntityId = created.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID can not be recognized.")
        }, cancellationToken);

        return EntityResult<SessionReminderPolicyReadDto>.Success(created.ToDto());
    }

    public async Task<EntityResult<SessionReminderPolicyReadDto>> UpdatePolicyAsync(
        SessionReminderPolicyWriteDto dto,
        CancellationToken cancellationToken)
    {
        EntityResult validationResult = Validate(dto);
        if (validationResult.IsFailure)
        {
            return EntityResult<SessionReminderPolicyReadDto>.Failure([.. validationResult.Errors]);
        }

        SessionReminderPolicy? existing = await repository.GetAsync(dto.Id, cancellationToken);
        if (existing is null)
        {
            return EntityResult<SessionReminderPolicyReadDto>.Failure(
                EntityError.NotFound("Reminder policy not found.", nameof(dto.Id)));
        }

        existing.IsEnabled = dto.IsEnabled;
        existing.Channel = dto.Channel;
        existing.OffsetBefore = dto.OffsetBefore;
        existing.QualifyingStatuses = dto.QualifyingStatuses;
        existing.SubjectTemplate = dto.SubjectTemplate;
        existing.BodyTemplate = dto.BodyTemplate;

        SessionReminderPolicy updated = await repository.UpdateAsync(existing, cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Action = "Updated",
            ActorId = requestContext.UserId,
            EntityType = PolicyEntityType,
            EntityId = updated.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID can not be recognized.")
        }, cancellationToken);

        return EntityResult<SessionReminderPolicyReadDto>.Success(updated.ToDto());
    }

    public async Task<EntityResult> RemovePolicyAsync(Guid policyId, CancellationToken cancellationToken)
    {
        SessionReminderPolicy? existing = await repository.GetAsync(policyId, cancellationToken);
        if (existing is null)
        {
            return EntityResult.Failure(EntityError.NotFound("Reminder policy not found.", nameof(policyId)));
        }

        await repository.RemoveAsync(existing, cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Action = "Removed",
            ActorId = requestContext.UserId,
            EntityType = PolicyEntityType,
            EntityId = policyId,
            OccurredAt = DateTimeOffset.UtcNow,
            TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID can not be recognized.")
        }, cancellationToken);

        return EntityResult.Success();
    }

    private static EntityResult Validate(SessionReminderPolicyWriteDto dto)
    {
        List<EntityError> errors = [];

        if (dto.OffsetBefore <= TimeSpan.Zero)
        {
            errors.Add(EntityError.Validation(
                "Offset before must be a positive duration.",
                new Dictionary<string, string> { [nameof(dto.OffsetBefore)] = "Must be positive." }));
        }

        if (dto.QualifyingStatuses.Count == 0)
        {
            errors.Add(EntityError.Validation(
                "At least one qualifying status is required.",
                new Dictionary<string, string> { [nameof(dto.QualifyingStatuses)] = "Required." }));
        }

        if (string.IsNullOrWhiteSpace(dto.SubjectTemplate))
        {
            errors.Add(EntityError.Validation(
                "Subject template is required.",
                new Dictionary<string, string> { [nameof(dto.SubjectTemplate)] = "Required." }));
        }

        if (string.IsNullOrWhiteSpace(dto.BodyTemplate))
        {
            errors.Add(EntityError.Validation(
                "Body template is required.",
                new Dictionary<string, string> { [nameof(dto.BodyTemplate)] = "Required." }));
        }

        return errors.Count == 0 ? EntityResult.Success() : EntityResult.Failure([.. errors]);
    }
}
