namespace Vitarish.Sea.Interface.Components.Procedures;

public partial class ProcedureEditDialog : ProceduresBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public Guid ProjectId { get; set; }
    [Parameter] public ProcedureDto? Procedure { get; set; }
    [Parameter] public string ProjectName { get; set; } = string.Empty;

    private string name = string.Empty;

    protected override void OnInitialized()
    {
        if (Procedure is not null)
        {
            name = Procedure.Name;
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
            Search = name
        };

        IReadOnlyList<ProcedureDto> existing = await ProceduresService.GetListAsync(descriptor, CancellationToken.None)
            .EnsureValueAsync("Load procedures");
        if (existing.Any(p => p.Id != Procedure.Id && string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase)))
        {
            _ = await DialogService.ShowMessageBox(
                Localizer["Procedure"],
                Localizer["ProcedureExistsError", ProjectName, name],
                yesText: Localizer["Ok"]);
            return;
        }

        await ProceduresService.UpdateAsync(Procedure.Id, name, CancellationToken.None);

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();
}
