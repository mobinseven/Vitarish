using Vitarish.Sea.Application.Queries.Projects;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class GetProjectTemplateStateHandler(IEntityRepository<Project, ProjectDescriptor> repository) : IRequestHandler<GetProjectTemplateStateQuery, bool>
{
    public async Task<bool> Handle(GetProjectTemplateStateQuery request, CancellationToken cancellationToken)
    {
        Project? entity = await repository.GetAsync(request.Id, cancellationToken);
        return entity?.Template ?? false;
    }
}
