using Vitarish.Sea.Contracts.Responses;

namespace Vitarish.Sea.Interface.Mapping;

public static class SeaUiRequestMapping
{
    public static ProjectWriteDto ToUpdateRequest(this ProjectReadDto project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        AvatarFileId = project.AvatarFileId
    };

    public static ProcedureWriteDto ToUpdateRequest(this ProcedureReadDto procedure) => new()
    {
        Id = procedure.Id,
        Name = procedure.Name,
        Description = procedure.Description,
        ProjectId = procedure.ProjectId
    };

    public static EventWriteDto ToUpdateRequest(this EventReadDto seaEvent) => new()
    {
        Id = seaEvent.Id,
        Title = seaEvent.Title,
        Description = seaEvent.Description,
        ProcedureId = seaEvent.ProcedureId
    };
}
