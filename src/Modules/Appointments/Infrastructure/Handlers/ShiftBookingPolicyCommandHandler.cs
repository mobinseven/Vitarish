using Vitarish.Appointments.Domain.Models;
using Vitarish.Appointments.Application.Abstractions.BookingPolicies;
using MediatR;
using Vitarish.Appointments.Domain.Events;
using Vitarish.Appointments.Domain.Rules;
using Vitarish.Appointments.Domain.Services;

namespace Vitarish.Appointments.Infrastructure.Handlers;

[RegisterTransient]
internal sealed class ShiftBookingPolicyCommandHandler(IShiftBookingPolicyRepository repository, IPublisher publisher) : IUpsertShiftBookingPolicyCommand, IRemoveShiftBookingPolicyCommand
{
    public Task<EntityResult<ShiftBookingPolicyReadDto>> CreateAsync(UpsertShiftBookingPolicyCommandModel model, CancellationToken cancellationToken)
        => UpsertInternalAsync(model, isCreate: true, cancellationToken);

    public Task<EntityResult<ShiftBookingPolicyReadDto>> UpdateAsync(UpsertShiftBookingPolicyCommandModel model, CancellationToken cancellationToken)
        => UpsertInternalAsync(model, isCreate: false, cancellationToken);

    private async Task<EntityResult<ShiftBookingPolicyReadDto>> UpsertInternalAsync(UpsertShiftBookingPolicyCommandModel model, bool isCreate, CancellationToken cancellationToken)
    {
        if (model.ShiftId == Guid.Empty || (isCreate && model.PolicyId is not null)) return EntityResult<ShiftBookingPolicyReadDto>.Failure(EntityError.Validation(ShiftBookingPolicyErrorCode.InvalidCommand.ToString(), nameof(model.ShiftId)));

        Shift? shift = await repository.GetShiftWithPoliciesAsync(model.ShiftId, cancellationToken);
        if (shift is null) return EntityResult<ShiftBookingPolicyReadDto>.Failure(EntityError.NotFound(ShiftBookingPolicyErrorCode.ShiftNotFound.ToString(), nameof(model.ShiftId)));

        ShiftBookingPolicy? policy = isCreate ? null : shift.BookingPolicies.FirstOrDefault(x => x.Id == model.PolicyId);
        if (!isCreate && policy is null) return EntityResult<ShiftBookingPolicyReadDto>.Failure(EntityError.NotFound(ShiftBookingPolicyErrorCode.PolicyNotFound.ToString(), nameof(model.PolicyId)));

        ShiftBookingPolicyViolation? violation = ShiftBookingPolicyRules.ValidateWrite(shift, model.Date, policy?.Id);
        if (violation is not null) return EntityResult<ShiftBookingPolicyReadDto>.Failure(EntityError.Validation(violation.Code.ToString(), violation.Field));

        policy = isCreate ? ShiftBookingPolicyMutator.Create(shift, model) : policy!;
        if (!isCreate) ShiftBookingPolicyMutator.Update(policy, model);
        await repository.SaveAsync(cancellationToken);
        await publisher.Publish(isCreate ? new ShiftBookingPolicyCreated(policy.Id) : new ShiftBookingPolicyUpdated(policy.Id), cancellationToken);
        return EntityResult<ShiftBookingPolicyReadDto>.Success(policy.ToDto());
    }

    public async Task<EntityResult> ExecuteAsync(RemoveShiftBookingPolicyCommandModel model, CancellationToken cancellationToken)
    {
        Shift? shift = await repository.GetShiftWithPoliciesAsync(model.ShiftId, cancellationToken);
        if (shift is null) return EntityResult.Failure(EntityError.NotFound(ShiftBookingPolicyErrorCode.ShiftNotFound.ToString(), nameof(model.ShiftId)));
        ShiftBookingPolicy? policy = shift.BookingPolicies.FirstOrDefault(x => x.Id == model.PolicyId);
        if (policy is null) return EntityResult.Failure(EntityError.NotFound(ShiftBookingPolicyErrorCode.PolicyNotFound.ToString(), nameof(model.PolicyId)));
        ShiftBookingPolicyMutator.Remove(shift, policy);
        await repository.SaveAsync(cancellationToken);
        await publisher.Publish(new ShiftBookingPolicyRemoved(model.PolicyId), cancellationToken);
        return EntityResult.Success();
    }
}
