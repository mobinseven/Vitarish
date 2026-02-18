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
    public async Task<Guid> AddAsync(string name, string description, bool template, string metadata, CancellationToken cancellationToken)
    {
        Project entity = (await AddEntityAsync(new Project(name, description, template, metadata), cancellationToken))
            .EnsureValue("Add project");

        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, string name, string description, bool template, string metadata, CancellationToken cancellationToken)
    {
        Project? project = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if (project is null)
        {
            return;
        }

        project.Rename(name);
        project.SetDescription(description);
        project.SetTemplate(template);
        project.SetMetadata(metadata);

        _ = (await UpdateEntityAsync(project, cancellationToken)).EnsureValue("Update project");
    }

    public async Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken)
    {
        Project? project = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if (project is null)
        {
            return;
        }

        project.SetMetadata(metadata);

        _ = (await UpdateEntityAsync(project, cancellationToken)).EnsureValue("Update project metadata");
    }
}
