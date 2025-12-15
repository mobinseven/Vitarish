

namespace Vitarish.Sea.Tools;

[McpServerToolType]
public static class ProceduresTools
{
    [McpServerTool(Name = nameof(GetProceduresAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query retrieves procedures by descriptor.")]
    public static async Task<IReadOnlyList<ProcedureDto>> GetProceduresAsync(
            IProceduresClient ProceduresService,
            ProcedureDescriptorDto descriptor,
            CancellationToken cancellationToken = default) => (await ProceduresService.GetListAsync(descriptor, cancellationToken))
            .EnsureValue("Get procedures");

    [McpServerTool(Name = nameof(AddProcedureAsync), Destructive = false, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command creates a new procedure.")]
    public static Task<Guid> AddProcedureAsync(
            IProceduresClient ProceduresService,
            Guid projectId,
            string name,
            CancellationToken cancellationToken = default) => ProceduresService.CreateProcedureAsync(projectId, name, cancellationToken);

    [McpServerTool(Name = nameof(UpdateProcedureAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command updates an existing procedure.")]
    public static Task UpdateProcedureAsync(
            IProceduresClient ProceduresService,
            Guid id,
            string name,
            CancellationToken cancellationToken = default) => ProceduresService.UpdateProcedureNameAsync(id, name, cancellationToken);

    [McpServerTool(Name = nameof(DeleteProcedureAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command deletes a procedure.")]
    public static Task DeleteProcedureAsync(
            IProceduresClient ProceduresService,
            Guid id,
            CancellationToken cancellationToken = default) => ProceduresService.DeleteAsync(id, cancellationToken);

    [McpServerTool(Name = nameof(GetProcedureSnapshotAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query retrieves a snapshot for the specified procedure.")]
    public static Task<ProcedureSnapshot?> GetProcedureSnapshotAsync(
            IProceduresClient ProceduresService,
            Guid id,
            CancellationToken cancellationToken = default) => ProceduresService.CaptureSnapshotAsync(id, cancellationToken);
}

