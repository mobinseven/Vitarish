namespace Vitarish.Sea.Interface.Components.Procedures;

public partial class ProceduresGrid : ProceduresBase
{
    [Parameter, EditorRequired] public ProjectDto Project { get; set; }
    [Parameter] public EventCallback<Guid?> ProcedureClicked { get; set; }

    private List<ProcedureDto> procedures = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ReloadData();
            StateHasChanged();
        }
    }
        
    private async Task ReloadData()
    {
        ProcedureDescriptorDto descriptor = new()
        {
            ProjectId = Project.Id
        };
        IReadOnlyList<ProcedureDto> Items = await ProceduresService.GetListAsync(descriptor, CancellationToken.None)
            .EnsureValueAsync("Load procedures");

        procedures = [.. Items
            .OrderByDescending(e => (e.Stats?.Unread ?? false) || ((e.Stats?.NewItems ?? 0) > 0))
            .ThenBy(e => e.Name)];
    }

    protected override async Task ReloadRequired() => await ReloadData();

}
