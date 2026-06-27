using Microsoft.Extensions.Localization;

namespace Vitarish.Sea.Interface.Components.Procedures;

public partial class ProcedureTemplatesBrowser
{
    [Inject] private IStringLocalizer Localizer { get; set; } = default!;
    [Inject] private ISeaClient SeaClient { get; set; } = default!;
        
    private List<ProjectReadDto> projects = [];
    private readonly Dictionary<Guid, List<ProcedureReadDto>> procedures = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        projects = [.. (await SeaClient.ListProjectsAsync(CancellationToken.None)).Value ?? []];
        foreach (ProjectReadDto p in projects)
        {
            procedures[p.Id] = [.. (await SeaClient.ListProceduresAsync(p.Id, CancellationToken.None)).Value ?? []];
        }
    }
}
