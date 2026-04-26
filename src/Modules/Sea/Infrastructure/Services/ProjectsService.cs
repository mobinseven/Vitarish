namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class ProjectsService(
    IMediator mediator,
    IProceduresService proceduresService) : EntityService<Project, ProjectDescriptor, ProjectReadDto, ProjectWriteDto, ProjectDescriptorDto>
    (
        mediator,
        project => project.ToDto(),
        project => project.ToDomain(),
        project => project.ToDomain()
    ),
    IProjectsService
{
}
