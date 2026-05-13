namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class UpdateShiftStatusHandler(IEntityRepository<Shift, ShiftDescriptor> repository)
    : IRequestHandler<UpdateShiftStatusCommand>
{
    public async Task Handle(UpdateShiftStatusCommand request, CancellationToken cancellationToken)
    {
        Shift? entity = await repository.GetAsync(request.ShiftId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.Status = request.Status;
        _ = await repository.UpdateAsync(entity, cancellationToken);
    }
}
