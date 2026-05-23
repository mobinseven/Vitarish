using Vitarish.Requests.Application.Abstractions;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Appointments.Domain.Rules;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftReminderPolicyManager(
    IMediator mediator,
    IAuditWriter auditWriter,
    IRequestContext requestContext)
    : EntityService<ShiftReminderPolicy, ShiftReminderPolicyDescriptor, ShiftReminderPolicyReadDto, ShiftReminderPolicyWriteDto, ShiftReminderPolicyDescriptorDto>(
        mediator,
        policy => policy.ToDto(),
        dto => dto.ToDomain(),
        dto => dto.ToDomain()),
      IShiftReminderPolicyManager
{
    public override async Task<EntityResult<ShiftReminderPolicyReadDto>> AddAsync(
        ShiftReminderPolicyWriteDto dto,
        CancellationToken cancellationToken)
    {
        EntityResult validationResult = ShiftReminderPolicyRules.Validate(dto);
        if (validationResult.IsFailure)
        {
            return EntityResult<ShiftReminderPolicyReadDto>.Failure([.. validationResult.Errors]);
        }

        EntityResult<ShiftReminderPolicyReadDto> result = await base.AddAsync(dto, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            await auditWriter.WriteAsync(CreateEntry("Created", result.Value.Id), cancellationToken);
        }

        return result;
    }

    public override async Task<EntityResult<ShiftReminderPolicyReadDto>> UpdateAsync(
        ShiftReminderPolicyWriteDto dto,
        CancellationToken cancellationToken)
    {
        EntityResult validationResult = ShiftReminderPolicyRules.Validate(dto);
        if (validationResult.IsFailure)
        {
            return EntityResult<ShiftReminderPolicyReadDto>.Failure([.. validationResult.Errors]);
        }

        EntityResult<ShiftReminderPolicyReadDto> result = await base.UpdateAsync(dto, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            await auditWriter.WriteAsync(CreateEntry("Updated", result.Value.Id), cancellationToken);
        }

        return result;
    }

    public override async Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        EntityResult result = await base.RemoveAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            await auditWriter.WriteAsync(CreateEntry("Removed", id), cancellationToken);
        }

        return result;
    }

    private AuditEntry CreateEntry(string action, Guid id) => new()
    {
        Action = action,
        ActorId = requestContext.UserId,
        EntityType = nameof(ShiftReminderPolicy),
        EntityId = id,
        OccurredAt = DateTimeOffset.UtcNow,
        TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.")
    };
}
