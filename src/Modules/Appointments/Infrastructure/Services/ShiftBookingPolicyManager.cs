using Vitarish.Appointments.Domain.Mappers;
using Vitarish.Appointments.Domain.Rules;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftBookingPolicyManager(
    IMediator mediator,
    IEntityRepository<Shift, ShiftDescriptor> shiftRepository,
    IEntityRepository<ShiftBookingPolicy, ShiftBookingPolicyDescriptor> policyRepository)
    : EntityService<ShiftBookingPolicy, ShiftBookingPolicyDescriptor, ShiftBookingPolicyReadDto, ShiftBookingPolicyWriteDto, ShiftBookingPolicyDescriptorDto>(
        mediator,
        policy => policy.ToRead(),
        dto => dto.ToEntity(),
        dto => dto.ToDescriptor()),
      IShiftBookingPolicyManager
{
    async Task<EntityResult<ShiftBookingPolicyReadDto>> IEntityService<ShiftBookingPolicyReadDto, ShiftBookingPolicyWriteDto, ShiftBookingPolicyDescriptorDto>.AddAsync(
        ShiftBookingPolicyWriteDto dto,
        CancellationToken cancellationToken)
    {
        ShiftBookingPolicyViolation? violation = await ValidateWriteAsync(dto, null, cancellationToken);
        return violation is not null
            ? EntityResult<ShiftBookingPolicyReadDto>.Failure(
                EntityError.Validation(violation.Code.ToString(), violation.Field))
            : await base.AddAsync(dto, cancellationToken);
    }

    async Task<EntityResult<ShiftBookingPolicyReadDto>> IEntityService<ShiftBookingPolicyReadDto, ShiftBookingPolicyWriteDto, ShiftBookingPolicyDescriptorDto>.UpdateAsync(
        ShiftBookingPolicyWriteDto dto,
        CancellationToken cancellationToken)
    {
        ShiftBookingPolicyViolation? violation = await ValidateWriteAsync(dto, dto.Id, cancellationToken);
        return violation is not null
            ? EntityResult<ShiftBookingPolicyReadDto>.Failure(
                EntityError.Validation(violation.Code.ToString(), violation.Field))
            : await base.UpdateAsync(dto, cancellationToken);
    }

    private async Task<ShiftBookingPolicyViolation?> ValidateWriteAsync(
        ShiftBookingPolicyWriteDto dto,
        Guid? candidateId,
        CancellationToken cancellationToken)
    {
        Task<Shift?> shiftTask = shiftRepository.GetAsync(dto.ShiftId, cancellationToken);
        Task<IReadOnlyList<ShiftBookingPolicy>> existingTask = policyRepository.GetAsync(
            new ShiftBookingPolicyDescriptor { ShiftId = dto.ShiftId }, cancellationToken);

        await Task.WhenAll(shiftTask, existingTask);

        Shift? shift = await shiftTask;
        if (shift is null)
        {
            return new ShiftBookingPolicyViolation(ShiftBookingPolicyErrorCode.ShiftNotFound, nameof(dto.ShiftId));
        }

        IReadOnlyList<ShiftBookingPolicy> existing = await existingTask;

        return ShiftBookingPolicyRules.ValidateWrite(shift.DayOfWeek, dto.Date, existing, candidateId);
    }
}
