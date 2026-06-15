using Vitarish.Appointments.Domain.Mappers;
using Vitarish.Appointments.Domain.Rules;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftReminderPolicyManager(
    IMediator mediator,
    IAuditWriter auditWriter,
    IRequestContext requestContext)
    : EntityService<ShiftReminderPolicy, ShiftReminderPolicyDescriptor, ShiftReminderPolicyReadDto, ShiftReminderPolicyWriteDto, ShiftReminderPolicyDescriptorDto>(
        mediator,
        policy => policy.ToRead(),
        dto => dto.ToEntity(),
        dto => dto.ToDescriptor()),
      IShiftReminderPolicyManager
{
    async Task<EntityResult<ShiftReminderPolicyReadDto>> IEntityService<ShiftReminderPolicyReadDto, ShiftReminderPolicyWriteDto, ShiftReminderPolicyDescriptorDto>.AddAsync(
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

    async Task<EntityResult<ShiftReminderPolicyReadDto>> IEntityService<ShiftReminderPolicyReadDto, ShiftReminderPolicyWriteDto, ShiftReminderPolicyDescriptorDto>.UpdateAsync(
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

    async Task<EntityResult> IEntityService<ShiftReminderPolicyReadDto, ShiftReminderPolicyWriteDto, ShiftReminderPolicyDescriptorDto>.RemoveAsync(
        Guid id,
        CancellationToken cancellationToken)
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
        OccurredAt = DateTime.UtcNow,
        TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.")
    };
}
