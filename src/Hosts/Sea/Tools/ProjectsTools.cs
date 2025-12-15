using System.ComponentModel;

namespace Vitarish.Sea.Tools;

[McpServerToolType]
public static class ProjectsTools
{
    [McpServerTool(Name = nameof(GetProjectsAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query retrieves projects by description.")]
    public static async Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(
            IProjectsClient ProjectsService,
            ProjectDescriptorDto projectDescriptor,
            CancellationToken cancellationToken = default) => (await ProjectsService.GetListAsync(projectDescriptor, cancellationToken))
            .EnsureValue("Get projects");

    [McpServerTool(Name = nameof(GetProjectsCountAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = false), Description("This query gets the number of projects.")]
    public static async Task<int> GetProjectsCountAsync(
            IProjectsClient ProjectsService,
            ProjectDescriptorDto projectDescriptor,
            CancellationToken cancellationToken = default) => (await ProjectsService.CountAsync(projectDescriptor, cancellationToken))
            .EnsureValue("Count projects");

    [McpServerTool(Name = nameof(GetProjectAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query gets a project by its identifier.")]
    public static async Task<ProjectDto?> GetProjectAsync(
            IProjectsClient ProjectsService,
            Guid id,
            CancellationToken cancellationToken = default) => (await ProjectsService.GetAsync(id, cancellationToken)).GetValueOrDefault();

    [McpServerTool(Name = nameof(AddProjectAsync), Destructive = false, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command creates a new project.")]
    public static Task<Guid> AddProjectAsync(
            IProjectsClient ProjectsService,
            string name,
            string description,
            bool template = false,
            string metadata = "",
            CancellationToken cancellationToken = default) => ProjectsService.AddAsync(name, description, template, metadata, cancellationToken);

    [McpServerTool(Name = nameof(UpdateProjectAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command updates an existing project.")]
    public static Task UpdateProjectAsync(
            IProjectsClient ProjectsService,
            Guid id,
            string name,
            string description,
            bool template,
            string metadata,
            CancellationToken cancellationToken = default) => ProjectsService.UpdateAsync(id, name, description, template, metadata, cancellationToken);

    [McpServerTool(Name = nameof(RemoveProjectAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command deletes a project.")]
    public static Task RemoveProjectAsync(
            IProjectsClient ProjectsService,
            Guid id,
            CancellationToken cancellationToken = default) => ProjectsService.RemoveByIdAsync(id, cancellationToken);

    [McpServerTool(Name = nameof(GetProjectSnapshotAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query retrieves a snapshot for the specified project.")]
    public static Task<ProjectSnapshot?> GetProjectSnapshotAsync(
            IProjectsClient ProjectsService,
            Guid id,
            CancellationToken cancellationToken = default) => ProjectsService.CaptureSnapshotAsync(id, cancellationToken);
}
