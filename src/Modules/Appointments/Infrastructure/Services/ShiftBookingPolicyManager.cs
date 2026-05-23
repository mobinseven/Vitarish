using Vitarish.Appointments.Domain.Rules;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftBookingPolicyManager(
    IMediator mediator,
    IEntityRepository<Shift, ShiftDescriptor> shiftRepository,
    IEntityRepository<ShiftBookingPolicy, ShiftBookingPolicyDescriptor> policyRepository)
    : EntityService<ShiftBookingPolicy, ShiftBookingPolicyDescriptor, ShiftBookingPolicyReadDto, ShiftBookingPolicyWriteDto, ShiftBookingPolicyDescriptorDto>(
        mediator,
        policy => policy.ToDto(),
        dto => dto.ToDomain(),
        dto => dto.ToDomain()),
      IShiftBookingPolicyManager
{
    async Task<EntityResult<ShiftBookingPolicyReadDto>> IEntityService<ShiftBookingPolicyReadDto, ShiftBookingPolicyWriteDto, ShiftBookingPolicyDescriptorDto>.AddAsync(
        ShiftBookingPolicyWriteDto dto,
        CancellationToken cancellationToken)
    {
        ShiftBookingPolicyViolation? violation = await ValidateWriteAsync(dto, null, cancellationToken);
        if (violation is not null)
        {
            return EntityResult<ShiftBookingPolicyReadDto>.Failure(
                EntityError.Validation(violation.Code.ToString(), violation.Field));
        }

        return await base.AddAsync(dto, cancellationToken);
    }

    async Task<EntityResult<ShiftBookingPolicyReadDto>> IEntityService<ShiftBookingPolicyReadDto, ShiftBookingPolicyWriteDto, ShiftBookingPolicyDescriptorDto>.UpdateAsync(
        ShiftBookingPolicyWriteDto dto,
        CancellationToken cancellationToken)
    {
        ShiftBookingPolicyViolation? violation = await ValidateWriteAsync(dto, dto.Id, cancellationToken);
        if (violation is not null)
        {
            return EntityResult<ShiftBookingPolicyReadDto>.Failure(
                EntityError.Validation(violation.Code.ToString(), violation.Field));
        }

        return await base.UpdateAsync(dto, cancellationToken);
    }

    private async Task<ShiftBookingPolicyViolation?> ValidateWriteAsync(
        ShiftBookingPolicyWriteDto dto,
        Guid? candidateId,
        CancellationToken cancellationToken)
    {
        Shift? shift = await shiftRepository.GetAsync(dto.ShiftId, cancellationToken);
        if (shift is null)
        {
            return new ShiftBookingPolicyViolation(ShiftBookingPolicyErrorCode.ShiftNotFound, nameof(dto.ShiftId));
        }

        IReadOnlyList<ShiftBookingPolicy> existing = await policyRepository.GetAsync(
            new ShiftBookingPolicyDescriptor { ShiftId = dto.ShiftId }, cancellationToken);

        return ShiftBookingPolicyRules.ValidateWrite(shift.DayOfWeek, dto.Date, existing, candidateId);
    }
}
