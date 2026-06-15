using Vitarish.Sea.Domain.Mappers;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class ProceduresService(
    IMediator mediator,
    IEventsService eventsService)
    : EntityService<Procedure, ProcedureDescriptor, ProcedureReadDto, ProcedureWriteDto, ProcedureDescriptorDto>
    (
        mediator,
        procedure => procedure.ToDto(),
        procedure => procedure.ToDomain(),
        procedure => procedure.ToDomain()
    ),
    IProceduresService
{
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Procedure? entity = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if (entity is null)
        {
            return;
        }

        await Mediator.Send(new UpdateEventsChainCommand(id, []), cancellationToken);
        (await RemoveAsync(entity, cancellationToken)).EnsureSuccess("Remove procedure");
    }
}
