namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class UpdateSessionStatusHandler(IEntityRepository<Session, SessionDescriptor> repository)
    : IRequestHandler<UpdateSessionStatusCommand>
{
    public async Task Handle(UpdateSessionStatusCommand request, CancellationToken cancellationToken)
    {
        Session? entity = await repository.GetAsync(request.SessionId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.Status = request.Status;
        _ = await repository.UpdateAsync(entity, cancellationToken);
    }
}
