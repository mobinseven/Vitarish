namespace Vitarish.Sea.Interface.Components.Procedures;

public partial class ProcedureEditDialog : ProceduresBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public Guid ProjectId { get; set; }
    [Parameter] public ProcedureReadDto? Procedure { get; set; }
    [Parameter] public string ProjectName { get; set; } = string.Empty;

    private ProcedureWriteDto procedure = new();
    protected override void OnInitialized()
    {
        if (Procedure is not null)
        {
            procedure = Procedure.ToWrite();
        }
    }

    private async Task SaveAsync()
    {
        if (Procedure is null)
        {
            MudDialog.Cancel();
            return;
        }

        ProcedureDescriptorDto descriptor = new()
        {
            ProjectId = ProjectId,
            Search = procedure.Name
        };

        IReadOnlyList<ProcedureReadDto> existing = await ProceduresService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync() ?? [];
        if (existing.Any(p => p.Id != Procedure.Id && string.Equals(p.Name, procedure.Name, StringComparison.CurrentCultureIgnoreCase)))
        {
            _ = await DialogService.ShowMessageBox(
                Localizer["Procedure"],
                Localizer["ProcedureExistsError", ProjectName, procedure.Name],
                yesText: Localizer["Ok"]);
            return;
        }

        ProcedureReadDto? updated = await ProceduresService.UpdateAsync(procedure, CancellationToken.None).GetValueOrDefaultAsync();

        MudDialog.Close(DialogResult.Ok(true));
    }
    private void Cancel() => MudDialog.Cancel();
}
