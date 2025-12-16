namespace Vitarish.Sea.Infrastructure.Services;

using System;
using System.Collections.Generic;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Models;
using Vitarish.Sea.Infrastructure.Extensions;
using Vitarish.Sea.Infrastructure.Helpers;

[RegisterScoped]
internal class ProjectsService(
    IServiceScopeFactory serviceScopeFactory,
    ProjectsMapper mapper,
    IProceduresService proceduresService) : EntityService<Project, ProjectDescriptor, ProjectDto, ProjectDto, ProjectDescriptorDto>(serviceScopeFactory, mapper.ToDto, mapper.ToDomain, mapper.ToDomain), IProjectsService
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
        if(project is null)
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
        if(project is null)
        {
            return;
        }

        project.SetMetadata(metadata);

        _ = (await UpdateEntityAsync(project, cancellationToken)).EnsureValue("Update project metadata");
    }

    public async Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken)
    {
        ProjectDto? project = (await GetAsync(id, cancellationToken)).GetValueOrDefault();
        if(project is null)
        {
            return null;
        }

        (string? overview, DateTimeOffset? updatedAt) = OverviewMetadataParser.Parse(project.Metadata);

        ProcedureDescriptorDto descriptor = new() { ProjectId = id };
        IReadOnlyList<ProcedureDto> procedures = (await proceduresService.GetListAsync(descriptor, cancellationToken))
            .EnsureValue("Get project procedures");

        List<ProcedureSnapshot> procedureSnapshots = new(procedures.Count);
        foreach(ProcedureDto procedure in procedures)
        {
            ProcedureSnapshot? snapshot = await proceduresService.CaptureSnapshotAsync(procedure.Id, cancellationToken);
            if(snapshot is not null)
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
