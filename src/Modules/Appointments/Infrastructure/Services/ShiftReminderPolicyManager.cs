using Vitarish.Requests.Application.Abstractions;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftReminderPolicyManager(
    IShiftAggregateLoader aggregateLoader,
    IAuditWriter auditWriter,
    IRequestContext requestContext) : IShiftReminderPolicyManager
{
    private const string PolicyEntityType = nameof(ShiftReminderPolicy);
    private const string ShiftNotFound = "ShiftReminderPolicyShiftNotFoundError";
    private const string PolicyNotFound = "ShiftReminderPolicyNotFoundError";
    private const string OffsetBeforePositive = "ShiftReminderPolicyOffsetBeforePositiveError";
    private const string QualifyingStatusesRequired = "ShiftReminderPolicyQualifyingStatusesRequiredError";
    private const string SubjectTemplateRequired = "ShiftReminderPolicySubjectTemplateRequiredError";
    private const string BodyTemplateRequired = "ShiftReminderPolicyBodyTemplateRequiredError";

    public async Task<IReadOnlyList<ShiftReminderPolicyReadDto>> GetByShiftAsync(
        Guid shiftId,
        CancellationToken cancellationToken)
    {
        Shift? shift = await aggregateLoader.LoadWithPoliciesAsync(shiftId, cancellationToken);
        return shift is null ? [] : [.. shift.ReminderPolicies.Select(p => p.ToDto())];
    }

    public async Task<EntityResult<ShiftReminderPolicyReadDto>> AddPolicyAsync(
        ShiftReminderPolicyWriteDto dto,
        CancellationToken cancellationToken)
    {
        EntityResult validationResult = Validate(dto);
        if (validationResult.IsFailure)
        {
            return EntityResult<ShiftReminderPolicyReadDto>.Failure([.. validationResult.Errors]);
        }

        Shift? shift = await aggregateLoader.LoadWithPoliciesAsync(dto.ShiftId, cancellationToken);
        if (shift is null)
        {
            return EntityResult<ShiftReminderPolicyReadDto>.Failure(
                EntityError.NotFound(ShiftNotFound, nameof(dto.ShiftId)));
        }

        ShiftReminderPolicy policy = dto.ToDomain();
        policy.ShiftId = dto.ShiftId;
        shift.ReminderPolicies.Add(policy);

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

        return EntityResult<ShiftReminderPolicyReadDto>.Success(policy.ToDto());
    }

    public async Task<EntityResult<ShiftReminderPolicyReadDto>> UpdatePolicyAsync(
        ShiftReminderPolicyWriteDto dto,
        CancellationToken cancellationToken)
    {
        EntityResult validationResult = Validate(dto);
        if (validationResult.IsFailure)
        {
            return EntityResult<ShiftReminderPolicyReadDto>.Failure([.. validationResult.Errors]);
        }

        Shift? shift = await aggregateLoader.LoadWithPoliciesAsync(dto.ShiftId, cancellationToken);
        if (shift is null)
        {
            return EntityResult<ShiftReminderPolicyReadDto>.Failure(
                EntityError.NotFound(ShiftNotFound, nameof(dto.ShiftId)));
        }

        ShiftReminderPolicy? existing = shift.ReminderPolicies.FirstOrDefault(p => p.Id == dto.Id);
        if (existing is null)
        {
            return EntityResult<ShiftReminderPolicyReadDto>.Failure(
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

        return EntityResult<ShiftReminderPolicyReadDto>.Success(existing.ToDto());
    }

    public async Task<EntityResult> RemovePolicyAsync(Guid shiftId, Guid policyId, CancellationToken cancellationToken)
    {
        Shift? shift = await aggregateLoader.LoadWithPoliciesAsync(shiftId, cancellationToken);
        if (shift is null)
        {
            return EntityResult.Failure(EntityError.NotFound(ShiftNotFound, nameof(shiftId)));
        }

        ShiftReminderPolicy? existing = shift.ReminderPolicies.FirstOrDefault(p => p.Id == policyId);
        if (existing is null)
        {
            return EntityResult.Failure(EntityError.NotFound(PolicyNotFound, nameof(policyId)));
        }

        shift.ReminderPolicies.Remove(existing);

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

    private static EntityResult Validate(ShiftReminderPolicyWriteDto dto)
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
