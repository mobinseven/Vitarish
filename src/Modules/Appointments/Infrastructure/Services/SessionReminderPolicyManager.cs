using Vitarish.Requests.Application.Abstractions;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionReminderPolicyManager(
    ISessionAggregateLoader aggregateLoader,
    IAuditWriter auditWriter,
    IRequestContext requestContext) : ISessionReminderPolicyManager
{
    private const string PolicyEntityType = nameof(SessionReminderPolicy);
    private const string SessionNotFound = "SessionReminderPolicySessionNotFoundError";
    private const string PolicyNotFound = "SessionReminderPolicyNotFoundError";
    private const string OffsetBeforePositive = "SessionReminderPolicyOffsetBeforePositiveError";
    private const string QualifyingStatusesRequired = "SessionReminderPolicyQualifyingStatusesRequiredError";
    private const string SubjectTemplateRequired = "SessionReminderPolicySubjectTemplateRequiredError";
    private const string BodyTemplateRequired = "SessionReminderPolicyBodyTemplateRequiredError";

    public async Task<IReadOnlyList<SessionReminderPolicyReadDto>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        Session? session = await aggregateLoader.LoadWithPoliciesAsync(sessionId, cancellationToken);
        return session is null ? [] : [.. session.ReminderPolicies.Select(p => p.ToDto())];
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

        Session? session = await aggregateLoader.LoadWithPoliciesAsync(dto.SessionId, cancellationToken);
        if (session is null)
        {
            return EntityResult<SessionReminderPolicyReadDto>.Failure(
                EntityError.NotFound(SessionNotFound, nameof(dto.SessionId)));
        }

        SessionReminderPolicy policy = dto.ToDomain();
        policy.SessionId = dto.SessionId;
        session.ReminderPolicies.Add(policy);

        await aggregateLoader.SaveAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Action = "Created",
            ActorId = requestContext.UserId,
            EntityType = PolicyEntityType,
            EntityId = policy.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.")
        }, cancellationToken);

        return EntityResult<SessionReminderPolicyReadDto>.Success(policy.ToDto());
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

        Session? session = await aggregateLoader.LoadWithPoliciesAsync(dto.SessionId, cancellationToken);
        if (session is null)
        {
            return EntityResult<SessionReminderPolicyReadDto>.Failure(
                EntityError.NotFound(SessionNotFound, nameof(dto.SessionId)));
        }

        SessionReminderPolicy? existing = session.ReminderPolicies.FirstOrDefault(p => p.Id == dto.Id);
        if (existing is null)
        {
            return EntityResult<SessionReminderPolicyReadDto>.Failure(
                EntityError.NotFound(PolicyNotFound, nameof(dto.Id)));
        }

        existing.IsEnabled = dto.IsEnabled;
        existing.Channel = dto.Channel;
        existing.OffsetBefore = dto.OffsetBefore;
        existing.QualifyingStatuses = dto.QualifyingStatuses;
        existing.SubjectTemplate = dto.SubjectTemplate;
        existing.BodyTemplate = dto.BodyTemplate;

        await aggregateLoader.SaveAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Action = "Updated",
            ActorId = requestContext.UserId,
            EntityType = PolicyEntityType,
            EntityId = existing.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.")
        }, cancellationToken);

        return EntityResult<SessionReminderPolicyReadDto>.Success(existing.ToDto());
    }

    public async Task<EntityResult> RemovePolicyAsync(Guid sessionId, Guid policyId, CancellationToken cancellationToken)
    {
        Session? session = await aggregateLoader.LoadWithPoliciesAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return EntityResult.Failure(EntityError.NotFound(SessionNotFound, nameof(sessionId)));
        }

        SessionReminderPolicy? existing = session.ReminderPolicies.FirstOrDefault(p => p.Id == policyId);
        if (existing is null)
        {
            return EntityResult.Failure(EntityError.NotFound(PolicyNotFound, nameof(policyId)));
        }

        session.ReminderPolicies.Remove(existing);

        await aggregateLoader.SaveAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Action = "Removed",
            ActorId = requestContext.UserId,
            EntityType = PolicyEntityType,
            EntityId = policyId,
            OccurredAt = DateTimeOffset.UtcNow,
            TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.")
        }, cancellationToken);

        return EntityResult.Success();
    }

    private static EntityResult Validate(SessionReminderPolicyWriteDto dto)
    {
        List<EntityError> errors = [];

        if (dto.OffsetBefore <= TimeSpan.Zero)
        {
            errors.Add(EntityError.Validation(
                OffsetBeforePositive,
                new Dictionary<string, string> { [nameof(dto.OffsetBefore)] = OffsetBeforePositive }));
        }

        if (dto.QualifyingStatuses.Count == 0)
        {
            errors.Add(EntityError.Validation(
                QualifyingStatusesRequired,
                new Dictionary<string, string> { [nameof(dto.QualifyingStatuses)] = QualifyingStatusesRequired }));
        }

        if (string.IsNullOrWhiteSpace(dto.SubjectTemplate))
        {
            errors.Add(EntityError.Validation(
                SubjectTemplateRequired,
                new Dictionary<string, string> { [nameof(dto.SubjectTemplate)] = SubjectTemplateRequired }));
        }

        if (string.IsNullOrWhiteSpace(dto.BodyTemplate))
        {
            errors.Add(EntityError.Validation(
                BodyTemplateRequired,
                new Dictionary<string, string> { [nameof(dto.BodyTemplate)] = BodyTemplateRequired }));
        }

        return errors.Count == 0 ? EntityResult.Success() : EntityResult.Failure([.. errors]);
    }
}
