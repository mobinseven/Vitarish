namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class ProjectsService(
    IMediator mediator,
    ProjectsMapper mapper,
    IProceduresService proceduresService) : EntityService<Project, ProjectDescriptor, ProjectReadDto, ProjectWriteDto, ProjectDescriptorDto>(mediator, mapper.ToDto, mapper.ToDomain, mapper.ToDomain), IProjectsService
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

    public async Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken)
    {
        ProjectReadDto? project = (await GetAsync(id, cancellationToken)).GetValueOrDefault();
        if (project is null)
        {
            return null;
        }

        (string? overview, DateTimeOffset? updatedAt) = OverviewMetadataParser.Parse(project.Metadata);

        ProcedureDescriptorDto descriptor = new() { ProjectId = id };
        IReadOnlyList<ProcedureReadDto> procedures = (await proceduresService.GetListAsync(descriptor, cancellationToken))
            .EnsureValue("Get project procedures");

        List<ProcedureSnapshot> procedureSnapshots = new(procedures.Count);
        foreach (ProcedureReadDto procedure in procedures)
        {
            ProcedureSnapshot? snapshot = await proceduresService.CaptureSnapshotAsync(procedure.Id, cancellationToken);
            if (snapshot is not null)
            {
                procedureSnapshots.Add(snapshot);
            }
        }

        return new ProjectSnapshot(
            project.Id,
            project.Name,
            project.Description,
            project.Template,
            project.Metadata,
            overview,
            updatedAt,
            project.Stats,
            procedureSnapshots);
    }
}
